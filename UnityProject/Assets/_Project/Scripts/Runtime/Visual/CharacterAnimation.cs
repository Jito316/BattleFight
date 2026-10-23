using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// アニメーションの合図をまとめる(プレイヤー・敵の両方)。CharacterVisual に Animator があれば、
    /// 下の名前のパラメーターへ流す。Animator にないパラメーターは無視するので、使うものだけ作ればよい。
    /// 技・敵の攻撃に AnimationState(ステートの名前)があれば、合図の代わりにそのステートへ切り替える。
    /// </summary>
    public class CharacterAnimation : MonoBehaviour
    {
        // ---------- Animator のパラメーター名(アーティスト向けの約束) ----------
        /// <summary>水平の移動の速さ(m/秒)。float</summary>
        public const string SpeedParameter = "Speed";
        /// <summary>地面に立っているか。bool</summary>
        public const string GroundedParameter = "Grounded";
        /// <summary>ガードを構えているか。bool</summary>
        public const string GuardParameter = "Guard";
        /// <summary>技の武器種(WeaponType の番号)・段(0 から)・スロット(SlotType の番号)。int</summary>
        public const string WeaponParameter = "Weapon";
        public const string StepParameter = "Step";
        public const string SlotParameter = "Slot";
        /// <summary>trigger: 攻撃・回避(移動スキル)・ジャンプ・着地・被弾・パリィ・倒れる</summary>
        public const string AttackTrigger = "Attack";
        public const string DodgeTrigger = "Dodge";
        public const string JumpTrigger = "Jump";
        public const string LandTrigger = "Land";
        public const string HitTrigger = "Hit";
        public const string ParryTrigger = "Parry";
        public const string DieTrigger = "Die";

        /// <summary>AnimationState へ切り替えるときのつなぎの長さ(秒)</summary>
        const float mCrossFadeTime = 0.05f;

        CharacterVisual mVisual;
        Animator mAnimator;
        readonly HashSet<int> mParameters = new HashSet<int>();
        CharacterController mController;
        Damageable mDamageable;
        PlayerMotor mMotor;
        Vector3 mLastPosition;

        /// <summary>合図を出した(引数は合図の名前。テストや音・演出のつなぎ込み用)</summary>
        public event Action<string> ActionPlayed;
        /// <summary>最後に出した合図と、切り替えたステート(テスト用)</summary>
        public string LastAction { get; private set; }
        public string LastState { get; private set; }
        public bool IsGuarding { get; private set; }

        void Awake()
        {
            mController = GetComponent<CharacterController>();
            mDamageable = GetComponent<Damageable>();
            mMotor = GetComponent<PlayerMotor>();
            mLastPosition = transform.position;
        }

        void OnEnable()
        {
            if (mDamageable != null)
            {
                mDamageable.Damaged += OnDamaged;
                mDamageable.Died += OnDied;
            }
            if (mMotor != null)
            {
                mMotor.Jumped += OnJumped;
                mMotor.Landed += OnLanded;
            }
        }

        void OnDisable()
        {
            if (mDamageable != null)
            {
                mDamageable.Damaged -= OnDamaged;
                mDamageable.Died -= OnDied;
            }
            if (mMotor != null)
            {
                mMotor.Jumped -= OnJumped;
                mMotor.Landed -= OnLanded;
            }
        }

        void Start() => Bind();

        /// <summary>見た目(Animator)を探し直す。見た目を差し替えたあとに呼ぶ</summary>
        public void Bind()
        {
            mVisual = CharacterVisual.Of(this);
            mAnimator = mVisual != null ? mVisual.Animator : null;
            mParameters.Clear();
            if (mAnimator == null || mAnimator.runtimeAnimatorController == null) return;
            foreach (var aParameter in mAnimator.parameters) mParameters.Add(aParameter.nameHash);
        }

        void Update()
        {
            float aDeltaTime = Time.deltaTime;
            if (aDeltaTime <= 0f) return;
            Vector3 aMove = transform.position - mLastPosition;
            mLastPosition = transform.position;
            if (mAnimator == null) return;
            aMove.y = 0f;
            SetFloat(SpeedParameter, aMove.magnitude / aDeltaTime);
            SetBool(GroundedParameter, mMotor != null ? mMotor.IsGrounded : mController == null || mController.isGrounded);
        }

        /// <summary>プレイヤーの技の段が始まった(移動スキルは回避の合図)</summary>
        public void PlaySkill(SkillData iSkill, int iStep)
        {
            if (iSkill == null) return;
            SetInteger(WeaponParameter, (int)iSkill.Weapon);
            SetInteger(StepParameter, iStep);
            SetInteger(SlotParameter, (int)iSkill.Slot);
            Fire(iSkill.Slot == SlotType.Movement ? DodgeTrigger : AttackTrigger, iSkill.AnimationState);
        }

        /// <summary>敵の攻撃の予備動作が始まった</summary>
        public void PlayEnemyAttack(EnemyAttack iAttack)
        {
            if (iAttack == null) return;
            Fire(AttackTrigger, iAttack.AnimationState);
        }

        public void SetGuard(bool iOn)
        {
            IsGuarding = iOn;
            SetBool(GuardParameter, iOn);
        }

        /// <summary>合図を出す(トリガー名は上の定数)</summary>
        public void Play(string iTrigger) => Fire(iTrigger, null);

        void OnJumped() => Play(JumpTrigger);
        void OnLanded(float iFallSpeed) => Play(LandTrigger);
        void OnDied() => Play(DieTrigger);

        void OnDamaged(HitInfo iHit, HitOutcome iOutcome)
        {
            if (iOutcome == HitOutcome.Hit || iOutcome == HitOutcome.ArmorBroken) Play(HitTrigger);
        }

        void Fire(string iTrigger, string iState)
        {
            LastAction = iTrigger;
            LastState = string.IsNullOrEmpty(iState) ? null : iState;
            ActionPlayed?.Invoke(iTrigger);
            if (mAnimator == null || !mAnimator.isActiveAndEnabled) return;
            if (LastState != null)
            {
                int aState = Animator.StringToHash(LastState);
                if (mAnimator.HasState(0, aState))
                {
                    mAnimator.CrossFadeInFixedTime(aState, mCrossFadeTime, 0);
                    return;
                }
            }
            int aTrigger = Animator.StringToHash(iTrigger);
            if (mParameters.Contains(aTrigger)) mAnimator.SetTrigger(aTrigger);
        }

        void SetFloat(string iName, float iValue)
        {
            int aHash = Animator.StringToHash(iName);
            if (mAnimator != null && mParameters.Contains(aHash)) mAnimator.SetFloat(aHash, iValue);
        }

        void SetBool(string iName, bool iValue)
        {
            int aHash = Animator.StringToHash(iName);
            if (mAnimator != null && mParameters.Contains(aHash)) mAnimator.SetBool(aHash, iValue);
        }

        void SetInteger(string iName, int iValue)
        {
            int aHash = Animator.StringToHash(iName);
            if (mAnimator != null && mParameters.Contains(aHash)) mAnimator.SetInteger(aHash, iValue);
        }
    }
}
