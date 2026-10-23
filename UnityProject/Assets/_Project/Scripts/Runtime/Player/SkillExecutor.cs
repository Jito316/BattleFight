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
    public partial class SkillExecutor : MonoBehaviour
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
        float mFrozenUntil = -1f;
        PlayerGuard mGuard;
        PlayerHitReaction mBodyPose;
        float mNextAfterimageTime;
        Transform mBody;
        /// <summary>回避の残像の色</summary>
        static readonly Color mAfterimageColor = new Color(0.55f, 0.75f, 1f);
        /// <summary>残像を出す間隔(秒)</summary>
        const float mAfterimageInterval = 0.035f;
        /// <summary>この段で、当てたときの踏み込みをもう入れたか(1段に1回だけ)</summary>
        bool mLungedThisStep;

        public SkillData Current => mCurrent;
        public bool IsBusy => mCurrent != null;
        public bool IsFinisherActive => mCurrent != null && mIsFinisher;
        public bool IsStunned => Time.time < mStunnedUntil;
        /// <summary>ヒットストップで止まっている(近接で当てたプレイヤーと、当てられた敵だけが止まる)</summary>
        public bool IsHitFrozen => Time.unscaledTime < mFrozenUntil;
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

            // ヒットストップ中は技を進めない(切り替えは受け付ける: 当てた瞬間のスワップキャンセル)
            bool aFrozen = IsHitFrozen;
            mMotor.Frozen = aFrozen;
            if (IsStunned)
            {
                mMotor.LocomotionEnabled = false;
            }
            else if (!aFrozen)
            {
                // ガード中は技を出さない(放したらすぐ出せる。パリィからの反撃)
                if (mGuard == null) mGuard = GetComponent<PlayerGuard>();
                if (mCurrent == null)
                {
                    if (mGuard == null || !mGuard.IsGuarding) TryStartFromInput();
                }
                else
                {
                    TickSkill(Time.deltaTime);
                }
                mMotor.LocomotionEnabled = mCurrent == null;
            }

            if (mCurrent == null) mSelf.Invulnerable = Time.time < mExtraInvulnerableUntil;
            UpdateWeaponPose();
            UpdateAfterimages();

            var aMovement = mSlots.GetCurrent(SlotType.Movement);
            GrapplePreview = aMovement != null && aMovement.Behavior == SkillBehavior.Grapple ? FindGrapplePoint(aMovement.Range) : null;
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

        /// <summary>ヒットストップ: しばらくプレイヤーを止める(攻撃を受けた瞬間など)</summary>
        public void Freeze(float iDuration)
        {
            if (iDuration > 0f) mFrozenUntil = Mathf.Max(mFrozenUntil, Time.unscaledTime + iDuration);
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

        float Progress(int iFrames) => iFrames <= 0 ? 1f : Mathf.Clamp01(mPhaseTime / FrameTime.ToSeconds(iFrames));

        Vector3 DirectionTo(Vector3 iPosition)
        {
            Vector3 aDirection = Flat(iPosition - transform.position);
            return aDirection.sqrMagnitude > 1e-4f ? aDirection.normalized : transform.forward;
        }

        static Vector3 Flat(Vector3 iVector) => new Vector3(iVector.x, 0f, iVector.z);
    }
}
