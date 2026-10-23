using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// ガードとパリィ(GuardRules)。技を出していないときにガードを押している間、前から来た攻撃を受け止める。
    /// 押した直後に受けるとパリィ: ダメージなしで弾き返し、殴った敵がよろける。
    /// 見た目は、体の前に出す光る盾。受け止めた瞬間に大きく弾む。
    /// </summary>
    public class PlayerGuard : MonoBehaviour
    {
        const float mShieldPopDuration = 0.18f;
        /// <summary>盾の直径。体(直径 約1m)より大きくして、後ろのカメラからも体の周りに縁が見えるようにする</summary>
        public const float ShieldSize = 1.8f;
        static readonly Color mGuardColor = new Color(0.55f, 0.8f, 1f);
        static readonly Color mParryColor = new Color(1f, 0.85f, 0.3f);

        PlayerInputReader mInput;
        SkillExecutor mExecutor;
        PlayerMotor mMotor;
        Damageable mSelf;
        StyleRankSystem mStyle;
        CharacterAnimation mAnimation;
        Transform mShield;
        Renderer mShieldRenderer;
        float mGuardStartTime = -99f;
        float mShieldPopTime = -99f;
        bool mParryPop;

        public bool IsGuarding { get; private set; }
        /// <summary>パリィを決めた回数(テスト用)</summary>
        public int ParryCount { get; private set; }

        void Awake()
        {
            mInput = GetComponent<PlayerInputReader>();
            mExecutor = GetComponent<SkillExecutor>();
            mMotor = GetComponent<PlayerMotor>();
            mSelf = GetComponent<Damageable>();
            mStyle = GetComponent<StyleRankSystem>();
        }

        void OnEnable()
        {
            if (mSelf != null) mSelf.GuardHandler = OnGuard;
        }

        void OnDisable()
        {
            if (mSelf != null && mSelf.GuardHandler == OnGuard) mSelf.GuardHandler = null;
            SetGuarding(false);
        }

        void Update()
        {
            bool aCanGuard = mInput != null && mExecutor != null && !mExecutor.IsBusy && !mExecutor.IsStunned && !mExecutor.IsDriving
                             && !mSelf.IsDead && !GamePause.IsPaused
                             && (ArenaDirector.Instance == null || ArenaDirector.Instance.State != ArenaDirector.GameState.StageSelect);
            bool aWant = aCanGuard && mInput.IsHeld(PlayerAction.Guard);
            // 押した瞬間からパリィの受付が始まる(押しっぱなしで構え直しても、パリィにはならない)
            if (aWant && !IsGuarding) mGuardStartTime = Time.unscaledTime;
            if (mInput != null) mInput.Consume(PlayerAction.Guard);
            SetGuarding(aWant);
            UpdateShield();
        }

        void SetGuarding(bool iOn)
        {
            if (IsGuarding != iOn)
            {
                if (mAnimation == null) mAnimation = GetComponent<CharacterAnimation>();
                if (mAnimation != null) mAnimation.SetGuard(iOn);
            }
            IsGuarding = iOn;
            if (mMotor != null) mMotor.SpeedScale = iOn ? GuardRules.MoveSpeedScale : 1f;
        }

        HitOutcome OnGuard(HitInfo iHit)
        {
            if (!IsGuarding || !GuardRules.Covers(transform.forward, iHit.Direction)) return HitOutcome.Hit;
            bool aParry = GuardRules.IsParry(mGuardStartTime, Time.unscaledTime);
            var aEnemy = !iHit.Ranged && iHit.Source != null ? iHit.Source.GetComponent<EnemyController>() : null;
            var aContact = transform.position + Vector3.up * 1.1f - iHit.Direction * 0.7f;
            var aFeedback = CombatFeedback.Instance;
            mShieldPopTime = Time.unscaledTime;
            mParryPop = aParry;

            if (aParry)
            {
                ParryCount++;
                if (mAnimation != null) mAnimation.Play(CharacterAnimation.ParryTrigger);
                // パリィ: 長めに止まり、画面が光り、殴った敵が大きくよろける
                mExecutor.Freeze(0.16f);
                if (aEnemy != null) aEnemy.Rebound(iHit.Direction, true);
                if (mStyle != null) mStyle.AddBonus(mStyle.Config.CounterBonus, GameText.Get("announce.parry"), mParryColor);
                mExecutor.Drive.Add(0.1f);
                SoundPlayer.Play(Sfx.Parry, 1f);
                SoundPlayer.Play(Sfx.Weak, 0.5f, 1.2f);
                Rumble.Play(0.6f, 0.9f, 0.18f);
                if (aFeedback != null)
                {
                    aFeedback.Flash(0.5f);
                    aFeedback.Punch(5f);
                    aFeedback.Vignette(mParryColor, 0.7f);
                    aFeedback.Shake(0.18f);
                    aFeedback.Kick(-iHit.Direction, 0.12f);
                    if (!aFeedback.PlayCue(EffectCue.Parry, aContact, transform.rotation, 1f, mParryColor))
                    {
                        aFeedback.SpawnShockwave(transform.position + Vector3.up * 0.05f, 2.6f, mParryColor);
                        aFeedback.SpawnSparks(aContact, mParryColor, 16, 10f);
                    }
                }
                return HitOutcome.Parried;
            }

            // ガード: 少しだけ押し下げられ、相手も軽く弾かれる
            mExecutor.Freeze(0.05f);
            if (aEnemy != null) aEnemy.Rebound(iHit.Direction, false);
            mMotor.AddImpulse(iHit.Direction * (iHit.Knockback * 0.4f));
            SoundPlayer.Play(Sfx.Guard, 0.8f);
            Rumble.Play(0.25f, 0.4f, 0.08f);
            if (aFeedback != null)
            {
                aFeedback.Shake(0.08f);
                aFeedback.Kick(iHit.Direction, 0.06f);
                if (!aFeedback.PlayCue(EffectCue.Guard, aContact, transform.rotation, 1f, mGuardColor)) aFeedback.SpawnSparks(aContact, mGuardColor, 8, 7f);
                aFeedback.SpawnDamageNumber(transform.position + Vector3.up * 2.2f, GuardRules.ChipDamage(iHit.Damage, mSelf.Health),
                    new Color(0.7f, 0.75f, 0.85f), 0.9f);
            }
            return HitOutcome.Guarded;
        }

        /// <summary>ガード中に体の前に出す盾。受け止めた瞬間に大きく弾んで光る</summary>
        void UpdateShield()
        {
            if (mShield == null)
            {
                if (CombatFeedback.Instance == null) return;
                var aObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                aObject.name = "GuardShield";
                Destroy(aObject.GetComponent<Collider>());
                mShield = aObject.transform;
                mShield.SetParent(transform, false);
                mShield.localPosition = new Vector3(0f, 1.0f, 0.7f);
                mShield.localRotation = Quaternion.Euler(90f, 0f, 0f);
                mShieldRenderer = aObject.GetComponent<Renderer>();
                if (CombatFeedback.Instance.EffectMaterial != null) mShieldRenderer.sharedMaterial = CombatFeedback.Instance.EffectMaterial;
                mShieldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            float aPop = Mathf.Clamp01(1f - (Time.unscaledTime - mShieldPopTime) / mShieldPopDuration);
            bool aVisible = IsGuarding || aPop > 0f;
            if (mShield.gameObject.activeSelf != aVisible) mShield.gameObject.SetActive(aVisible);
            if (!aVisible) return;
            float aSize = ShieldSize * (1f + (mParryPop ? 0.6f : 0.3f) * aPop);
            mShield.localScale = new Vector3(aSize, 0.03f, aSize);
            var aColor = Color.Lerp(mGuardColor, mParryPop ? mParryColor : Color.white, aPop);
            mShieldRenderer.material.color = aColor;
        }
    }
}
