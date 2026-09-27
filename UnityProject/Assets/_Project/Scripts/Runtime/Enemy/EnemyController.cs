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
        bool attackLanded;
        float flashTimer;
        float strafeSign = 1f;
        Vector3 slamDestination;
        Vector3 pullDestination;
        float pullSpeed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active.Clear();
            attackTokensInUse = 0;
        }

        public EnemyProfile Profile => profile;
        public Damageable Damageable => damageable;
        public bool IsDead => damageable == null || damageable.IsDead;
        public bool IsHeavy => profile != null && profile.heavy;
        public bool IsBeingPulled => state == State.Pulled;
        public bool IsTelegraphing => state == State.Windup;
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
                    if (stateTime >= stateDuration && grounded) EnterChase();
                    break;
                case State.Pulled:
                    planar = TickPulled(dt);
                    break;
            }

            if (state != State.Pulled) verticalVelocity += (juggled ? juggleGravity : gravity) * dt;
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

            if (cooldown <= 0f && distance <= attack.range && TryTakeToken())
            {
                EnterWindup();
                return Vector3.zero;
            }

            Vector3 direction = distance > 0.01f ? to / distance : transform.forward;
            Vector3 move;
            if (distance > attack.range * 0.85f)
                move = direction * profile.moveSpeed;
            else if (!hasToken)
                move = Vector3.Cross(Vector3.up, direction) * (strafeSign * profile.moveSpeed * 0.4f);
            else
                move = Vector3.zero;

            return move + Separation() * profile.moveSpeed * 0.6f;
        }

        void EnterWindup()
        {
            state = State.Windup;
            stateTime = 0f;
            attackLanded = false;
            if (attack.jumpSlam)
            {
                verticalVelocity = 12f;
                slamDestination = target.position;
            }
        }

        Vector3 TickWindup(float dt)
        {
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
                if (attack.jumpSlam || attack.radius * profile.scale >= 3f)
                {
                    var center = transform.TransformPoint(attack.offset);
                    if (CombatFeedback.Instance != null)
                    {
                        CombatFeedback.Instance.SpawnShockwave(new Vector3(center.x, transform.position.y, center.z),
                            attack.radius * profile.scale, profile.telegraphColor);
                        CombatFeedback.Instance.Shake(0.2f);
                    }
                }
            }
            return planar;
        }

        Vector3 TickAttack()
        {
            DoAttackHit();
            Vector3 planar = attack.lunge > 0f && attack.active > 0f
                ? transform.forward * (attack.lunge / attack.active)
                : Vector3.zero;

            if (stateTime >= attack.active)
            {
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
            cooldown = profile.attackCooldown * Random.Range(0.7f, 1.3f);
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

        EnemyAttack PickAttack()
        {
            float total = 0f;
            foreach (var a in profile.attacks) total += Mathf.Max(0f, a.weight);
            float roll = Random.value * total;
            foreach (var a in profile.attacks)
            {
                roll -= Mathf.Max(0f, a.weight);
                if (roll <= 0f) return a;
            }
            return profile.attacks[0];
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
