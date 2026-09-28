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
            Hang,
        }

        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] SkillSlotController slots;
        [SerializeField] WeaponHolder weapons;
        [SerializeField] LockOnSystem lockOn;
        [SerializeField] StyleRankSystem style;
        [SerializeField] Damageable self;
        [SerializeField, Tooltip("ワイヤーの対象を「視界内」で選ぶためのカメラ")] Camera view;

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
        [SerializeField, Tooltip("移動が壁などで止まったときに打ち切るまでの余裕(秒)")] float travelTimeMargin = 0.3f;
        [SerializeField, Tooltip("ロックオン対象にこれ以上近いと前進しない")] float stopDistance = 1.4f;

        [Header("ワイヤー")]
        [SerializeField, Tooltip("ポイントに着いてからぶら下がる秒数")] float grappleHangTime = 0.6f;
        [SerializeField] float grappleJumpVelocity = 11f;
        [SerializeField] float grappleJumpForward = 6f;
        [SerializeField, Tooltip("画面端のこの割合はポイントの対象から外す")] float grappleViewMargin = 0.05f;
        [SerializeField, Tooltip("これより近いポイントは「今ぶら下がっているポイント」とみなして対象から外す")] float grappleMinDistance = 2.5f;

        static readonly Collider[] OverlapBuffer = new Collider[32];
        readonly HashSet<Damageable> hitThisStep = new HashSet<Damageable>();
        readonly List<Damageable> targetBuffer = new List<Damageable>();

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
        /// <summary>切り替えるたびに増える。弾が「どの切り替えのスワップストライクか」を覚えておくのに使う</summary>
        int swapStrikeId;

        EnemyController travelEnemy;
        Vector3 travelTarget;
        bool travelToPoint;
        float travelTimeout;

        SlotType lastSwapCancelSlot;
        int swapCancelStreak;
        float lastSwapCancelTime = -99f;

        public SkillData Current => current;
        public bool IsBusy => current != null;
        public bool IsFinisherActive => current != null && isFinisher;
        public bool IsStunned => Time.time < stunnedUntil;
        public bool SwapStrikeReady => Time.time <= swapStrikeUntil;
        /// <summary>スワップストライクの残り時間(1 → 0)</summary>
        public float SwapStrikeRemaining => swapStrikeWindow <= 0f ? 0f : Mathf.Clamp01((swapStrikeUntil - Time.time) / swapStrikeWindow);
        /// <summary>移動スロットがワイヤーのとき、今押したら飛ぶポイント(HUD の表示用)</summary>
        public GrapplePoint GrapplePreview { get; private set; }

        /// <summary>今このスロットを切り替えるとスワップキャンセルになるか(HUD の表示用)</summary>
        public bool CanSwapCancelNow(SlotType slot) => CanSwapCancel(slot);

        public string DebugLabel => current == null
            ? (IsStunned ? "被弾" : "待機")
            : $"{current.displayName} {stepIndex + 1}/{current.StepCount}段 [{phase}]";

        SkillStep Step => current.GetStep(stepIndex);

        void Update()
        {
            if (GamePause.IsPaused) return;
            // ステージ選択中は技も切り替えも受け付けない(メニュー操作のキーで技が出ないように)
            // (先行入力は消さない。編成画面を開く P などは別のコンポーネントが読む)
            if (ArenaDirector.Instance != null && ArenaDirector.Instance.State == ArenaDirector.GameState.StageSelect) return;
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

            var movement = slots.GetCurrent(SlotType.Movement);
            GrapplePreview = movement != null && movement.behavior == SkillBehavior.Grapple ? FindGrapplePoint(movement.range) : null;
        }

        // ---------- 切り替え ----------

        void HandleSwaps()
        {
            if (slots.Mode == SwapMode.Rack)
            {
                TrySwap(SlotType.AttackA, PlayerAction.Swap1);
                TrySwap(SlotType.AttackB, PlayerAction.Swap2);
                TrySwap(SlotType.Movement, PlayerAction.Swap3);
                input.Consume(PlayerAction.Swap4);
                return;
            }

            for (int i = 0; i < SkillSlotController.PresetCount; i++)
            {
                if (input.Consume(PlayerInputReader.SwapAction(i))) TrySelectPreset(i);
            }
        }

        /// <summary>ラック方式: 1スロットだけ切り替える</summary>
        void TrySwap(SlotType slot, PlayerAction action)
        {
            if (!input.Consume(action)) return;

            bool cancel = CanSwapCancel(slot);
            if (!slots.Cycle(slot)) return;

            OnSwapped(slots.GetCurrent(slot), slot == SlotType.AttackA);
            if (cancel) SwapCancel(slot);
        }

        /// <summary>プリセット方式: 3スロットをまとめて切り替える</summary>
        void TrySelectPreset(int index)
        {
            bool cancel = CanPresetCancel(index);
            SlotType? cancelSlot = current != null ? current.slot : null;
            if (!slots.SelectPreset(index)) return;

            OnSwapped(slots.GetCurrent(SlotType.AttackA), true);
            if (cancel && cancelSlot.HasValue) SwapCancel(cancelSlot.Value);
        }

        void OnSwapped(SkillData shown, bool updateMainWeapon)
        {
            swapStrikeUntil = Time.time + swapStrikeWindow;
            swapStrikeId++;
            if (shown == null) return;
            if (updateMainWeapon) weapons.SetMainWeapon(shown.weapon);

            // 切り替えた武器種の色で足元を光らせる
            var weaponData = slots.GetWeaponData(shown.weapon);
            if (CombatFeedback.Instance != null && weaponData != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, 1.4f, weaponData.color);
            }
        }

        void SwapCancel(SlotType slot)
        {
            bool continuing = lastSwapCancelSlot == slot && Time.time - lastSwapCancelTime < swapCancelStreakReset;
            swapCancelStreak = continuing ? swapCancelStreak + 1 : 1;
            lastSwapCancelSlot = slot;
            lastSwapCancelTime = Time.time;
            EndSkill();
            style.AddBonus(style.Config.swapCancelBonus, "SWAP CANCEL", new Color(0.4f, 0.9f, 1f));
        }

        /// <summary>このプリセットに切り替えると、実行中の技のスロットの中身が変わってスワップキャンセルになるか</summary>
        public bool CanPresetCancel(int index)
        {
            if (current == null || slots.Mode != SwapMode.Preset || index == slots.PresetIndex) return false;
            if (index < 0 || index >= slots.Presets.Count) return false;
            if (slots.Presets[index].Get(current.slot) == current) return false;
            return CanSwapCancel(current.slot);
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
            if (skill.behavior == SkillBehavior.DashStrike || skill.behavior == SkillBehavior.Blink)
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
                case SkillBehavior.Blink:
                    Blink();
                    BeginStep(0);
                    break;
                case SkillBehavior.Boost:
                    motor.GravityScale = skill.airGravityScale;
                    motor.SetVerticalVelocity(skill.jumpVelocity);
                    BeginStep(0);
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
                case Phase.Hang:
                    TickHang();
                    break;
            }
        }

        /// <summary>ポイントにぶら下がっている間。次のワイヤー・ジャンプ・空中攻撃につなげられる。</summary>
        void TickHang()
        {
            motor.GravityScale = 0f;
            motor.SetVerticalVelocity(0f);

            if (input.Consume(PlayerAction.Jump))
            {
                Vector3 forward = Flat(motor.DesiredDirection);
                if (forward.sqrMagnitude < 0.01f) forward = transform.forward;
                EndSkill();
                motor.SetVerticalVelocity(grappleJumpVelocity);
                motor.AddImpulse(forward.normalized * grappleJumpForward);
                return;
            }

            if (TryStartFromInput()) return;
            if (phaseTime >= grappleHangTime) EndSkill();
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
            var data = slots.GetWeaponData(current.weapon);
            var color = data != null ? data.color : Color.white;

            switch (current.behavior)
            {
                case SkillBehavior.Projectile:
                    FireProjectiles(color);
                    return;
                case SkillBehavior.TargetedStrike:
                    FireStrike(color);
                    return;
                case SkillBehavior.Beam:
                    if (CombatFeedback.Instance != null)
                    {
                        GetBeam(out var from, out var to);
                        CombatFeedback.Instance.SpawnBeam(from, to, current.beamRadius, color,
                            Mathf.Max(0.05f, FrameTime.ToSeconds(step.activeFrames)));
                    }
                    break;
            }

            if (step.hitboxRadius >= 2.5f && current.behavior != SkillBehavior.Beam && CombatFeedback.Instance != null)
            {
                var center = transform.TransformPoint(step.hitboxOffset);
                CombatFeedback.Instance.SpawnShockwave(new Vector3(center.x, transform.position.y + 0.05f, center.z),
                    step.hitboxRadius, color);
            }
            DoHitbox();
        }

        SkillHitContext CurrentContext() => new SkillHitContext
        {
            skill = current,
            stepIndex = stepIndex,
            multiplier = damageMultiplier,
            swapStrikeId = SwapStrikeReady ? swapStrikeId : -1,
        };

        void DoHitbox()
        {
            var step = Step;
            if (!hitsEnabled) return;
            if (current.behavior == SkillBehavior.Projectile || current.behavior == SkillBehavior.TargetedStrike) return;

            int count;
            if (current.behavior == SkillBehavior.Beam)
            {
                GetBeam(out var from, out var to);
                count = Physics.OverlapCapsuleNonAlloc(from, to, current.beamRadius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            }
            else
            {
                if (step.hitboxRadius <= 0f) return;
                Vector3 center = transform.TransformPoint(step.hitboxOffset);
                count = Physics.OverlapSphereNonAlloc(center, step.hitboxRadius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            }

            targetBuffer.Clear();
            for (int i = 0; i < count; i++)
            {
                var target = OverlapBuffer[i].GetComponentInParent<Damageable>();
                if (target == null || target.Team == self.Team || target.IsDead || !hitThisStep.Add(target)) continue;
                targetBuffer.Add(target);
            }
            if (targetBuffer.Count > 0) ResolveHits(CurrentContext(), targetBuffer, transform.position);
        }

        /// <summary>
        /// 技のヒットをまとめて処理する(近接の判定・弾・爆発のすべてがここを通る)。
        /// ダメージ、武器種ボーナス、スワップストライク、スタイル、演出を反映する。
        /// </summary>
        public void ResolveHits(SkillHitContext context, List<Damageable> targets, Vector3 sourcePosition)
        {
            if (context.skill == null || targets.Count == 0) return;

            var skill = context.skill;
            var step = context.Step;
            bool swapStrike = context.swapStrikeId >= 0 && context.swapStrikeId == swapStrikeId && SwapStrikeReady;
            bool anyLanded = false;
            bool styleRegistered = false;
            float hitstop = 0f;
            var bonus = slots.Bonus;
            var weaponData = slots.GetWeaponData(skill.weapon);

            foreach (var target in targets)
            {
                if (target == null || target.IsDead) continue;

                float damage = step.damage * context.multiplier;
                float stagger = step.stagger * context.multiplier;
                if (bonus.Boosts(skill.weapon) && weaponData != null) stagger *= 1f + weaponData.synergyStaggerBonus;
                if (swapStrike)
                {
                    damage *= swapStrikeDamageMultiplier;
                    stagger *= swapStrikeStaggerMultiplier;
                }

                Vector3 direction = Flat(target.transform.position - sourcePosition);
                var outcome = target.ApplyHit(new HitInfo
                {
                    damage = damage,
                    stagger = stagger,
                    armorBreak = step.armorBreak * context.multiplier,
                    knockback = step.knockback,
                    launch = step.launch,
                    direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward,
                    source = gameObject,
                });
                if (!outcome.Landed()) continue;

                anyLanded = true;
                hitstop = Mathf.Max(hitstop, step.hitstop * (outcome == HitOutcome.ArmorBroken ? 2f : 1f));

                // 近接の1段は1回だけ登録する。同じ段で複数の敵に当てた分は少しだけ加点
                bool firstForStep = context.skill == current && context.stepIndex == stepIndex ? !styleRegisteredThisStep : !styleRegistered;
                if (firstForStep)
                {
                    if (context.skill == current && context.stepIndex == stepIndex) styleRegisteredThisStep = true;
                    styleRegistered = true;
                    style.RegisterHit($"{skill.name}#{context.stepIndex}", skill.weapon, step.stylePoints * context.multiplier,
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

        // ---------- 飛び道具・ビーム ----------

        /// <summary>弾を撃つ方向。ロックオン中は対象の胸へ、それ以外は向いている方向へ水平に。</summary>
        Vector3 AimDirection(Vector3 origin)
        {
            var target = lockOn.Target;
            if (target != null && !target.IsDead)
            {
                Vector3 to = target.CenterPoint - origin;
                if (to.sqrMagnitude > 0.01f) return to.normalized;
            }
            return transform.forward;
        }

        Vector3 MuzzlePosition => transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;

        void FireProjectiles(Color color)
        {
            var context = CurrentContext();
            Vector3 origin = MuzzlePosition;
            Vector3 aim = AimDirection(origin);
            int count = Mathf.Max(1, current.projectileCount);
            bool fullCircle = current.projectileSpread >= 359f;

            for (int i = 0; i < count; i++)
            {
                float angle;
                if (count == 1) angle = 0f;
                else if (fullCircle) angle = 360f * i / count;
                else angle = Mathf.Lerp(-current.projectileSpread * 0.5f, current.projectileSpread * 0.5f, i / (count - 1f));

                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * aim;
                Vector3 spawn = fullCircle ? transform.position + Vector3.up * 1.2f + direction * 0.6f : origin;
                SkillProjectile.Spawn(this, context, spawn, direction * current.projectileSpeed, lockOn.Target, color);
            }
        }

        void FireStrike(Color color)
        {
            var target = lockOn.Target;
            Vector3 position;
            if (target != null && !target.IsDead && Vector3.Distance(target.transform.position, transform.position) <= current.strikeDistance * 2f)
            {
                position = target.transform.position;
            }
            else
            {
                position = transform.position + transform.forward * current.strikeDistance;
                // 地面の高さに合わせる
                if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out var ground, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    position = ground.point;
                }
            }
            SkillProjectile.Spawn(this, CurrentContext(), position + Vector3.up * 0.1f, Vector3.zero, null, color, current.strikeDelay);
        }

        void GetBeam(out Vector3 from, out Vector3 to)
        {
            from = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
            Vector3 direction = AimDirection(from);
            float length = current.beamLength;
            if (Physics.Raycast(from, direction, out var wall, length, ~0, QueryTriggerInteraction.Ignore)
                && wall.collider.GetComponentInParent<Damageable>() == null)
            {
                length = wall.distance;
            }
            to = from + direction * length;
        }

        // ---------- ブリンク ----------

        void Blink()
        {
            Vector3 direction = moveDirection;
            float distance = current.dashDistance;
            Vector3 bottom = transform.position + Vector3.up * 0.5f;
            Vector3 top = transform.position + Vector3.up * 1.5f;
            if (Physics.CapsuleCast(bottom, top, 0.35f, direction, out var hit, distance, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                distance = Mathf.Max(0f, hit.distance - 0.2f);
            }

            var data = slots.GetWeaponData(current.weapon);
            var color = data != null ? data.color : Color.white;
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, 1.2f, color);
            motor.Teleport(direction * distance);
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position + direction * distance, 1.6f, color);
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

            // 優先順: 視界内で一番近いポイント → ロックオン中の敵 → 空中ダッシュ
            var point = FindGrapplePoint(current.range);
            var target = lockOn.Target;
            if (point != null)
            {
                travelToPoint = true;
                travelTarget = point.transform.position - Vector3.up * 1.8f;
            }
            else if (target != null && Vector3.Distance(target.transform.position, transform.position) <= current.range)
            {
                travelEnemy = target;
            }
            else
            {
                travelTarget = transform.position + moveDirection * current.dashDistance + Vector3.up * 0.8f;
            }

            Vector3 destination = travelEnemy != null ? travelEnemy.transform.position : travelTarget;
            BeginTravel(destination);
            hitsEnabled = travelEnemy != null;
        }

        /// <summary>
        /// 視界内(カメラに映っていて、途中に遮るものがない)にあるグラップルポイントのうち、一番近いもの。
        /// 今ぶら下がっているポイントは除く。
        /// </summary>
        GrapplePoint FindGrapplePoint(float range)
        {
            Vector3 chest = transform.position + Vector3.up;
            GrapplePoint best = null;
            float bestDistance = float.MaxValue;
            foreach (var point in GrapplePoint.All)
            {
                Vector3 position = point.transform.position;
                float distance = Vector3.Distance(chest, position);
                if (distance > range || distance < grappleMinDistance || distance >= bestDistance) continue;
                if (!IsInView(position)) continue;
                if (Physics.Linecast(chest, position, out var hit, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }
                best = point;
                bestDistance = distance;
            }
            return best;
        }

        bool IsInView(Vector3 position)
        {
            if (view == null) return Vector3.Dot(Flat(position - transform.position).normalized, transform.forward) > 0.3f;
            Vector3 viewport = view.WorldToViewportPoint(position);
            return viewport.z > 0f
                   && viewport.x >= grappleViewMargin && viewport.x <= 1f - grappleViewMargin
                   && viewport.y >= grappleViewMargin && viewport.y <= 1f - grappleViewMargin;
        }

        void BeginTravel(Vector3 destination)
        {
            phase = Phase.Travel;
            phaseTime = 0f;
            float speed = Mathf.Max(1f, current.moveSpeed);
            travelTimeout = Vector3.Distance(transform.position, destination) / speed + travelTimeMargin;
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
                BeginTravel(target.transform.position);
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

            if (to.magnitude <= stepLength + 0.3f || phaseTime >= travelTimeout)
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
            if (travelToPoint)
            {
                phase = Phase.Hang;
                phaseTime = 0f;
                return;
            }

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
