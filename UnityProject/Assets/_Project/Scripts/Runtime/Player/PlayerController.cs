using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>カメラ基準の移動入力と、被弾・回避への反応をまとめる。</summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("input")] PlayerInputReader mInput;
        [SerializeField, FormerlySerializedAs("motor")] PlayerMotor mMotor;
        [SerializeField, FormerlySerializedAs("executor")] SkillExecutor mExecutor;
        [SerializeField, FormerlySerializedAs("damageable")] Damageable mDamageable;
        [SerializeField, FormerlySerializedAs("style")] StyleRankSystem mStyle;
        [SerializeField, FormerlySerializedAs("cameraTransform")] Transform mCameraTransform;

        [SerializeField, FormerlySerializedAs("hitStun")] float mHitStun = 0.35f;
        [SerializeField, FormerlySerializedAs("invulnerableAfterHit")] float mInvulnerableAfterHit = 0.6f;

        PlayerHitReaction mReaction;

        void Awake()
        {
            mReaction = GetComponent<PlayerHitReaction>();
            if (mReaction == null) mReaction = gameObject.AddComponent<PlayerHitReaction>();
            if (GetComponent<PlayerGuard>() == null) gameObject.AddComponent<PlayerGuard>();
        }

        void OnEnable()
        {
            mDamageable.Damaged += OnDamaged;
            mDamageable.Evaded += OnEvaded;
        }

        void OnDisable()
        {
            mDamageable.Damaged -= OnDamaged;
            mDamageable.Evaded -= OnEvaded;
        }

        void Update()
        {
            if (mDamageable.IsDead)
            {
                mMotor.DesiredDirection = Vector3.zero;
                return;
            }

            Vector2 aMove = mInput.Move;
            Transform aView = mCameraTransform != null ? mCameraTransform : transform;
            Vector3 aForward = Vector3.ProjectOnPlane(aView.forward, Vector3.up).normalized;
            Vector3 aRight = Vector3.ProjectOnPlane(aView.right, Vector3.up).normalized;
            mMotor.DesiredDirection = aForward * aMove.y + aRight * aMove.x;
        }

        void OnDamaged(HitInfo iHit, HitOutcome iOutcome)
        {
            mExecutor.Interrupt(mHitStun, mInvulnerableAfterHit);
            mMotor.AddImpulse(iHit.Direction * iHit.Knockback);
            mStyle.OnDamaged();

            // 受けたダメージが大きいほど、強く見せる(最大 HP の 1 割で 1、2 割以上で最大)
            float aRatio = mDamageable.MaxHealth > 0f ? mDamageable.LastDamage / mDamageable.MaxHealth : 0f;
            float aStrength = Mathf.Clamp(aRatio * 10f, 0.5f, 2f);
            bool aHeavy = aStrength >= 1.5f;

            // ヒットストップ: プレイヤーと、殴った敵だけが止まる(弾・爆発のときは撃った敵は止めない)
            float aFreeze = Mathf.Lerp(0.05f, 0.14f, Mathf.InverseLerp(0.5f, 2f, aStrength));
            mExecutor.Freeze(aFreeze);
            if (!iHit.Ranged && iHit.Source != null)
            {
                var aEnemy = iHit.Source.GetComponent<EnemyController>();
                if (aEnemy != null) aEnemy.Freeze(aFreeze);
            }
            mReaction.React(iHit.Direction, aStrength, aFreeze);

            SoundPlayer.Play(iHit.Ranged ? Sfx.HitGun : aHeavy ? Sfx.HitHammer : Sfx.HitGauntlet, 0.8f, aHeavy ? 0.8f : 0.9f);
            SoundPlayer.Play(Sfx.Hurt, 0.8f);
            Rumble.Play(Mathf.Clamp01(0.35f * aStrength), Mathf.Clamp01(0.3f * aStrength), 0.12f + 0.08f * aStrength);
            if (CombatFeedback.Instance != null)
            {
                var aFeedback = CombatFeedback.Instance;
                aFeedback.Shake(0.2f + 0.12f * aStrength);
                aFeedback.Kick(iHit.Direction, 0.12f * aStrength);
                aFeedback.Vignette(new Color(1f, 0.15f, 0.1f), Mathf.Clamp01(0.45f + 0.25f * aStrength));
                if (aHeavy)
                {
                    aFeedback.Flash(0.35f);
                    aFeedback.Punch(3f);
                }
                aFeedback.SpawnSparks(transform.position + Vector3.up * 1.1f - iHit.Direction * 0.3f, new Color(1f, 0.35f, 0.3f),
                    aHeavy ? 10 : 6, 7f);
                aFeedback.SpawnDamageNumber(transform.position + Vector3.up * 2.2f, mDamageable.LastDamage,
                    new Color(1f, 0.3f, 0.3f), aHeavy ? 1.5f : 1.2f);
            }
        }

        void OnEvaded(HitInfo iHit)
        {
            if (mExecutor.TryConsumeJustDodge())
            {
                mStyle.AddBonus(mStyle.Config.JustDodgeBonus, "JUST DODGE", new Color(0.6f, 1f, 0.6f));
                // 見切り: しばらく敵だけが遅くなる。避けてから反撃する時間
                EnemyTime.Slow();
                SoundPlayer.Play(Sfx.Dodge, 0.8f);
                if (CombatFeedback.Instance != null)
                {
                    CombatFeedback.Instance.SpawnShockwave(transform.position + Vector3.up * 0.05f, 3.5f, new Color(0.75f, 0.5f, 1f));
                    CombatFeedback.Instance.Shake(0.15f);
                }
            }
        }
    }
}
