using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        [SerializeField, FormerlySerializedAs("input")] PlayerInputReader mInput;
        [SerializeField, FormerlySerializedAs("motor")] PlayerMotor mMotor;
        [SerializeField, FormerlySerializedAs("slots")] SkillSlotController mSlots;
        [SerializeField, FormerlySerializedAs("weapons")] WeaponHolder mWeapons;
        [SerializeField, FormerlySerializedAs("lockOn")] LockOnSystem mLockOn;
        [SerializeField, FormerlySerializedAs("style")] StyleRankSystem mStyle;
        [SerializeField, FormerlySerializedAs("self")] Damageable mSelf;
        [SerializeField, Tooltip("ワイヤーの対象を「視界内」で選ぶためのカメラ"), FormerlySerializedAs("view")] Camera mView;

        [Header("スワップ")]
        [SerializeField, FormerlySerializedAs("swapStrikeWindow")] float mSwapStrikeWindow = 1f;
        [SerializeField, FormerlySerializedAs("swapStrikeDamageMultiplier")] float mSwapStrikeDamageMultiplier = 1.3f;
        [SerializeField, FormerlySerializedAs("swapStrikeStaggerMultiplier")] float mSwapStrikeStaggerMultiplier = 1.5f;
        [SerializeField, Tooltip("同じスロットで連続してスワップキャンセルすると、硬直のこの割合×回数だけ受付が遅くなる"), FormerlySerializedAs("swapCancelStreakPenalty")]
        float mSwapCancelStreakPenalty = 0.35f;
        [SerializeField, FormerlySerializedAs("swapCancelStreakReset")] float mSwapCancelStreakReset = 1.5f;

        [Header("切り替えのメリット: 弱点")]
        [SerializeField, FormerlySerializedAs("weaknessDamageMultiplier")] float mWeaknessDamageMultiplier = 1.5f;
        [SerializeField, FormerlySerializedAs("weaknessStaggerMultiplier")] float mWeaknessStaggerMultiplier = 1.5f;
        [SerializeField, FormerlySerializedAs("weaknessStyleBonus")] float mWeaknessStyleBonus = 10f;

        [Header("切り替えのメリット: チェンジチェイン")]
        [SerializeField, Tooltip("切り替えてから何秒以内に当てるとチェインがつながるか"), FormerlySerializedAs("chainLinkWindow")] float mChainLinkWindow = 2.5f;
        [SerializeField, Tooltip("最後につないでから何秒で途切れるか"), FormerlySerializedAs("chainKeepTime")] float mChainKeepTime = 4f;
        [SerializeField, FormerlySerializedAs("maxChain")] int mMaxChain = 5;
        [SerializeField, Tooltip("1段ごとのダメージ増加"), FormerlySerializedAs("chainBonusPerStep")] float mChainBonusPerStep = 0.1f;
        [SerializeField, FormerlySerializedAs("chainStyleBonusPerStep")] float mChainStyleBonusPerStep = 12f;

        [Header("切り替えのメリット: チェンジアタック")]
        [SerializeField, Tooltip("切り替えた瞬間に、新しい武器で周りを攻撃する"), FormerlySerializedAs("changeAttackEnabled")] bool mChangeAttackEnabled = true;
        [SerializeField, FormerlySerializedAs("changeAttackCooldown")] float mChangeAttackCooldown = 3f;
        [SerializeField, FormerlySerializedAs("changeAttackRadius")] float mChangeAttackRadius = 2.6f;
        [SerializeField, FormerlySerializedAs("changeAttackDamage")] float mChangeAttackDamage = 10f;
        [SerializeField, FormerlySerializedAs("changeAttackStagger")] float mChangeAttackStagger = 30f;
        [SerializeField, FormerlySerializedAs("changeAttackArmorBreak")] float mChangeAttackArmorBreak = 10f;
        [SerializeField, FormerlySerializedAs("changeAttackKnockback")] float mChangeAttackKnockback = 6f;
        [SerializeField, FormerlySerializedAs("changeAttackStylePoints")] float mChangeAttackStylePoints = 20f;

        [Header("チェンジドライブ(切り替えの必殺技)")]
        [SerializeField, Tooltip("周りのこの半径の敵に、各プリセットの武器で順に攻撃する"), FormerlySerializedAs("driveRadius")] float mDriveRadius = 6.5f;
        [SerializeField, Tooltip("1回ごとの威力(攻撃A の1段目に掛ける)"), FormerlySerializedAs("driveMultiplier")] float mDriveMultiplier = 2.4f;
        [SerializeField, Tooltip("次の武器に持ち替えるまでの秒数"), FormerlySerializedAs("driveInterval")] float mDriveInterval = 0.2f;

        [Header("溜め・カウンター")]
        [SerializeField, FormerlySerializedAs("minChargeMultiplier")] float mMinChargeMultiplier = 0.6f;
        [SerializeField, FormerlySerializedAs("maxChargeMultiplier")] float mMaxChargeMultiplier = 1.5f;
        [SerializeField, FormerlySerializedAs("counterMultiplier")] float mCounterMultiplier = 2f;
        [SerializeField, FormerlySerializedAs("counterInvulnerableTime")] float mCounterInvulnerableTime = 0.5f;

        [Header("移動系")]
        [SerializeField, Tooltip("移動が壁などで止まったときに打ち切るまでの余裕(秒)"), FormerlySerializedAs("travelTimeMargin")] float mTravelTimeMargin = 0.3f;
        [SerializeField, Tooltip("ロックオン対象にこれ以上近いと前進しない"), FormerlySerializedAs("stopDistance")] float mStopDistance = 1.4f;

        [Header("ワイヤー")]
        [SerializeField, Tooltip("ポイントに着いてからぶら下がる秒数"), FormerlySerializedAs("grappleHangTime")] float mGrappleHangTime = 0.6f;
        [SerializeField, FormerlySerializedAs("grappleJumpVelocity")] float mGrappleJumpVelocity = 11f;
        [SerializeField, FormerlySerializedAs("grappleJumpForward")] float mGrappleJumpForward = 6f;
        [SerializeField, Tooltip("画面端のこの割合はポイントの対象から外す"), FormerlySerializedAs("grappleViewMargin")] float mGrappleViewMargin = 0.05f;
        [SerializeField, Tooltip("これより近いポイントは「今ぶら下がっているポイント」とみなして対象から外す"), FormerlySerializedAs("grappleMinDistance")] float mGrappleMinDistance = 2.5f;

        static readonly Collider[] mOverlapBuffer = new Collider[32];
        readonly HashSet<Damageable> mHitThisStep = new HashSet<Damageable>();
        readonly List<Damageable> mTargetBuffer = new List<Damageable>();

        SkillData mCurrent;
        int mStepIndex;
        Phase mPhase;
        float mPhaseTime;
        float mSkillTime;
        bool mChainQueued;
        bool mIsFinisher;
        bool mHitsEnabled;
        bool mStyleRegisteredThisStep;
        bool mJustDodgeAwarded;
        float mDamageMultiplier = 1f;
        Vector3 mMoveDirection;

        float mStunnedUntil = -1f;
        float mExtraInvulnerableUntil = -1f;
        float mSwapStrikeUntil = -1f;
        /// <summary>切り替えるたびに増える。弾が「どの切り替えのスワップストライクか」を覚えておくのに使う</summary>
        int mSwapStrikeId;
        SwapChainMeter mChain;
        float mChangeAttackReadyAt;
        float mLastWeakAnnounce = -99f;

        EnemyController mTravelEnemy;
        Vector3 mTravelTarget;
        bool mTravelToPoint;
        float mTravelTimeout;

        SlotType mLastSwapCancelSlot;
        int mSwapCancelStreak;
        float mLastSwapCancelTime = -99f;

        public SkillData Current => mCurrent;
        public bool IsBusy => mCurrent != null;
        public bool IsFinisherActive => mCurrent != null && mIsFinisher;
        public bool IsStunned => Time.time < mStunnedUntil;
        public bool SwapStrikeReady => Time.time <= mSwapStrikeUntil;
        /// <summary>スワップストライクの残り時間(1 → 0)</summary>
        public float SwapStrikeRemaining => mSwapStrikeWindow <= 0f ? 0f : Mathf.Clamp01((mSwapStrikeUntil - Time.time) / mSwapStrikeWindow);
        /// <summary>移動スロットがワイヤーのとき、今押したら飛ぶポイント(HUD の表示用)</summary>
        public GrapplePoint GrapplePreview { get; private set; }

        public SwapChainMeter SwapChain => mChain ??= new SwapChainMeter(mChainLinkWindow, mChainKeepTime, mMaxChain, mChainBonusPerStep);
        /// <summary>途切れずに当てた数</summary>
        public ComboCounter Combo { get; } = new ComboCounter();
        /// <summary>チェンジドライブのゲージ(切り替えて当てるとたまる)</summary>
        public DriveGauge Drive { get; } = new DriveGauge();
        /// <summary>チェンジドライブを出している最中</summary>
        public bool IsDriving { get; private set; }
        public bool ChangeAttackReady => mChangeAttackEnabled && Time.time >= mChangeAttackReadyAt;
        /// <summary>チェンジアタックの準備(0 → 1 で使える)</summary>
        public float ChangeAttackCharge => !mChangeAttackEnabled || mChangeAttackCooldown <= 0f
            ? 1f
            : Mathf.Clamp01(1f - (mChangeAttackReadyAt - Time.time) / mChangeAttackCooldown);

        /// <summary>今このスロットを切り替えるとスワップキャンセルになるか(HUD の表示用)</summary>
        public bool CanSwapCancelNow(SlotType iSlot) => CanSwapCancel(iSlot);

        public string DebugLabel => mCurrent == null
            ? (IsStunned ? "被弾" : "待機")
            : $"{mCurrent.DisplayName} {mStepIndex + 1}/{mCurrent.StepCount}段 [{mPhase}]";

        SkillStep mStep => mCurrent.GetStep(mStepIndex);

        void Update()
        {
            if (GamePause.IsPaused) return;
            // ステージ選択中は技も切り替えも受け付けない(メニュー操作のキーで技が出ないように)
            // (先行入力は消さない。編成画面を開く P などは別のコンポーネントが読む)
            if (ArenaDirector.Instance != null && ArenaDirector.Instance.State == ArenaDirector.GameState.StageSelect) return;
            if (mSelf.IsDead)
            {
                mMotor.LocomotionEnabled = false;
                return;
            }

            SwapChain.Tick(Time.time);
            TickCombo();

            // チェンジドライブ中は、ほかの技や切り替えを受け付けない
            if (IsDriving)
            {
                mMotor.LocomotionEnabled = false;
                mSelf.Invulnerable = true;
                return;
            }
            if (mInput.Consume(PlayerAction.Drive) && Drive.IsFull && !IsStunned) StartCoroutine(ChangeDrive());

            HandleSwaps();

            if (IsStunned)
            {
                mMotor.LocomotionEnabled = false;
            }
            else
            {
                if (mCurrent == null) TryStartFromInput();
                else TickSkill(Time.deltaTime);
                mMotor.LocomotionEnabled = mCurrent == null;
            }

            if (mCurrent == null) mSelf.Invulnerable = Time.time < mExtraInvulnerableUntil;
            UpdateWeaponPose();

            var aMovement = mSlots.GetCurrent(SlotType.Movement);
            GrapplePreview = aMovement != null && aMovement.Behavior == SkillBehavior.Grapple ? FindGrapplePoint(aMovement.Range) : null;
        }

        // ---------- 切り替え ----------

        void HandleSwaps()
        {
            if (mSlots.Mode == SwapMode.Rack)
            {
                TrySwap(SlotType.AttackA, PlayerAction.Swap1);
                TrySwap(SlotType.AttackB, PlayerAction.Swap2);
                TrySwap(SlotType.Movement, PlayerAction.Swap3);
                mInput.Consume(PlayerAction.Swap4);
                return;
            }

            for (int aI = 0; aI < SkillSlotController.PresetCount; aI++)
            {
                if (mInput.Consume(PlayerInputReader.SwapAction(aI))) TrySelectPreset(aI);
            }
        }

        /// <summary>ラック方式: 1スロットだけ切り替える</summary>
        void TrySwap(SlotType iSlot, PlayerAction iAction)
        {
            if (!mInput.Consume(iAction)) return;

            bool aCancel = CanSwapCancel(iSlot);
            if (!mSlots.Cycle(iSlot)) return;

            OnSwapped(mSlots.GetCurrent(iSlot), iSlot == SlotType.AttackA);
            if (aCancel) SwapCancel(iSlot);
        }

        /// <summary>プリセット方式: 3スロットをまとめて切り替える</summary>
        void TrySelectPreset(int iIndex)
        {
            bool aCancel = CanPresetCancel(iIndex);
            SlotType? aCancelSlot = mCurrent != null ? mCurrent.Slot : null;
            if (!mSlots.SelectPreset(iIndex)) return;

            OnSwapped(mSlots.GetCurrent(SlotType.AttackA), true);
            if (aCancel && aCancelSlot.HasValue) SwapCancel(aCancelSlot.Value);
        }

        void OnSwapped(SkillData iShown, bool iUpdateMainWeapon)
        {
            mSwapStrikeUntil = Time.time + mSwapStrikeWindow;
            mSwapStrikeId++;
            SwapChain.OnSwap(Time.time);
            if (iShown == null) return;
            if (iUpdateMainWeapon) mWeapons.SetMainWeapon(iShown.Weapon);

            // 切り替えた武器種の色で足元を光らせる
            var aWeaponData = mSlots.GetWeaponData(iShown.Weapon);
            if (CombatFeedback.Instance != null && aWeaponData != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, 1.4f, aWeaponData.Color);
            }

            TryChangeAttack(iShown.Weapon);
        }

        /// <summary>
        /// チェンジアタック: 切り替えた瞬間、新しい武器で周りを一撃する(クールダウンあり)。
        /// 囲まれたときに切り替えで押し返せるようにする。これ自体はチェインを上げない。
        /// </summary>
        void TryChangeAttack(WeaponType iWeapon)
        {
            if (!ChangeAttackReady) return;
            mChangeAttackReadyAt = Time.time + mChangeAttackCooldown;

            var aData = mSlots.GetWeaponData(iWeapon);
            var aColor = aData != null ? aData.Color : Color.white;
            Vector3 aCenter = transform.position + Vector3.up;
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, mChangeAttackRadius, aColor);

            int aCount = Physics.OverlapSphereNonAlloc(aCenter, mChangeAttackRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            bool aAnyLanded = false;
            bool aAnyWeak = false;
            mTargetBuffer.Clear();
            for (int aI = 0; aI < aCount; aI++)
            {
                var aTarget = mOverlapBuffer[aI].GetComponentInParent<Damageable>();
                if (aTarget == null || aTarget.Team == mSelf.Team || aTarget.IsDead || mTargetBuffer.Contains(aTarget)) continue;
                mTargetBuffer.Add(aTarget);

                bool aWeak = IsWeak(aTarget, iWeapon);
                float aDamage = mChangeAttackDamage * SwapChain.DamageMultiplier * (aWeak ? mWeaknessDamageMultiplier : 1f);
                float aStagger = mChangeAttackStagger * (aWeak ? mWeaknessStaggerMultiplier : 1f);
                Vector3 aDirection = Flat(aTarget.transform.position - transform.position);
                var aOutcome = aTarget.ApplyHit(new HitInfo
                {
                    Damage = aDamage,
                    Stagger = aStagger,
                    ArmorBreak = mChangeAttackArmorBreak,
                    Knockback = mChangeAttackKnockback,
                    Direction = aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : transform.forward,
                    Source = gameObject,
                });
                if (!aOutcome.Landed()) continue;
                aAnyLanded = true;
                aAnyWeak |= aWeak;
                if (CombatFeedback.Instance != null)
                {
                    CombatFeedback.Instance.SpawnDamageNumber(aTarget.transform.position + Vector3.up * 2.2f, aTarget.LastDamage,
                        aWeak ? mWeakColor : aColor);
                }
            }

            if (!aAnyLanded) return;
            mStyle.RegisterHit($"change#{iWeapon}", iWeapon, mChangeAttackStylePoints, mSlots.Bonus.Kind == WeaponBonusKind.Arsenal);
            mStyle.Announce("CHANGE ATTACK", aColor);
            if (aAnyWeak) AnnounceWeak();
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.HitStop(0.05f);
                CombatFeedback.Instance.Shake(0.12f);
            }
        }

        static readonly Color mWeakColor = new Color(1f, 0.55f, 0.1f);

        /// <summary>コンボが時間切れで途切れたら、数に応じてスタイルのボーナス</summary>
        void TickCombo()
        {
            int aFinished = Combo.Tick(Time.time);
            if (aFinished < 10) return;
            mStyle.AddBonus(aFinished * mStyle.Config.ComboBonusPerHit, $"{aFinished} HITS  {ComboCounter.Rating(aFinished)}", new Color(1f, 0.85f, 0.4f));
        }

        /// <summary>
        /// チェンジドライブ: 各プリセットの攻撃A の武器に次々と持ち替え、周りの敵をまとめて攻撃する。
        /// 武器種が違う攻撃が続くので、どの敵にも弱点が入りやすい。出している間は無敵。
        /// </summary>
        System.Collections.IEnumerator ChangeDrive()
        {
            if (!Drive.TryConsume()) yield break;
            IsDriving = true;
            if (mCurrent != null) EndSkill();
            mStyle.AddBonus(100f, "CHANGE DRIVE", new Color(0.9f, 0.95f, 1f));
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SlowMotion(0.5f, 1.4f);
                CombatFeedback.Instance.Shake(0.3f);
            }

            foreach (var aSkill in DriveSkills())
            {
                yield return new WaitForSeconds(mDriveInterval);
                if (mSelf.IsDead) break;
                mWeapons.SetMainWeapon(aSkill.Weapon);
                var aData = mSlots.GetWeaponData(aSkill.Weapon);
                var aColor = aData != null ? aData.Color : Color.white;
                if (CombatFeedback.Instance != null)
                {
                    CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, mDriveRadius, aColor);
                    CombatFeedback.Instance.SpawnSparks(transform.position + Vector3.up, aColor, 14, 11f);
                }

                int aCount = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, mDriveRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
                mTargetBuffer.Clear();
                for (int aI = 0; aI < aCount; aI++)
                {
                    var aTarget = mOverlapBuffer[aI].GetComponentInParent<Damageable>();
                    if (aTarget == null || aTarget.Team == mSelf.Team || aTarget.IsDead || mTargetBuffer.Contains(aTarget)) continue;
                    mTargetBuffer.Add(aTarget);
                }
                // 持ち替えるたびにチェインがつながる
                SwapChain.OnSwap(Time.time);
                if (mTargetBuffer.Count > 0)
                {
                    ResolveHits(new SkillHitContext { Skill = aSkill, StepIndex = 0, Multiplier = mDriveMultiplier, SwapStrikeId = -1 },
                        new List<Damageable>(mTargetBuffer), transform.position);
                }
            }

            yield return new WaitForSeconds(mDriveInterval);
            var aMain = mSlots.GetCurrent(SlotType.AttackA);
            if (aMain != null) mWeapons.SetMainWeapon(aMain.Weapon);
            mExtraInvulnerableUntil = Time.time + 0.4f;
            IsDriving = false;
        }

        /// <summary>チェンジドライブで使う技: プリセット方式は各プリセットの攻撃A、ラック方式は攻撃A のラック</summary>
        List<SkillData> DriveSkills()
        {
            var aSkills = new List<SkillData>();
            if (mSlots.Mode == SwapMode.Preset)
            {
                foreach (var aPreset in mSlots.Presets)
                {
                    if (aPreset.AttackA != null) aSkills.Add(aPreset.AttackA);
                }
            }
            else
            {
                foreach (var aSkill in mSlots.GetRack(SlotType.AttackA).Skills)
                {
                    if (aSkill != null) aSkills.Add(aSkill);
                }
            }
            if (aSkills.Count == 0 && mSlots.GetCurrent(SlotType.AttackA) != null) aSkills.Add(mSlots.GetCurrent(SlotType.AttackA));
            return aSkills;
        }

        static bool IsWeak(Damageable iTarget, WeaponType iWeapon)
        {
            var aEnemy = iTarget.GetComponent<EnemyController>();
            // 型を切り替える敵は、今の型の弱点で判定する
            return aEnemy != null && aEnemy.Profile != null && aEnemy.IsWeakTo(iWeapon);
        }

        void AnnounceWeak()
        {
            if (Time.unscaledTime - mLastWeakAnnounce < 0.6f) return;
            mLastWeakAnnounce = Time.unscaledTime;
            mStyle.AddBonus(mWeaknessStyleBonus, "WEAK!", mWeakColor);
        }

        void SwapCancel(SlotType iSlot)
        {
            bool aContinuing = mLastSwapCancelSlot == iSlot && Time.time - mLastSwapCancelTime < mSwapCancelStreakReset;
            mSwapCancelStreak = aContinuing ? mSwapCancelStreak + 1 : 1;
            mLastSwapCancelSlot = iSlot;
            mLastSwapCancelTime = Time.time;
            EndSkill();
            mStyle.AddBonus(mStyle.Config.SwapCancelBonus, "SWAP CANCEL", new Color(0.4f, 0.9f, 1f));
        }

        /// <summary>このプリセットに切り替えると、実行中の技のスロットの中身が変わってスワップキャンセルになるか</summary>
        public bool CanPresetCancel(int iIndex)
        {
            if (mCurrent == null || mSlots.Mode != SwapMode.Preset || iIndex == mSlots.PresetIndex) return false;
            if (iIndex < 0 || iIndex >= mSlots.Presets.Count) return false;
            if (mSlots.Presets[iIndex].Get(mCurrent.Slot) == mCurrent) return false;
            return CanSwapCancel(mCurrent.Slot);
        }

        bool CanSwapCancel(SlotType iSlot)
        {
            if (mCurrent == null || mIsFinisher || mCurrent.Slot != iSlot || mPhase != Phase.Recovery) return false;

            bool aContinuing = mLastSwapCancelSlot == iSlot && Time.time - mLastSwapCancelTime < mSwapCancelStreakReset;
            int aStreak = aContinuing ? mSwapCancelStreak : 0;
            float aRecovery = FrameTime.ToSeconds(mStep.RecoveryFrames);
            float aRequired = Mathf.Max(
                FrameTime.ToSeconds(mCurrent.SwapCancelFrame),
                aRecovery * Mathf.Min(0.9f, mSwapCancelStreakPenalty * aStreak));
            return mPhaseTime >= aRequired;
        }

        // ---------- 発動 ----------

        bool TryStartFromInput()
        {
            var aFinisher = mSlots.MasteryFinisher;
            if (aFinisher != null && mInput.Consume(PlayerAction.Finisher))
            {
                StartSkill(aFinisher, true);
                return true;
            }

            if (TryStartSlot(SlotType.Movement)) return true;
            if (TryStartSlot(SlotType.AttackA)) return true;
            if (TryStartSlot(SlotType.AttackB)) return true;

            if (mInput.Peek(PlayerAction.Jump) && mMotor.IsGrounded)
            {
                mInput.Consume(PlayerAction.Jump);
                if (mCurrent != null) EndSkill();
                return mMotor.TryJump();
            }
            return false;
        }

        bool TryStartSlot(SlotType iSlot)
        {
            var aAction = PlayerInputReader.ActionFor(iSlot);
            if (!mInput.Peek(aAction)) return false;

            var aSkill = mSlots.GetCurrent(iSlot);
            if (aSkill == null) return false;
            // 空中で使えない技は、着地まで先行入力として残す
            if (!aSkill.UsableInAir && !mMotor.IsGrounded) return false;

            mInput.Consume(aAction);
            if (mCurrent != null) EndSkill();
            StartSkill(aSkill, false);
            return true;
        }

        void StartSkill(SkillData iSkill, bool iFinisher)
        {
            mCurrent = iSkill;
            mIsFinisher = iFinisher;
            mSkillTime = 0f;
            mDamageMultiplier = 1f;
            mHitsEnabled = true;
            mJustDodgeAwarded = false;
            mTravelEnemy = null;
            mTravelToPoint = false;

            mWeapons.ShowTemporary(iSkill.Weapon);

            var aTarget = mLockOn.Target;
            Vector3 aDesired = Flat(mMotor.DesiredDirection);
            if (iSkill.Behavior == SkillBehavior.DashStrike || iSkill.Behavior == SkillBehavior.Blink)
                mMoveDirection = aDesired.sqrMagnitude > 0.01f ? aDesired.normalized : transform.forward;
            else if (aTarget != null)
                mMoveDirection = DirectionTo(aTarget.transform.position);
            else
                mMoveDirection = aDesired.sqrMagnitude > 0.01f ? aDesired.normalized : transform.forward;
            mMotor.FaceDirection(mMoveDirection);

            if (mMotor.IsGrounded)
            {
                mMotor.GravityScale = 1f;
            }
            else
            {
                mMotor.GravityScale = iSkill.AirGravityScale;
                if (mMotor.VerticalVelocity < 0f) mMotor.SetVerticalVelocity(0f);
            }

            switch (iSkill.Behavior)
            {
                case SkillBehavior.ChargeCounter:
                    mPhase = Phase.Charging;
                    mPhaseTime = 0f;
                    mSelf.CounterHandler = OnCounter;
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
                    mMotor.GravityScale = iSkill.AirGravityScale;
                    mMotor.SetVerticalVelocity(iSkill.JumpVelocity);
                    BeginStep(0);
                    break;
                default:
                    BeginStep(0);
                    break;
            }
        }

        void BeginStep(int iIndex)
        {
            mStepIndex = iIndex;
            mPhase = Phase.Startup;
            mPhaseTime = 0f;
            mChainQueued = false;
            mStyleRegisteredThisStep = false;
            mHitThisStep.Clear();

            if (!mMotor.IsGrounded && mCurrent.Behavior != SkillBehavior.HammerJump && mMotor.VerticalVelocity < 0f)
            {
                mMotor.SetVerticalVelocity(0f);
            }
        }

        // ---------- 実行中 ----------

        void TickSkill(float iDt)
        {
            mPhaseTime += iDt;
            mSkillTime += iDt;

            mSelf.Invulnerable = mSkillTime < FrameTime.ToSeconds(mCurrent.InvulnerableFrames)
                                || Time.time < mExtraInvulnerableUntil
                                || (mPhase == Phase.Travel && mCurrent.Behavior == SkillBehavior.Grapple);

            // 同じボタンで次の段を予約
            bool aInStep = mPhase == Phase.Startup || mPhase == Phase.Active || mPhase == Phase.Recovery;
            if (aInStep && !mChainQueued && !mCurrent.AutoChain && mStepIndex + 1 < mCurrent.StepCount
                && mInput.Consume(PlayerInputReader.ActionFor(mCurrent.Slot)))
            {
                mChainQueued = true;
            }

            switch (mPhase)
            {
                case Phase.Startup:
                {
                    ApplyForward(iDt);
                    float aDuration = FrameTime.ToSeconds(mStep.StartupFrames);
                    if (mPhaseTime >= aDuration)
                    {
                        mPhaseTime -= aDuration;
                        mPhase = Phase.Active;
                        OnActiveStart();
                    }
                    break;
                }
                case Phase.Active:
                {
                    ApplyForward(iDt);
                    DoHitbox();
                    float aDuration = FrameTime.ToSeconds(mStep.ActiveFrames);
                    if (mPhaseTime >= aDuration)
                    {
                        mPhaseTime -= aDuration;
                        if (mCurrent.Behavior == SkillBehavior.HammerJump && mStepIndex == 0)
                        {
                            mMotor.GravityScale = 1f;
                            mMotor.SetVerticalVelocity(mCurrent.JumpVelocity);
                            mPhase = Phase.Airborne;
                            mPhaseTime = 0f;
                        }
                        else
                        {
                            mPhase = Phase.Recovery;
                        }
                    }
                    break;
                }
                case Phase.Recovery:
                    TickRecovery();
                    break;
                case Phase.Charging:
                {
                    float aMax = FrameTime.ToSeconds(mCurrent.ChargeFrames);
                    if (!mInput.IsHeld(PlayerInputReader.ActionFor(mCurrent.Slot)) || mPhaseTime >= aMax)
                    {
                        ReleaseCharge(false);
                    }
                    break;
                }
                case Phase.Airborne:
                    mMotor.Displace(mMoveDirection * (mCurrent.DashDistance / 0.8f) * iDt);
                    if (mPhaseTime > 0.1f && mMotor.IsGrounded)
                    {
                        if (mCurrent.StepCount > 1) BeginStep(1);
                        else EndSkill();
                    }
                    break;
                case Phase.Travel:
                    TickTravel(iDt);
                    break;
                case Phase.PullWait:
                    if (mTravelEnemy == null || !mTravelEnemy.IsBeingPulled || mPhaseTime > 0.5f) BeginStep(0);
                    break;
                case Phase.Hang:
                    TickHang();
                    break;
            }
        }

        /// <summary>ポイントにぶら下がっている間。次のワイヤー・ジャンプ・空中攻撃につなげられる。</summary>
        void TickHang()
        {
            mMotor.GravityScale = 0f;
            mMotor.SetVerticalVelocity(0f);

            if (mInput.Consume(PlayerAction.Jump))
            {
                Vector3 aForward = Flat(mMotor.DesiredDirection);
                if (aForward.sqrMagnitude < 0.01f) aForward = transform.forward;
                EndSkill();
                mMotor.SetVerticalVelocity(mGrappleJumpVelocity);
                mMotor.AddImpulse(aForward.normalized * mGrappleJumpForward);
                return;
            }

            if (TryStartFromInput()) return;
            if (mPhaseTime >= mGrappleHangTime) EndSkill();
        }

        void TickRecovery()
        {
            var aStep = mStep;
            float aDuration = FrameTime.ToSeconds(aStep.RecoveryFrames);
            bool aHasNext = mStepIndex + 1 < mCurrent.StepCount;

            if (aHasNext && (mChainQueued || mCurrent.AutoChain))
            {
                float aChainAt = mCurrent.AutoChain ? aDuration : FrameTime.ToSeconds(aStep.ChainFrame);
                if (mPhaseTime >= aChainAt)
                {
                    BeginStep(mStepIndex + 1);
                    return;
                }
            }

            if (!mIsFinisher && mPhaseTime >= FrameTime.ToSeconds(aStep.CancelFrame) && TryStartFromInput()) return;

            if (mPhaseTime >= aDuration && !(aHasNext && mCurrent.AutoChain)) EndSkill();
        }

        void ApplyForward(float iDt)
        {
            var aStep = mStep;
            float aTotal = FrameTime.ToSeconds(aStep.StartupFrames + aStep.ActiveFrames);
            if (aTotal <= 0f || Mathf.Approximately(aStep.ForwardMove, 0f)) return;

            var aTarget = mLockOn.Target;
            if (aTarget != null && mCurrent.Behavior != SkillBehavior.DashStrike)
            {
                float aDistance = Flat(aTarget.transform.position - transform.position).magnitude;
                if (aDistance <= mStopDistance + aTarget.Radius) return;
            }
            mMotor.Displace(mMoveDirection * (aStep.ForwardMove * iDt / aTotal));
        }

        void OnActiveStart()
        {
            var aStep = mStep;
            var aData = mSlots.GetWeaponData(mCurrent.Weapon);
            var aColor = aData != null ? aData.Color : Color.white;

            switch (mCurrent.Behavior)
            {
                case SkillBehavior.Projectile:
                    FireProjectiles(aColor);
                    return;
                case SkillBehavior.TargetedStrike:
                    FireStrike(aColor);
                    return;
                case SkillBehavior.Beam:
                    if (CombatFeedback.Instance != null)
                    {
                        GetBeam(out var aFrom, out var aTo);
                        CombatFeedback.Instance.SpawnBeam(aFrom, aTo, mCurrent.BeamRadius, aColor,
                            Mathf.Max(0.05f, FrameTime.ToSeconds(aStep.ActiveFrames)));
                    }
                    break;
            }

            if (aStep.HitboxRadius >= 2.5f && mCurrent.Behavior != SkillBehavior.Beam && CombatFeedback.Instance != null)
            {
                var aCenter = transform.TransformPoint(aStep.HitboxOffset);
                CombatFeedback.Instance.SpawnShockwave(new Vector3(aCenter.x, transform.position.y + 0.05f, aCenter.z),
                    aStep.HitboxRadius, aColor);
            }
            DoHitbox();
        }

        SkillHitContext CurrentContext() => new SkillHitContext
        {
            Skill = mCurrent,
            StepIndex = mStepIndex,
            Multiplier = mDamageMultiplier,
            SwapStrikeId = SwapStrikeReady ? mSwapStrikeId : -1,
        };

        void DoHitbox()
        {
            var aStep = mStep;
            if (!mHitsEnabled) return;
            if (mCurrent.Behavior == SkillBehavior.Projectile || mCurrent.Behavior == SkillBehavior.TargetedStrike) return;

            int aCount;
            if (mCurrent.Behavior == SkillBehavior.Beam)
            {
                GetBeam(out var aFrom, out var aTo);
                aCount = Physics.OverlapCapsuleNonAlloc(aFrom, aTo, mCurrent.BeamRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            }
            else
            {
                if (aStep.HitboxRadius <= 0f) return;
                Vector3 aCenter = transform.TransformPoint(aStep.HitboxOffset);
                aCount = Physics.OverlapSphereNonAlloc(aCenter, aStep.HitboxRadius, mOverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            }

            mTargetBuffer.Clear();
            for (int aI = 0; aI < aCount; aI++)
            {
                // 探索: ひび割れた壁(決まった武器種の技で崩れる)
                var aWall = mOverlapBuffer[aI].GetComponentInParent<BreakableWall>();
                if (aWall != null)
                {
                    aWall.TryBreak(mCurrent);
                    continue;
                }

                var aTarget = mOverlapBuffer[aI].GetComponentInParent<Damageable>();
                if (aTarget == null || aTarget.Team == mSelf.Team || aTarget.IsDead || !mHitThisStep.Add(aTarget)) continue;
                mTargetBuffer.Add(aTarget);
            }
            if (mTargetBuffer.Count > 0) ResolveHits(CurrentContext(), mTargetBuffer, transform.position);
        }

        /// <summary>
        /// 技のヒットをまとめて処理する(近接の判定・弾・爆発のすべてがここを通る)。
        /// ダメージ、武器種ボーナス、スワップストライク、スタイル、演出を反映する。
        /// </summary>
        public void ResolveHits(SkillHitContext iContext, List<Damageable> iTargets, Vector3 iSourcePosition)
        {
            if (iContext.Skill == null || iTargets.Count == 0) return;

            var aSkill = iContext.Skill;
            var aStep = iContext.Step;
            bool aSwapStrike = iContext.SwapStrikeId >= 0 && iContext.SwapStrikeId == mSwapStrikeId && SwapStrikeReady;
            bool aAnyLanded = false;
            bool aStyleRegistered = false;
            float aHitstop = 0f;
            var aBonus = mSlots.Bonus;
            var aWeaponData = mSlots.GetWeaponData(aSkill.Weapon);

            bool aAnyWeak = false;
            int aLanded = 0;
            int aWeakLanded = 0;
            // スタイルランクが高いほど攻撃力が上がる(格好よく戦うほど強い)
            float aRankMultiplier = mStyle.Config.DamageMultiplier(mStyle.Meter.RankIndex);
            foreach (var aTarget in iTargets)
            {
                if (aTarget == null || aTarget.IsDead) continue;

                bool aWeak = IsWeak(aTarget, aSkill.Weapon);
                float aDamage = aStep.Damage * iContext.Multiplier * SwapChain.DamageMultiplier * aRankMultiplier;
                float aStagger = aStep.Stagger * iContext.Multiplier;
                if (aWeak)
                {
                    aDamage *= mWeaknessDamageMultiplier;
                    aStagger *= mWeaknessStaggerMultiplier;
                }
                if (aBonus.Boosts(aSkill.Weapon) && aWeaponData != null) aStagger *= 1f + aWeaponData.SynergyStaggerBonus;
                if (aSwapStrike)
                {
                    aDamage *= mSwapStrikeDamageMultiplier;
                    aStagger *= mSwapStrikeStaggerMultiplier;
                }

                Vector3 aDirection = Flat(aTarget.transform.position - iSourcePosition);
                var aOutcome = aTarget.ApplyHit(new HitInfo
                {
                    Damage = aDamage,
                    Stagger = aStagger,
                    ArmorBreak = aStep.ArmorBreak * iContext.Multiplier,
                    Knockback = aStep.Knockback,
                    Launch = aStep.Launch,
                    Direction = aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : transform.forward,
                    Source = gameObject,
                });
                if (!aOutcome.Landed()) continue;

                aAnyLanded = true;
                aAnyWeak |= aWeak;
                aLanded++;
                if (aWeak) aWeakLanded++;
                aHitstop = Mathf.Max(aHitstop, aStep.Hitstop * (aOutcome == HitOutcome.ArmorBroken ? 2f : 1f));

                // 近接の1段は1回だけ登録する。同じ段で複数の敵に当てた分は少しだけ加点
                bool aFirstForStep = iContext.Skill == mCurrent && iContext.StepIndex == mStepIndex ? !mStyleRegisteredThisStep : !aStyleRegistered;
                if (aFirstForStep)
                {
                    if (iContext.Skill == mCurrent && iContext.StepIndex == mStepIndex) mStyleRegisteredThisStep = true;
                    aStyleRegistered = true;
                    mStyle.RegisterHit($"{aSkill.name}#{iContext.StepIndex}", aSkill.Weapon, aStep.StylePoints * iContext.Multiplier,
                        aBonus.Kind == WeaponBonusKind.Arsenal);
                }
                else
                {
                    mStyle.AddBonus(aStep.StylePoints * 0.3f);
                }

                if (aOutcome == HitOutcome.ArmorBroken) mStyle.Announce("ARMOR BREAK", new Color(1f, 0.6f, 0.2f));

                if (CombatFeedback.Instance != null)
                {
                    var aColor = aSwapStrike ? new Color(1f, 0.9f, 0.2f)
                        : aWeak ? mWeakColor
                        : aOutcome == HitOutcome.Armored ? new Color(0.7f, 0.7f, 0.7f)
                        : Color.white;
                    CombatFeedback.Instance.SpawnDamageNumber(aTarget.transform.position + Vector3.up * 2.2f, aTarget.LastDamage, aColor);

                    // 火花(弱点・アーマー破壊・重い一撃は多く)。重い一撃は画面も大きく揺らす
                    bool aHeavy = aTarget.LastDamage >= 25f || aOutcome == HitOutcome.ArmorBroken;
                    var aSparkColor = aWeak || aSwapStrike ? aColor : aWeaponData != null ? Color.Lerp(aWeaponData.Color, Color.white, 0.4f) : Color.white;
                    var aHitPoint = Vector3.Lerp(aTarget.transform.position + Vector3.up * 1.1f, iSourcePosition + Vector3.up * 1.1f, 0.25f);
                    CombatFeedback.Instance.SpawnSparks(aHitPoint, aSparkColor, aHeavy || aWeak ? 9 : 5, aHeavy ? 9f : 7f);
                    if (aHeavy) CombatFeedback.Instance.Shake(0.22f);
                }
            }

            if (!aAnyLanded) return;
            Combo.AddHit(Time.time, aLanded);
            Drive.AddFromAttack(aLanded, aWeakLanded, aSwapStrike);
            if (aSwapStrike)
            {
                mSwapStrikeUntil = -1f;
                mStyle.AddBonus(mStyle.Config.SwapStrikeBonus, "SWAP STRIKE!", new Color(1f, 0.9f, 0.2f));
            }
            if (aAnyWeak) AnnounceWeak();
            if (SwapChain.OnHit(Time.time))
            {
                mStyle.AddBonus(mChainStyleBonusPerStep * SwapChain.Chain, $"CHAIN ×{SwapChain.Chain}", new Color(0.55f, 1f, 0.9f));
            }
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.HitStop(aHitstop);
                CombatFeedback.Instance.Shake(0.05f + aHitstop);
            }
        }

        // ---------- 飛び道具・ビーム ----------

        /// <summary>弾を撃つ方向。ロックオン中は対象の胸へ、それ以外は向いている方向へ水平に。</summary>
        Vector3 AimDirection(Vector3 iOrigin)
        {
            var aTarget = mLockOn.Target;
            if (aTarget != null && !aTarget.IsDead)
            {
                Vector3 aTo = aTarget.CenterPoint - iOrigin;
                if (aTo.sqrMagnitude > 0.01f) return aTo.normalized;
            }
            return transform.forward;
        }

        Vector3 mMuzzlePosition => transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;

        void FireProjectiles(Color iColor)
        {
            var aContext = CurrentContext();
            Vector3 aOrigin = mMuzzlePosition;
            Vector3 aAim = AimDirection(aOrigin);
            int aCount = Mathf.Max(1, mCurrent.ProjectileCount);
            bool aFullCircle = mCurrent.ProjectileSpread >= 359f;

            for (int aI = 0; aI < aCount; aI++)
            {
                float aAngle;
                if (aCount == 1) aAngle = 0f;
                else if (aFullCircle) aAngle = 360f * aI / aCount;
                else aAngle = Mathf.Lerp(-mCurrent.ProjectileSpread * 0.5f, mCurrent.ProjectileSpread * 0.5f, aI / (aCount - 1f));

                Vector3 aDirection = Quaternion.AngleAxis(aAngle, Vector3.up) * aAim;
                Vector3 aSpawn = aFullCircle ? transform.position + Vector3.up * 1.2f + aDirection * 0.6f : aOrigin;
                SkillProjectile.Spawn(this, aContext, aSpawn, aDirection * mCurrent.ProjectileSpeed, mLockOn.Target, iColor);
            }
        }

        void FireStrike(Color iColor)
        {
            var aTarget = mLockOn.Target;
            Vector3 aPosition;
            if (aTarget != null && !aTarget.IsDead && Vector3.Distance(aTarget.transform.position, transform.position) <= mCurrent.StrikeDistance * 2f)
            {
                aPosition = aTarget.transform.position;
            }
            else
            {
                aPosition = transform.position + transform.forward * mCurrent.StrikeDistance;
                // 地面の高さに合わせる
                if (Physics.Raycast(aPosition + Vector3.up * 5f, Vector3.down, out var aGround, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    aPosition = aGround.point;
                }
            }
            SkillProjectile.Spawn(this, CurrentContext(), aPosition + Vector3.up * 0.1f, Vector3.zero, null, iColor, mCurrent.StrikeDelay);
        }

        void GetBeam(out Vector3 oFrom, out Vector3 oTo)
        {
            oFrom = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
            Vector3 aDirection = AimDirection(oFrom);
            float aLength = mCurrent.BeamLength;
            if (Physics.Raycast(oFrom, aDirection, out var aWall, aLength, ~0, QueryTriggerInteraction.Ignore)
                && aWall.collider.GetComponentInParent<Damageable>() == null)
            {
                aLength = aWall.distance;
            }
            oTo = oFrom + aDirection * aLength;
        }

        // ---------- ブリンク ----------

        void Blink()
        {
            Vector3 aDirection = mMoveDirection;
            float aDistance = mCurrent.DashDistance;
            Vector3 aBottom = transform.position + Vector3.up * 0.5f;
            Vector3 aTop = transform.position + Vector3.up * 1.5f;
            if (Physics.CapsuleCast(aBottom, aTop, 0.35f, aDirection, out var aHit, aDistance, ~0, QueryTriggerInteraction.Ignore)
                && !aHit.collider.transform.IsChildOf(transform))
            {
                aDistance = Mathf.Max(0f, aHit.distance - 0.2f);
            }

            var aData = mSlots.GetWeaponData(mCurrent.Weapon);
            var aColor = aData != null ? aData.Color : Color.white;
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, 1.2f, aColor);
            mMotor.Teleport(aDirection * aDistance);
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.SpawnShockwave(transform.position + aDirection * aDistance, 1.6f, aColor);
            }
        }

        // ---------- 溜め・カウンター(居合) ----------

        bool OnCounter(HitInfo iHit)
        {
            if (mCurrent == null || mPhase != Phase.Charging) return false;
            ReleaseCharge(true);
            return true;
        }

        void ReleaseCharge(bool iCounter)
        {
            mSelf.CounterHandler = null;
            float aMax = FrameTime.ToSeconds(mCurrent.ChargeFrames);
            float aRatio = iCounter || aMax <= 0f ? 1f : Mathf.Clamp01(mPhaseTime / aMax);
            mDamageMultiplier = Mathf.Lerp(mMinChargeMultiplier, mMaxChargeMultiplier, aRatio) * (iCounter ? mCounterMultiplier : 1f);

            if (iCounter)
            {
                mExtraInvulnerableUntil = Time.time + mCounterInvulnerableTime;
                mStyle.AddBonus(mStyle.Config.CounterBonus, "COUNTER!", new Color(1f, 0.4f, 0.4f));
            }

            var aTarget = mLockOn.Target;
            if (aTarget != null)
            {
                mMoveDirection = DirectionTo(aTarget.transform.position);
                mMotor.FaceDirection(mMoveDirection);
            }
            BeginStep(0);
        }

        // ---------- ワイヤー・引き寄せ ----------

        void BeginGrapple()
        {
            mMotor.GravityScale = 0f;
            mMotor.SetVerticalVelocity(0f);

            // 優先順: 視界内で一番近いポイント → ロックオン中の敵 → 空中ダッシュ
            var aPoint = FindGrapplePoint(mCurrent.Range);
            var aTarget = mLockOn.Target;
            if (aPoint != null)
            {
                mTravelToPoint = true;
                mTravelTarget = aPoint.transform.position - Vector3.up * 1.8f;
            }
            else if (aTarget != null && Vector3.Distance(aTarget.transform.position, transform.position) <= mCurrent.Range)
            {
                mTravelEnemy = aTarget;
            }
            else
            {
                mTravelTarget = transform.position + mMoveDirection * mCurrent.DashDistance + Vector3.up * 0.8f;
            }

            Vector3 aDestination = mTravelEnemy != null ? mTravelEnemy.transform.position : mTravelTarget;
            BeginTravel(aDestination);
            mHitsEnabled = mTravelEnemy != null;
        }

        /// <summary>
        /// 視界内(カメラに映っていて、途中に遮るものがない)にあるグラップルポイントのうち、一番近いもの。
        /// 今ぶら下がっているポイントは除く。
        /// </summary>
        GrapplePoint FindGrapplePoint(float iRange)
        {
            Vector3 aChest = transform.position + Vector3.up;
            GrapplePoint aBest = null;
            float aBestDistance = float.MaxValue;
            foreach (var aPoint in GrapplePoint.All)
            {
                Vector3 aPosition = aPoint.transform.position;
                float aDistance = Vector3.Distance(aChest, aPosition);
                if (aDistance > iRange || aDistance < mGrappleMinDistance || aDistance >= aBestDistance) continue;
                if (!IsInView(aPosition)) continue;
                if (Physics.Linecast(aChest, aPosition, out var aHit, ~0, QueryTriggerInteraction.Ignore)
                    && !aHit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }
                aBest = aPoint;
                aBestDistance = aDistance;
            }
            return aBest;
        }

        bool IsInView(Vector3 iPosition)
        {
            if (mView == null) return Vector3.Dot(Flat(iPosition - transform.position).normalized, transform.forward) > 0.3f;
            Vector3 aViewport = mView.WorldToViewportPoint(iPosition);
            return aViewport.z > 0f
                   && aViewport.x >= mGrappleViewMargin && aViewport.x <= 1f - mGrappleViewMargin
                   && aViewport.y >= mGrappleViewMargin && aViewport.y <= 1f - mGrappleViewMargin;
        }

        void BeginTravel(Vector3 iDestination)
        {
            mPhase = Phase.Travel;
            mPhaseTime = 0f;
            float aSpeed = Mathf.Max(1f, mCurrent.MoveSpeed);
            mTravelTimeout = Vector3.Distance(transform.position, iDestination) / aSpeed + mTravelTimeMargin;
        }

        void BeginPull()
        {
            var aTarget = mLockOn.Target;
            if (aTarget == null || Vector3.Distance(aTarget.transform.position, transform.position) > mCurrent.Range)
            {
                aTarget = FindEnemyInFront(mCurrent.Range);
            }

            if (aTarget == null)
            {
                BeginStep(0);
                return;
            }

            mTravelEnemy = aTarget;
            mMoveDirection = DirectionTo(aTarget.transform.position);
            mMotor.FaceDirection(mMoveDirection);

            if (aTarget.IsHeavy)
            {
                // 重い敵には自分が飛びつく
                mMotor.GravityScale = 0f;
                mMotor.SetVerticalVelocity(0f);
                BeginTravel(aTarget.transform.position);
                return;
            }

            aTarget.BeginPull(transform.position + mMoveDirection * 1.6f, mCurrent.MoveSpeed);
            mPhase = Phase.PullWait;
            mPhaseTime = 0f;
        }

        void TickTravel(float iDt)
        {
            mMotor.GravityScale = 0f;
            mMotor.SetVerticalVelocity(0f);

            if (mTravelEnemy != null && mTravelEnemy.IsDead)
            {
                mTravelEnemy = null;
                mHitsEnabled = false;
                ArriveTravel();
                return;
            }

            Vector3 aDestination = mTravelEnemy != null ? ApproachPoint(mTravelEnemy) : mTravelTarget;
            Vector3 aTo = aDestination - transform.position;
            float aStepLength = mCurrent.MoveSpeed * iDt;

            Vector3 aFlat = Flat(aTo);
            if (aFlat.sqrMagnitude > 0.01f)
            {
                mMoveDirection = aFlat.normalized;
                mMotor.FaceDirection(mMoveDirection);
            }

            if (aTo.magnitude <= aStepLength + 0.3f || mPhaseTime >= mTravelTimeout)
            {
                mMotor.Displace(Vector3.ClampMagnitude(aTo, aStepLength));
                ArriveTravel();
            }
            else
            {
                mMotor.Displace(aTo.normalized * aStepLength);
            }
        }

        void ArriveTravel()
        {
            if (mTravelToPoint)
            {
                mPhase = Phase.Hang;
                mPhaseTime = 0f;
                return;
            }

            mMotor.GravityScale = mCurrent.AirGravityScale;
            if (mTravelEnemy != null) mMotor.FaceDirection(DirectionTo(mTravelEnemy.transform.position));
            BeginStep(0);
        }

        Vector3 ApproachPoint(EnemyController iEnemy)
        {
            Vector3 aAway = Flat(transform.position - iEnemy.transform.position);
            aAway = aAway.sqrMagnitude > 0.01f ? aAway.normalized : -transform.forward;
            return iEnemy.transform.position + aAway * (iEnemy.Radius + 1.0f);
        }

        EnemyController FindEnemyInFront(float iRange)
        {
            EnemyController aBest = null;
            float aBestDistance = float.MaxValue;
            foreach (var aEnemy in EnemyController.Active)
            {
                if (aEnemy.IsDead) continue;
                Vector3 aTo = aEnemy.transform.position - transform.position;
                float aDistance = aTo.magnitude;
                if (aDistance > iRange || Vector3.Dot(Flat(aTo).normalized, mMoveDirection) < 0.5f) continue;
                if (aDistance < aBestDistance)
                {
                    aBestDistance = aDistance;
                    aBest = aEnemy;
                }
            }
            return aBest;
        }

        // ---------- 終了・割り込み ----------

        void EndSkill()
        {
            mSelf.CounterHandler = null;
            mCurrent = null;
            mPhase = Phase.None;
            mIsFinisher = false;
            mChainQueued = false;
            mTravelEnemy = null;
            mMotor.GravityScale = 1f;
            mSelf.Invulnerable = Time.time < mExtraInvulnerableUntil;
            mWeapons.ClearTemporary();
        }

        /// <summary>被弾などで技を中断する</summary>
        public void Interrupt(float iStunDuration, float iInvulnerableDuration)
        {
            if (mCurrent != null) EndSkill();
            // 被弾するとチェンジチェインとコンボが途切れる
            SwapChain.Reset();
            Combo.Break(Time.time);
            mStunnedUntil = Time.time + iStunDuration;
            mExtraInvulnerableUntil = Time.time + iInvulnerableDuration;
        }

        /// <summary>移動スキルの無敵で攻撃をかわしたとき、1回の発動につき1度だけ true を返す</summary>
        public bool TryConsumeJustDodge()
        {
            if (mCurrent == null || mCurrent.Slot != SlotType.Movement || mJustDodgeAwarded) return false;
            mJustDodgeAwarded = true;
            return true;
        }

        // ---------- 見た目 ----------

        void UpdateWeaponPose()
        {
            if (mCurrent == null)
            {
                mWeapons.SetIdle();
                return;
            }

            var aStep = mStep;
            float aNormalized = mPhase switch
            {
                Phase.Startup => -Progress(aStep.StartupFrames),
                Phase.Active => Mathf.Lerp(-1f, 1f, Progress(aStep.ActiveFrames)),
                Phase.Recovery => Mathf.Lerp(1f, 0f, Progress(aStep.RecoveryFrames)),
                Phase.Charging => -1f,
                _ => -0.6f,
            };
            mWeapons.SetSwing(aNormalized, mCurrent.Weapon == WeaponType.Hammer, mStepIndex % 2 == 0 ? 1f : -1f);
        }

        float Progress(int iFrames) => iFrames <= 0 ? 1f : Mathf.Clamp01(mPhaseTime / FrameTime.ToSeconds(iFrames));

        Vector3 DirectionTo(Vector3 iPosition)
        {
            Vector3 aDirection = Flat(iPosition - transform.position);
            return aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : transform.forward;
        }

        static Vector3 Flat(Vector3 iV) => new Vector3(iV.x, 0f, iV.z);
    }
}
