using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 人型モデル(Quaternius)のアニメーション。移動・空中・技・被弾・倒れるを、プレイヤーの状態から選んで流す。
    /// 技は段が始まるたびに、スキルの種類と武器から決めたモーションに切り替える。
    /// 武器を持つ位置(HandAnchor)は右手の骨に合わせて動かす(向きは WeaponHolder の振りのまま)。
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public const string LocomotionState = "Locomotion";
        public const string AirState = "Air";
        public const string HitState = "Hit_Chest";
        public const string DeathState = "Death01";
        public const string SpeedParameter = "Speed";

        [SerializeField, FormerlySerializedAs("animator")] Animator mAnimator;
        [SerializeField, FormerlySerializedAs("executor")] SkillExecutor mExecutor;
        [SerializeField, FormerlySerializedAs("motor")] PlayerMotor mMotor;
        [SerializeField, FormerlySerializedAs("self")] Damageable mSelf;
        [Tooltip("武器を持たせる位置。右手の骨に合わせて動かす")]
        [SerializeField, FormerlySerializedAs("handAnchor")] Transform mHandAnchor;
        [SerializeField, FormerlySerializedAs("crossFade")] float mCrossFade = 0.08f;

        Transform mHandBone;
        Vector3 mLastPosition;
        float mSpeed;
        int mLastStepSerial = -1;
        bool mWasBusy;
        bool mDead;

        /// <summary>今流しているモーションの名前(テスト・デバッグ用)</summary>
        public string CurrentState { get; private set; } = LocomotionState;
        public Animator Animator => mAnimator;

        void Awake()
        {
            if (mAnimator != null)
            {
                mAnimator.applyRootMotion = false;
                if (mAnimator.isHuman) mHandBone = mAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            }
            mLastPosition = transform.position;
        }

        void OnEnable()
        {
            if (mSelf != null)
            {
                mSelf.Damaged += OnDamaged;
                mSelf.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (mSelf != null)
            {
                mSelf.Damaged -= OnDamaged;
                mSelf.Died -= OnDied;
            }
        }

        void Update()
        {
            if (mAnimator == null || mDead) return;

            // 実際に動いた量から速さを出す(押し出しや技の移動もそのまま足の動きになる)
            float aDt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector3 aDelta = transform.position - mLastPosition;
            mLastPosition = transform.position;
            float aTarget = new Vector3(aDelta.x, 0f, aDelta.z).magnitude / aDt;
            mSpeed = Mathf.Lerp(mSpeed, aTarget, 1f - Mathf.Exp(-12f * aDt));
            mAnimator.SetFloat(SpeedParameter, mSpeed);

            bool aBusy = mExecutor != null && mExecutor.IsBusy;
            if (aBusy && mExecutor.StepSerial != mLastStepSerial)
            {
                mLastStepSerial = mExecutor.StepSerial;
                Play(ActionFor(mExecutor.Current, mExecutor.StepIndex), mCrossFade);
            }
            else if (!aBusy)
            {
                bool aGrounded = mMotor == null || mMotor.IsGrounded;
                string aRest = aGrounded ? LocomotionState : AirState;
                // 技が終わったら、移動か空中のモーションに戻す。被弾のモーションは最後まで流す
                bool aHitPlaying = CurrentState == HitState && !IsFinished();
                if (CurrentState != aRest && !aHitPlaying) Play(aRest, mWasBusy ? 0.15f : 0.2f);
            }
            mWasBusy = aBusy;
        }

        void LateUpdate()
        {
            // アニメーションを流したあとの手の位置に、武器を持たせる
            if (mHandAnchor != null && mHandBone != null) mHandAnchor.position = mHandBone.position;
        }

        void Play(string iState, float iFade)
        {
            CurrentState = iState;
            mAnimator.CrossFadeInFixedTime(iState, iFade, 0, 0f);
        }

        bool IsFinished()
        {
            var aInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
            return !mAnimator.IsInTransition(0) && aInfo.normalizedTime >= 0.95f;
        }

        void OnDamaged(HitInfo iHit, HitOutcome iOutcome)
        {
            if (mDead || iOutcome != HitOutcome.Hit) return;
            if (mExecutor != null && mExecutor.IsBusy) return;
            Play(HitState, 0.05f);
        }

        void OnDied()
        {
            mDead = true;
            Play(DeathState, 0.1f);
        }

        /// <summary>技と段から、流すモーションを選ぶ</summary>
        public static string ActionFor(SkillData iSkill, int iStep)
        {
            if (iSkill == null) return LocomotionState;
            switch (iSkill.Behavior)
            {
                case SkillBehavior.ChargeCounter: return "Sword_Block";
                case SkillBehavior.HammerJump: return "NinjaJump_Start";
                case SkillBehavior.Boost: return "Jump_Start";
                case SkillBehavior.Grapple:
                case SkillBehavior.Pull: return "OverhandThrow";
                case SkillBehavior.Projectile:
                case SkillBehavior.Beam:
                case SkillBehavior.TargetedStrike: return iSkill.Weapon == WeaponType.Gun ? "Pistol_Shoot" : "Spell_Simple_Shoot";
                case SkillBehavior.DashStrike: return "Sword_Dash";
                case SkillBehavior.Blink: return "Roll";
            }
            if (iSkill.Slot == SlotType.Movement) return iSkill.Weapon == WeaponType.Sword ? "Sword_Dash" : "Roll";
            switch (iSkill.Weapon)
            {
                case WeaponType.Sword:
                    return (iStep % 3) switch { 0 => "Sword_Regular_A", 1 => "Sword_Regular_B", _ => "Sword_Regular_C" };
                case WeaponType.Hammer:
                    return iStep % 2 == 0 ? "Sword_Attack" : "Sword_Regular_C";
                case WeaponType.Chain:
                    return iStep % 2 == 0 ? "Melee_Hook" : "Sword_Regular_B";
                case WeaponType.Staff:
                    return "Spell_Simple_Shoot";
                case WeaponType.Gun:
                    return "Pistol_Shoot";
                case WeaponType.Gauntlet:
                    return iStep % 2 == 0 ? "Punch_Jab" : "Punch_Cross";
            }
            return "Sword_Attack";
        }

        /// <summary>アニメーターに用意するモーション(エディタでコントローラーを作るときに使う)</summary>
        public static readonly string[] ActionStates =
        {
            "Sword_Block", "NinjaJump_Start", "Jump_Start", "OverhandThrow", "Spell_Simple_Shoot", "Pistol_Shoot",
            "Sword_Dash", "Roll", "Sword_Regular_A", "Sword_Regular_B", "Sword_Regular_C", "Sword_Attack",
            "Melee_Hook", "Punch_Jab", "Punch_Cross", HitState, DeathState,
        };
    }
}
