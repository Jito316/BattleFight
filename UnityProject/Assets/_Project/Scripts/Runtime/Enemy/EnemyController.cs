using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 敵の簡易AI。追跡 → 予備動作(色で予告) → 攻撃 → 硬直 を繰り返す。
    /// 同時に攻撃してくる敵の数はトークンで制限する(ボスは例外)。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Damageable))]
    public class EnemyController : MonoBehaviour
    {
        enum State
        {
            Chase,
            Windup,
            Attack,
            Recovery,
            Stagger,
            Pulled,
            Dead,
        }

        const int MaxAttackTokens = 2;
        const float FlashDuration = 0.08f;
        const float DeathDuration = 0.5f;

        public static readonly List<EnemyController> Active = new List<EnemyController>();
        static int attackTokensInUse;
        static readonly Collider[] OverlapBuffer = new Collider[16];

        [SerializeField] EnemyProfile profile;
        [SerializeField] float gravity = -25f;
        [SerializeField, Tooltip("打ち上げられている間の重力(空中コンボ用に弱め)")] float juggleGravity = -11f;

        CharacterController controller;
        Damageable damageable;
        Transform target;
        Renderer[] renderers;
        Color[] baseColors;

        State state;
        float stateTime;
        float stateDuration;
        EnemyAttack attack;
        float cooldown;
        float verticalVelocity;
        Vector3 knockback;
        bool grounded;
        bool juggled;
        bool hasToken;
        bool teleported;
        int repeatsLeft;
        readonly List<EnemyAttack> availableAttacks = new List<EnemyAttack>();
        bool attackLanded;
        float flashTimer;
        float strafeSign = 1f;
        Vector3 slamDestination;
        Vector3 attackDirection;
        Vector3 pullDestination;
        float pullSpeed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            attackTokensInUse = 0;
            PhaseChanged = null;
        }

        public EnemyProfile Profile => profile;
        public Damageable Damageable => damageable;
        public bool IsDead => damageable == null || damageable.IsDead;
        public bool IsHeavy => profile != null && profile.heavy;
        public bool IsBeingPulled => state == State.Pulled;
        public bool IsTelegraphing => state == State.Windup;
        public bool IsPhase2 { get; private set; }
        float MoveSpeed => profile.moveSpeed * (IsPhase2 ? profile.phase2SpeedMultiplier : 1f);

        /// <summary>ボスが第二形態になった(HUD の告知用)</summary>
        public static event System.Action<EnemyController> PhaseChanged;
        public float Height => controller.height * transform.lossyScale.y;
        public float Radius => controller.radius * transform.lossyScale.x;
        public Vector3 CenterPoint => transform.position + Vector3.up * (Height * 0.5f);

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            damageable = GetComponent<Damageable>();
        }

        void OnEnable()
        {
            Active.Add(this);
            damageable.Damaged += OnDamaged;
            damageable.Died += OnDied;
        }

        void OnDisable()
        {
            Active.Remove(this);
            ReleaseToken();
            damageable.Damaged -= OnDamaged;
            damageable.Died -= OnDied;
        }

        public void Initialize(EnemyProfile newProfile, Transform player)
        {
            profile = newProfile;
            target = player;
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].material.color;
            cooldown = Random.Range(0.6f, 1.6f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            state = State.Chase;
        }

        void Update()
        {
            if (profile == null) return;
            float dt = Time.deltaTime;

            if (state == State.Dead)
            {
                stateTime += dt;
                transform.localScale = Vector3.one * (profile.scale * Mathf.Max(0f, 1f - stateTime / DeathDuration));
                if (stateTime >= DeathDuration) Destroy(gameObject);
                return;
            }

            stateTime += dt;
            cooldown -= dt;

            Vector3 planar = Vector3.zero;
            switch (state)
            {
                case State.Chase:
                    planar = TickChase(dt);
                    break;
                case State.Windup:
                    planar = TickWindup(dt);
                    break;
                case State.Attack:
                    planar = TickAttack();
                    break;
                case State.Recovery:
                    if (stateTime >= attack.recovery) EnterChase();
                    break;
                case State.Stagger:
                    if (stateTime >= stateDuration && (grounded || profile.flying))
                    {
                        juggled = false;
                        EnterChase();
                    }
                    break;
                case State.Pulled:
                    planar = TickPulled(dt);
                    break;
            }

            // 自爆などでこのフレームに倒れた
            if (state == State.Dead) return;

            if (profile.flying && !juggled && state != State.Pulled)
            {
                // 地面から一定の高さに浮く。急降下中は高さを保たない
                float targetY = GroundHeight() + profile.hoverHeight;
                verticalVelocity = state == State.Attack ? 0f : Mathf.Clamp((targetY - transform.position.y) * 3f, -6f, 6f);
            }
            else if (state != State.Pulled)
            {
                verticalVelocity += (juggled ? juggleGravity : gravity) * dt;
            }
            knockback = Vector3.Lerp(knockback, Vector3.zero, 1f - Mathf.Exp(-6f * dt));

            var flags = controller.Move((planar + knockback + Vector3.up * verticalVelocity) * dt);
            grounded = (flags & CollisionFlags.Below) != 0;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
                juggled = false;
            }

            UpdateColor(dt);
        }

        // ---------- 状態 ----------

        Vector3 TickChase(float dt)
        {
            if (target == null) return Vector3.zero;

            Vector3 to = Flat(target.position - transform.position);
            float distance = to.magnitude;
            if (attack == null) attack = PickAttack();
            Face(to, dt);

            // 攻撃を持たない敵(訓練用の人形など)は向きを変えるだけ
            if (attack == null) return Separation() * MoveSpeed * 0.6f;

            if (cooldown <= 0f && distance <= attack.range && TryTakeToken())
            {
                EnterWindup();
                return Vector3.zero;
            }

            Vector3 direction = distance > 0.01f ? to / distance : transform.forward;
            Vector3 move;
            if (profile.preferredDistance > 0f && distance < profile.preferredDistance * 0.7f)
                move = -direction * (MoveSpeed * 0.8f);
            else if (distance > attack.range * 0.85f)
                move = direction * MoveSpeed;
            else if (!hasToken)
                move = Vector3.Cross(Vector3.up, direction) * (strafeSign * MoveSpeed * 0.4f);
            else
                move = Vector3.zero;

            return move + Separation() * MoveSpeed * 0.6f;
        }

        void EnterWindup()
        {
            state = State.Windup;
            stateTime = 0f;
            attackLanded = false;
            teleported = false;
            repeatsLeft = attack.repeat;
            if (attack.jumpSlam)
            {
                verticalVelocity = 12f;
                slamDestination = target.position;
            }
        }

        /// <summary>連続攻撃の2発目以降。予備動作を短くしてもう一度出す</summary>
        void EnterRepeatWindup()
        {
            repeatsLeft--;
            state = State.Windup;
            stateTime = Mathf.Max(0f, attack.windup - attack.repeatWindup);
            attackLanded = false;
        }

        /// <summary>予備動作の途中で消え、プレイヤーの背後に現れる(残りの予備動作は背後で見せる)</summary>
        void TeleportBehindTarget()
        {
            teleported = true;
            if (target == null) return;
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 1f, profile.telegraphColor);

            Vector3 behind = target.position - Flat(target.forward).normalized * (Radius + 1.8f);
            // アリーナの外に出ないようにする
            Vector3 flat = Flat(behind);
            if (flat.magnitude > 27f) behind = flat.normalized * 27f + Vector3.up * behind.y;
            behind.y = target.position.y + 0.1f;

            controller.enabled = false;
            transform.position = behind;
            controller.enabled = true;
            Vector3 look = Flat(target.position - transform.position);
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 1f, profile.telegraphColor);
        }

        Vector3 TickWindup(float dt)
        {
            if (attack.teleportBehind && !teleported && stateTime >= attack.windup * 0.5f) TeleportBehindTarget();
            if (stateTime < attack.windup * 0.6f && target != null) Face(Flat(target.position - transform.position), dt);

            Vector3 planar = Vector3.zero;
            if (attack.jumpSlam)
            {
                Vector3 to = Flat(slamDestination - transform.position);
                float remaining = Mathf.Max(0.05f, attack.windup - stateTime);
                planar = Vector3.ClampMagnitude(to / remaining, 20f);
            }

            if (stateTime >= attack.windup && (!attack.jumpSlam || grounded))
            {
                state = State.Attack;
                stateTime = 0f;
                OnAttackStart();
            }
            return planar;
        }

        void OnAttackStart()
        {
            // 飛んでいる敵はプレイヤーへ向かって斜めに急降下する
            attackDirection = transform.forward;
            if (profile.flying && target != null)
            {
                Vector3 to = target.position + Vector3.up - CenterPoint;
                if (to.sqrMagnitude > 0.01f) attackDirection = to.normalized;
            }

            if (attack.ranged)
            {
                FireProjectiles();
                attackLanded = true;
            }
            else if (attack.targetedStrike)
            {
                if (target != null)
                {
                    EnemyProjectile.Spawn(gameObject, target.position + Vector3.up * 0.05f, Vector3.zero, attack.radius, attack.damage,
                        attack.knockback, profile.telegraphColor, delay: attack.strikeDelay);
                }
                attackLanded = true;
            }
            else if (attack.summon != null && attack.summonCount > 0)
            {
                for (int i = 0; i < attack.summonCount; i++)
                {
                    float angle = 360f * i / attack.summonCount;
                    Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (Radius + 2f);
                    if (ArenaDirector.Instance != null) ArenaDirector.Instance.SpawnExtra(attack.summon, transform.position + offset);
                }
                if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 2f, profile.telegraphColor);
                attackLanded = true;
            }

            if (attack.jumpSlam || attack.selfDestruct || attack.radius * profile.scale >= 3f)
            {
                var center = transform.TransformPoint(attack.offset);
                if (CombatFeedback.Instance != null && !attack.ranged && !attack.targetedStrike)
                {
                    CombatFeedback.Instance.SpawnShockwave(new Vector3(center.x, transform.position.y, center.z),
                        attack.radius * profile.scale, profile.telegraphColor);
                    CombatFeedback.Instance.Shake(0.2f);
                }
            }
        }

        void FireProjectiles()
        {
            if (target == null) return;
            Vector3 origin = CenterPoint + transform.forward * (Radius + 0.3f);
            Vector3 aim = target.position + Vector3.up * 1.1f - origin;
            aim = aim.sqrMagnitude > 0.01f ? aim.normalized : transform.forward;
            int count = Mathf.Max(1, attack.projectileCount);
            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : Mathf.Lerp(-attack.projectileSpread * 0.5f, attack.projectileSpread * 0.5f, i / (count - 1f));
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * aim;
                EnemyProjectile.Spawn(gameObject, origin, direction * attack.projectileSpeed, attack.projectileRadius, attack.damage,
                    attack.knockback, profile.telegraphColor);
            }
        }

        Vector3 TickAttack()
        {
            DoAttackHit();
            if (attack.selfDestruct && !IsDead)
            {
                damageable.Kill();
                return Vector3.zero;
            }

            Vector3 planar = attack.lunge > 0f && attack.active > 0f
                ? attackDirection * (attack.lunge / attack.active)
                : Vector3.zero;

            if (stateTime >= attack.active)
            {
                if (repeatsLeft > 0 && !IsDead)
                {
                    EnterRepeatWindup();
                    return planar;
                }
                state = State.Recovery;
                stateTime = 0f;
                ReleaseToken();
            }
            return planar;
        }

        void DoAttackHit()
        {
            if (attackLanded) return;
            Vector3 center = transform.TransformPoint(attack.offset);
            float radius = attack.radius * profile.scale;
            int count = Physics.OverlapSphereNonAlloc(center, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var victim = OverlapBuffer[i].GetComponentInParent<Damageable>();
                if (victim == null || victim.Team != Team.Player || victim.IsDead) continue;

                Vector3 direction = Flat(victim.transform.position - transform.position);
                victim.ApplyHit(new HitInfo
                {
                    damage = attack.damage,
                    stagger = 100f,
                    knockback = attack.knockback,
                    direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward,
                    source = gameObject,
                });
                attackLanded = true;
                return;
            }
        }

        void EnterChase()
        {
            state = State.Chase;
            stateTime = 0f;
            attack = null;
            cooldown = profile.attackCooldown * Random.Range(0.7f, 1.3f) * (IsPhase2 ? profile.phase2CooldownMultiplier : 1f);
        }

        void EnterStagger(float duration)
        {
            ReleaseToken();
            attack = null;
            state = State.Stagger;
            stateTime = 0f;
            stateDuration = duration;
        }

        public void BeginPull(Vector3 destination, float speed)
        {
            if (IsDead) return;
            ReleaseToken();
            attack = null;
            state = State.Pulled;
            stateTime = 0f;
            pullDestination = destination;
            pullSpeed = speed;
            verticalVelocity = 0f;
        }

        Vector3 TickPulled(float dt)
        {
            verticalVelocity = 0f;
            Vector3 to = pullDestination - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.3f || stateTime > 0.6f)
            {
                EnterStagger(0.7f);
                return Vector3.zero;
            }
            return to.normalized * Mathf.Min(pullSpeed, to.magnitude / Mathf.Max(dt, 1e-4f));
        }

        // ---------- 被弾 ----------

        void OnDamaged(HitInfo hit, HitOutcome outcome)
        {
            if (state == State.Dead) return;
            flashTimer = FlashDuration;

            float knockScale = outcome == HitOutcome.Armored ? 0.15f : 1f;
            if (profile.isBoss) knockScale *= 0.3f;
            knockback += hit.direction * (hit.knockback * knockScale);

            // 第二形態になったときは、そのひるみ(長め)を普通のひるみで上書きしない
            if (outcome != HitOutcome.Killed && CheckPhase2()) return;

            switch (outcome)
            {
                case HitOutcome.Killed:
                    return;
                case HitOutcome.ArmorBroken:
                    EnterStagger(profile.armorBreakStagger);
                    break;
                case HitOutcome.Armored:
                    return;
                default:
                    if (hit.stagger >= profile.staggerThreshold)
                    {
                        EnterStagger(profile.staggerDuration);
                        if (hit.launch > 0f && !profile.isBoss)
                        {
                            verticalVelocity = hit.launch;
                            juggled = true;
                        }
                    }
                    break;
            }

            // 空中で攻撃を受けている間は落ちにくくする
            if (juggled && !grounded) verticalVelocity = Mathf.Max(verticalVelocity, 2.5f);
        }

        void OnDied()
        {
            ReleaseToken();
            state = State.Dead;
            stateTime = 0f;
            controller.enabled = false;
            Active.Remove(this);
        }

        // ---------- 補助 ----------

        float GroundHeight()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }
            return 0f;
        }

        /// <summary>今使える攻撃(第二形態では増える)</summary>
        List<EnemyAttack> AvailableAttacks()
        {
            availableAttacks.Clear();
            if (profile.attacks != null) availableAttacks.AddRange(profile.attacks);
            if (IsPhase2 && profile.phase2Attacks != null) availableAttacks.AddRange(profile.phase2Attacks);
            return availableAttacks;
        }

        EnemyAttack PickAttack()
        {
            var attacks = AvailableAttacks();
            if (attacks.Count == 0) return null;
            float total = 0f;
            foreach (var a in attacks) total += Mathf.Max(0f, a.weight);
            float roll = Random.value * total;
            foreach (var a in attacks)
            {
                roll -= Mathf.Max(0f, a.weight);
                if (roll <= 0f) return a;
            }
            return attacks[0];
        }

        /// <summary>指定した攻撃をすぐに始める(テストやデバッグ用)。使える攻撃の番号で指定する</summary>
        public bool ForceAttack(int index)
        {
            var attacks = AvailableAttacks();
            if (IsDead || index < 0 || index >= attacks.Count || target == null) return false;
            ReleaseToken();
            attack = attacks[index];
            TryTakeToken();
            EnterWindup();
            return true;
        }

        /// <summary>体力が一定を下回ったら第二形態になる。少しひるんで、アーマーが戻る</summary>
        bool CheckPhase2()
        {
            if (IsPhase2 || profile.phase2HealthRatio <= 0f || IsDead) return false;
            if (damageable.Health > damageable.MaxHealth * profile.phase2HealthRatio) return false;

            IsPhase2 = true;
            damageable.RestoreArmor();
            EnterStagger(1.2f);
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position, Radius + 4f, profile.telegraphColor);
                CombatFeedback.Instance.Shake(0.4f);
            }
            PhaseChanged?.Invoke(this);
            return true;
        }

        bool TryTakeToken()
        {
            if (hasToken) return true;
            if (!profile.isBoss && attackTokensInUse >= MaxAttackTokens) return false;
            hasToken = true;
            attackTokensInUse++;
            return true;
        }

        void ReleaseToken()
        {
            if (!hasToken) return;
            hasToken = false;
            attackTokensInUse = Mathf.Max(0, attackTokensInUse - 1);
        }

        Vector3 Separation()
        {
            Vector3 push = Vector3.zero;
            foreach (var other in Active)
            {
                if (other == this) continue;
                Vector3 away = Flat(transform.position - other.transform.position);
                float minDistance = (Radius + other.Radius) * 1.5f;
                float distance = away.magnitude;
                if (distance > 0.01f && distance < minDistance) push += away / distance * (1f - distance / minDistance);
            }
            return push;
        }

        void Face(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude < 1e-4f) return;
            var rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, profile.turnSpeed * dt);
        }

        void UpdateColor(float dt)
        {
            if (renderers == null) return;
            flashTimer -= dt;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = baseColors[i];
                if (flashTimer > 0f)
                {
                    color = Color.white;
                }
                else if (state == State.Windup)
                {
                    float t = Mathf.Clamp01(stateTime / Mathf.Max(0.01f, attack.windup));
                    float blink = Mathf.PingPong(stateTime * 8f, 1f);
                    color = Color.Lerp(baseColors[i], profile.telegraphColor, t * (0.6f + 0.4f * blink));
                }
                else if (state == State.Stagger)
                {
                    color = Color.Lerp(baseColors[i], Color.gray, 0.4f);
                }
                renderers[i].material.color = color;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
