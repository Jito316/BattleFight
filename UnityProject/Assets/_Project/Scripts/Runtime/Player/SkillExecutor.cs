using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// スキルの実行とリアルタイム切り替え。
    /// ・技は 開始 → 攻撃判定 → 硬直 のフェーズで進む(フレーム数は SkillData)
    /// ・同じスロットのボタンで次の段へ、硬直の cancelFrame 以降は他の技・ジャンプでキャンセル
    /// ・実行中のスロットを硬直中に切り替えるとスワップキャンセル
    /// ・切り替えた直後の最初のヒットはスワップストライク
    /// </summary>
    public class SkillExecutor : MonoBehaviour
    {
        enum Phase
        {
            None,
            Startup,
            Active,
            Recovery,
            Charging,
            Airborne,
            Travel,
            PullWait,
        }

        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] SkillSlotController slots;
        [SerializeField] WeaponHolder weapons;
        [SerializeField] LockOnSystem lockOn;
        [SerializeField] StyleRankSystem style;
        [SerializeField] Damageable self;

        [Header("スワップ")]
        [SerializeField] float swapStrikeWindow = 1f;
        [SerializeField] float swapStrikeDamageMultiplier = 1.3f;
        [SerializeField] float swapStrikeStaggerMultiplier = 1.5f;
        [SerializeField, Tooltip("同じスロットで連続してスワップキャンセルすると、硬直のこの割合×回数だけ受付が遅くなる")]
        float swapCancelStreakPenalty = 0.35f;
        [SerializeField] float swapCancelStreakReset = 1.5f;

        [Header("溜め・カウンター")]
        [SerializeField] float minChargeMultiplier = 0.6f;
        [SerializeField] float maxChargeMultiplier = 1.5f;
        [SerializeField] float counterMultiplier = 2f;
        [SerializeField] float counterInvulnerableTime = 0.5f;

        [Header("移動系")]
        [SerializeField] float maxTravelTime = 0.8f;
        [SerializeField] float grapplePointPopVelocity = 7f;
        [SerializeField, Tooltip("ロックオン対象にこれ以上近いと前進しない")] float stopDistance = 1.4f;

        static readonly Collider[] OverlapBuffer = new Collider[32];
        readonly HashSet<Damageable> hitThisStep = new HashSet<Damageable>();

        SkillData current;
        int stepIndex;
        Phase phase;
        float phaseTime;
        float skillTime;
        bool chainQueued;
        bool isFinisher;
        bool hitsEnabled;
        bool styleRegisteredThisStep;
        bool justDodgeAwarded;
        float damageMultiplier = 1f;
        Vector3 moveDirection;

        float stunnedUntil = -1f;
        float extraInvulnerableUntil = -1f;
        float swapStrikeUntil = -1f;

        EnemyController travelEnemy;
        Vector3 travelTarget;
        bool travelToPoint;

        SlotType lastSwapCancelSlot;
        int swapCancelStreak;
        float lastSwapCancelTime = -99f;

        public SkillData Current => current;
        public bool IsBusy => current != null;
        public bool IsFinisherActive => current != null && isFinisher;
        public bool IsStunned => Time.time < stunnedUntil;
        public bool SwapStrikeReady => Time.time <= swapStrikeUntil;

        public string DebugLabel => current == null
            ? (IsStunned ? "被弾" : "待機")
            : $"{current.displayName} {stepIndex + 1}/{current.StepCount}段 [{phase}]";

        SkillStep Step => current.GetStep(stepIndex);

        void Update()
        {
            if (self.IsDead)
            {
                motor.LocomotionEnabled = false;
                return;
            }

            HandleSwaps();

            if (IsStunned)
            {
                motor.LocomotionEnabled = false;
            }
            else
            {
                if (current == null) TryStartFromInput();
                else TickSkill(Time.deltaTime);
                motor.LocomotionEnabled = current == null;
            }

            if (current == null) self.Invulnerable = Time.time < extraInvulnerableUntil;
            UpdateWeaponPose();
        }

        // ---------- 切り替え ----------

        void HandleSwaps()
        {
            TrySwap(SlotType.AttackA, PlayerAction.SwapAttackA);
            TrySwap(SlotType.AttackB, PlayerAction.SwapAttackB);
            TrySwap(SlotType.Movement, PlayerAction.SwapMovement);
        }

        void TrySwap(SlotType slot, PlayerAction action)
        {
            if (!input.Consume(action)) return;

            bool cancel = CanSwapCancel(slot);
            if (!slots.Cycle(slot)) return;

            swapStrikeUntil = Time.time + swapStrikeWindow;
            if (slot == SlotType.AttackA) weapons.SetMainWeapon(slots.GetCurrent(slot).weapon);

            if (cancel)
            {
                bool continuing = lastSwapCancelSlot == slot && Time.time - lastSwapCancelTime < swapCancelStreakReset;
                swapCancelStreak = continuing ? swapCancelStreak + 1 : 1;
                lastSwapCancelSlot = slot;
                lastSwapCancelTime = Time.time;
                EndSkill();
                style.AddBonus(style.Config.swapCancelBonus, "SWAP CANCEL", new Color(0.4f, 0.9f, 1f));
            }
        }

        bool CanSwapCancel(SlotType slot)
        {
            if (current == null || isFinisher || current.slot != slot || phase != Phase.Recovery) return false;

            bool continuing = lastSwapCancelSlot == slot && Time.time - lastSwapCancelTime < swapCancelStreakReset;
            int streak = continuing ? swapCancelStreak : 0;
            float recovery = FrameTime.ToSeconds(Step.recoveryFrames);
            float required = Mathf.Max(
                FrameTime.ToSeconds(current.swapCancelFrame),
                recovery * Mathf.Min(0.9f, swapCancelStreakPenalty * streak));
            return phaseTime >= required;
        }

        // ---------- 発動 ----------

        bool TryStartFromInput()
        {
            var finisher = slots.MasteryFinisher;
            if (finisher != null && input.Consume(PlayerAction.Finisher))
            {
                StartSkill(finisher, true);
                return true;
            }

            if (TryStartSlot(SlotType.Movement)) return true;
            if (TryStartSlot(SlotType.AttackA)) return true;
            if (TryStartSlot(SlotType.AttackB)) return true;

            if (input.Peek(PlayerAction.Jump) && motor.IsGrounded)
            {
                input.Consume(PlayerAction.Jump);
                if (current != null) EndSkill();
                return motor.TryJump();
            }
            return false;
        }

        bool TryStartSlot(SlotType slot)
        {
            var action = PlayerInputReader.ActionFor(slot);
            if (!input.Peek(action)) return false;

            var skill = slots.GetCurrent(slot);
            if (skill == null) return false;
            // 空中で使えない技は、着地まで先行入力として残す
            if (!skill.usableInAir && !motor.IsGrounded) return false;

            input.Consume(action);
            if (current != null) EndSkill();
            StartSkill(skill, false);
            return true;
        }

        void StartSkill(SkillData skill, bool finisher)
        {
            current = skill;
            isFinisher = finisher;
            skillTime = 0f;
            damageMultiplier = 1f;
            hitsEnabled = true;
            justDodgeAwarded = false;
            travelEnemy = null;
            travelToPoint = false;

            weapons.ShowTemporary(skill.weapon);

            var target = lockOn.Target;
            Vector3 desired = Flat(motor.DesiredDirection);
            if (skill.behavior == SkillBehavior.DashStrike)
                moveDirection = desired.sqrMagnitude > 0.01f ? desired.normalized : transform.forward;
            else if (target != null)
                moveDirection = DirectionTo(target.transform.position);
            else
                moveDirection = desired.sqrMagnitude > 0.01f ? desired.normalized : transform.forward;
            motor.FaceDirection(moveDirection);

            if (motor.IsGrounded)
            {
                motor.GravityScale = 1f;
            }
            else
            {
                motor.GravityScale = skill.airGravityScale;
                if (motor.VerticalVelocity < 0f) motor.SetVerticalVelocity(0f);
            }

            switch (skill.behavior)
            {
                case SkillBehavior.ChargeCounter:
                    phase = Phase.Charging;
                    phaseTime = 0f;
                    self.CounterHandler = OnCounter;
                    break;
                case SkillBehavior.Grapple:
                    BeginGrapple();
                    break;
                case SkillBehavior.Pull:
                    BeginPull();
                    break;
                default:
                    BeginStep(0);
                    break;
            }
        }

        void BeginStep(int index)
        {
            stepIndex = index;
            phase = Phase.Startup;
            phaseTime = 0f;
            chainQueued = false;
            styleRegisteredThisStep = false;
            hitThisStep.Clear();

            if (!motor.IsGrounded && current.behavior != SkillBehavior.HammerJump && motor.VerticalVelocity < 0f)
            {
                motor.SetVerticalVelocity(0f);
            }
        }

        // ---------- 実行中 ----------

        void TickSkill(float dt)
        {
            phaseTime += dt;
            skillTime += dt;

            self.Invulnerable = skillTime < FrameTime.ToSeconds(current.invulnerableFrames)
                                || Time.time < extraInvulnerableUntil
                                || (phase == Phase.Travel && current.behavior == SkillBehavior.Grapple);

            // 同じボタンで次の段を予約
            bool inStep = phase == Phase.Startup || phase == Phase.Active || phase == Phase.Recovery;
            if (inStep && !chainQueued && !current.autoChain && stepIndex + 1 < current.StepCount
                && input.Consume(PlayerInputReader.ActionFor(current.slot)))
            {
                chainQueued = true;
            }

            switch (phase)
            {
                case Phase.Startup:
                {
                    ApplyForward(dt);
                    float duration = FrameTime.ToSeconds(Step.startupFrames);
                    if (phaseTime >= duration)
                    {
                        phaseTime -= duration;
                        phase = Phase.Active;
                        OnActiveStart();
                    }
                    break;
                }
                case Phase.Active:
                {
                    ApplyForward(dt);
                    DoHitbox();
                    float duration = FrameTime.ToSeconds(Step.activeFrames);
                    if (phaseTime >= duration)
                    {
                        phaseTime -= duration;
                        if (current.behavior == SkillBehavior.HammerJump && stepIndex == 0)
                        {
                            motor.GravityScale = 1f;
                            motor.SetVerticalVelocity(current.jumpVelocity);
                            phase = Phase.Airborne;
                            phaseTime = 0f;
                        }
                        else
                        {
                            phase = Phase.Recovery;
                        }
                    }
                    break;
                }
                case Phase.Recovery:
                    TickRecovery();
                    break;
                case Phase.Charging:
                {
                    float max = FrameTime.ToSeconds(current.chargeFrames);
                    if (!input.IsHeld(PlayerInputReader.ActionFor(current.slot)) || phaseTime >= max)
                    {
                        ReleaseCharge(false);
                    }
                    break;
                }
                case Phase.Airborne:
                    motor.Displace(moveDirection * (current.dashDistance / 0.8f) * dt);
                    if (phaseTime > 0.1f && motor.IsGrounded)
                    {
                        if (current.StepCount > 1) BeginStep(1);
                        else EndSkill();
                    }
                    break;
                case Phase.Travel:
                    TickTravel(dt);
                    break;
                case Phase.PullWait:
                    if (travelEnemy == null || !travelEnemy.IsBeingPulled || phaseTime > 0.5f) BeginStep(0);
                    break;
            }
        }

        void TickRecovery()
        {
            var step = Step;
            float duration = FrameTime.ToSeconds(step.recoveryFrames);
            bool hasNext = stepIndex + 1 < current.StepCount;

            if (hasNext && (chainQueued || current.autoChain))
            {
                float chainAt = current.autoChain ? duration : FrameTime.ToSeconds(step.chainFrame);
                if (phaseTime >= chainAt)
                {
                    BeginStep(stepIndex + 1);
                    return;
                }
            }

            if (!isFinisher && phaseTime >= FrameTime.ToSeconds(step.cancelFrame) && TryStartFromInput()) return;

            if (phaseTime >= duration && !(hasNext && current.autoChain)) EndSkill();
        }

        void ApplyForward(float dt)
        {
            var step = Step;
            float total = FrameTime.ToSeconds(step.startupFrames + step.activeFrames);
            if (total <= 0f || Mathf.Approximately(step.forwardMove, 0f)) return;

            var target = lockOn.Target;
            if (target != null && current.behavior != SkillBehavior.DashStrike)
            {
                float distance = Flat(target.transform.position - transform.position).magnitude;
                if (distance <= stopDistance + target.Radius) return;
            }
            motor.Displace(moveDirection * (step.forwardMove * dt / total));
        }

        void OnActiveStart()
        {
            var step = Step;
            if (step.hitboxRadius >= 2.5f)
            {
                var data = slots.GetWeaponData(current.weapon);
                var center = transform.TransformPoint(step.hitboxOffset);
                if (CombatFeedback.Instance != null)
                {
                    CombatFeedback.Instance.SpawnShockwave(new Vector3(center.x, transform.position.y + 0.05f, center.z),
                        step.hitboxRadius, data != null ? data.color : Color.white);
                }
            }
            DoHitbox();
        }

        void DoHitbox()
        {
            var step = Step;
            if (!hitsEnabled || step.hitboxRadius <= 0f) return;

            Vector3 center = transform.TransformPoint(step.hitboxOffset);
            int count = Physics.OverlapSphereNonAlloc(center, step.hitboxRadius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);

            bool swapStrike = SwapStrikeReady;
            bool anyLanded = false;
            float hitstop = 0f;
            var bonus = slots.Bonus;
            var weaponData = slots.GetWeaponData(current.weapon);

            for (int i = 0; i < count; i++)
            {
                var target = OverlapBuffer[i].GetComponentInParent<Damageable>();
                if (target == null || target.Team == self.Team || target.IsDead || !hitThisStep.Add(target)) continue;

                float damage = step.damage * damageMultiplier;
                float stagger = step.stagger * damageMultiplier;
                if (bonus.Boosts(current.weapon) && weaponData != null) stagger *= 1f + weaponData.synergyStaggerBonus;
                if (swapStrike)
                {
                    damage *= swapStrikeDamageMultiplier;
                    stagger *= swapStrikeStaggerMultiplier;
                }

                var outcome = target.ApplyHit(new HitInfo
                {
                    damage = damage,
                    stagger = stagger,
                    armorBreak = step.armorBreak * damageMultiplier,
                    knockback = step.knockback,
                    launch = step.launch,
                    direction = DirectionTo(target.transform.position),
                    source = gameObject,
                });
                if (!outcome.Landed()) continue;

                anyLanded = true;
                hitstop = Mathf.Max(hitstop, step.hitstop * (outcome == HitOutcome.ArmorBroken ? 2f : 1f));

                if (!styleRegisteredThisStep)
                {
                    styleRegisteredThisStep = true;
                    style.RegisterHit($"{current.name}#{stepIndex}", current.weapon, step.stylePoints * damageMultiplier,
                        bonus.Kind == WeaponBonusKind.Arsenal);
                }
                else
                {
                    style.AddBonus(step.stylePoints * 0.3f);
                }

                if (outcome == HitOutcome.ArmorBroken) style.Announce("ARMOR BREAK", new Color(1f, 0.6f, 0.2f));

                if (CombatFeedback.Instance != null)
                {
                    var color = swapStrike ? new Color(1f, 0.9f, 0.2f)
                        : outcome == HitOutcome.Armored ? new Color(0.7f, 0.7f, 0.7f)
                        : Color.white;
                    CombatFeedback.Instance.SpawnDamageNumber(target.transform.position + Vector3.up * 2.2f, target.LastDamage, color);
                }
            }

            if (!anyLanded) return;
            if (swapStrike)
            {
                swapStrikeUntil = -1f;
                style.AddBonus(style.Config.swapStrikeBonus, "SWAP STRIKE!", new Color(1f, 0.9f, 0.2f));
            }
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.HitStop(hitstop);
                CombatFeedback.Instance.Shake(0.05f + hitstop);
            }
        }

        // ---------- 溜め・カウンター(居合) ----------

        bool OnCounter(HitInfo hit)
        {
            if (current == null || phase != Phase.Charging) return false;
            ReleaseCharge(true);
            return true;
        }

        void ReleaseCharge(bool counter)
        {
            self.CounterHandler = null;
            float max = FrameTime.ToSeconds(current.chargeFrames);
            float ratio = counter || max <= 0f ? 1f : Mathf.Clamp01(phaseTime / max);
            damageMultiplier = Mathf.Lerp(minChargeMultiplier, maxChargeMultiplier, ratio) * (counter ? counterMultiplier : 1f);

            if (counter)
            {
                extraInvulnerableUntil = Time.time + counterInvulnerableTime;
                style.AddBonus(style.Config.counterBonus, "COUNTER!", new Color(1f, 0.4f, 0.4f));
            }

            var target = lockOn.Target;
            if (target != null)
            {
                moveDirection = DirectionTo(target.transform.position);
                motor.FaceDirection(moveDirection);
            }
            BeginStep(0);
        }

        // ---------- ワイヤー・引き寄せ ----------

        void BeginGrapple()
        {
            motor.GravityScale = 0f;
            motor.SetVerticalVelocity(0f);

            var target = lockOn.Target;
            if (target != null && Vector3.Distance(target.transform.position, transform.position) <= current.range)
            {
                travelEnemy = target;
            }
            else if (TryFindGrapplePoint(out var point))
            {
                travelToPoint = true;
                travelTarget = point - Vector3.up * 1.8f;
            }
            else
            {
                travelTarget = transform.position + moveDirection * current.dashDistance + Vector3.up * 0.8f;
            }

            hitsEnabled = travelEnemy != null;
            phase = Phase.Travel;
            phaseTime = 0f;
        }

        bool TryFindGrapplePoint(out Vector3 position)
        {
            position = default;
            float bestScore = float.MaxValue;
            foreach (var point in GrapplePoint.All)
            {
                Vector3 to = point.transform.position - transform.position;
                float distance = to.magnitude;
                if (distance > current.range || distance < 1f) continue;
                float facing = Vector3.Dot(Flat(to).normalized, moveDirection);
                // 向いている方向にあるポイントを優先する
                float score = distance * (facing > 0.3f ? 1f : 4f);
                if (score < bestScore)
                {
                    bestScore = score;
                    position = point.transform.position;
                }
            }
            return bestScore < float.MaxValue;
        }

        void BeginPull()
        {
            var target = lockOn.Target;
            if (target == null || Vector3.Distance(target.transform.position, transform.position) > current.range)
            {
                target = FindEnemyInFront(current.range);
            }

            if (target == null)
            {
                BeginStep(0);
                return;
            }

            travelEnemy = target;
            moveDirection = DirectionTo(target.transform.position);
            motor.FaceDirection(moveDirection);

            if (target.IsHeavy)
            {
                // 重い敵には自分が飛びつく
                motor.GravityScale = 0f;
                motor.SetVerticalVelocity(0f);
                phase = Phase.Travel;
                phaseTime = 0f;
                return;
            }

            target.BeginPull(transform.position + moveDirection * 1.6f, current.moveSpeed);
            phase = Phase.PullWait;
            phaseTime = 0f;
        }

        void TickTravel(float dt)
        {
            motor.GravityScale = 0f;
            motor.SetVerticalVelocity(0f);

            if (travelEnemy != null && travelEnemy.IsDead)
            {
                travelEnemy = null;
                hitsEnabled = false;
                ArriveTravel();
                return;
            }

            Vector3 destination = travelEnemy != null ? ApproachPoint(travelEnemy) : travelTarget;
            Vector3 to = destination - transform.position;
            float stepLength = current.moveSpeed * dt;

            Vector3 flat = Flat(to);
            if (flat.sqrMagnitude > 0.01f)
            {
                moveDirection = flat.normalized;
                motor.FaceDirection(moveDirection);
            }

            if (to.magnitude <= stepLength + 0.3f || phaseTime >= maxTravelTime)
            {
                motor.Displace(Vector3.ClampMagnitude(to, stepLength));
                ArriveTravel();
            }
            else
            {
                motor.Displace(to.normalized * stepLength);
            }
        }

        void ArriveTravel()
        {
            if (travelToPoint) motor.SetVerticalVelocity(grapplePointPopVelocity);
            motor.GravityScale = current.airGravityScale;
            if (travelEnemy != null) motor.FaceDirection(DirectionTo(travelEnemy.transform.position));
            BeginStep(0);
        }

        Vector3 ApproachPoint(EnemyController enemy)
        {
            Vector3 away = Flat(transform.position - enemy.transform.position);
            away = away.sqrMagnitude > 0.01f ? away.normalized : -transform.forward;
            return enemy.transform.position + away * (enemy.Radius + 1.0f);
        }

        EnemyController FindEnemyInFront(float range)
        {
            EnemyController best = null;
            float bestDistance = float.MaxValue;
            foreach (var enemy in EnemyController.Active)
            {
                if (enemy.IsDead) continue;
                Vector3 to = enemy.transform.position - transform.position;
                float distance = to.magnitude;
                if (distance > range || Vector3.Dot(Flat(to).normalized, moveDirection) < 0.5f) continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }
            return best;
        }

        // ---------- 終了・割り込み ----------

        void EndSkill()
        {
            self.CounterHandler = null;
            current = null;
            phase = Phase.None;
            isFinisher = false;
            chainQueued = false;
            travelEnemy = null;
            motor.GravityScale = 1f;
            self.Invulnerable = Time.time < extraInvulnerableUntil;
            weapons.ClearTemporary();
        }

        /// <summary>被弾などで技を中断する</summary>
        public void Interrupt(float stunDuration, float invulnerableDuration)
        {
            if (current != null) EndSkill();
            stunnedUntil = Time.time + stunDuration;
            extraInvulnerableUntil = Time.time + invulnerableDuration;
        }

        /// <summary>移動スキルの無敵で攻撃をかわしたとき、1回の発動につき1度だけ true を返す</summary>
        public bool TryConsumeJustDodge()
        {
            if (current == null || current.slot != SlotType.Movement || justDodgeAwarded) return false;
            justDodgeAwarded = true;
            return true;
        }

        // ---------- 見た目 ----------

        void UpdateWeaponPose()
        {
            if (current == null)
            {
                weapons.SetIdle();
                return;
            }

            var step = Step;
            float normalized = phase switch
            {
                Phase.Startup => -Progress(step.startupFrames),
                Phase.Active => Mathf.Lerp(-1f, 1f, Progress(step.activeFrames)),
                Phase.Recovery => Mathf.Lerp(1f, 0f, Progress(step.recoveryFrames)),
                Phase.Charging => -1f,
                _ => -0.6f,
            };
            weapons.SetSwing(normalized, current.weapon == WeaponType.Hammer, stepIndex % 2 == 0 ? 1f : -1f);
        }

        float Progress(int frames) => frames <= 0 ? 1f : Mathf.Clamp01(phaseTime / FrameTime.ToSeconds(frames));

        Vector3 DirectionTo(Vector3 position)
        {
            Vector3 direction = Flat(position - transform.position);
            return direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
