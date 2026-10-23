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
            SoundPlayer.Play(Sfx.Hurt, 0.8f);
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.Shake(0.3f);
                CombatFeedback.Instance.SpawnDamageNumber(transform.position + Vector3.up * 2.2f, mDamageable.LastDamage,
                    new Color(1f, 0.3f, 0.3f));
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
