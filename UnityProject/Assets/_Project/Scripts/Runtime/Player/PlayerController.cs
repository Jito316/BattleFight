using UnityEngine;

namespace BattleFight
{
    /// <summary>カメラ基準の移動入力と、被弾・回避への反応をまとめる。</summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] SkillExecutor executor;
        [SerializeField] Damageable damageable;
        [SerializeField] StyleRankSystem style;
        [SerializeField] Transform cameraTransform;

        [SerializeField] float hitStun = 0.35f;
        [SerializeField] float invulnerableAfterHit = 0.6f;

        void OnEnable()
        {
            damageable.Damaged += OnDamaged;
            damageable.Evaded += OnEvaded;
        }

        void OnDisable()
        {
            damageable.Damaged -= OnDamaged;
            damageable.Evaded -= OnEvaded;
        }

        void Update()
        {
            if (damageable.IsDead)
            {
                motor.DesiredDirection = Vector3.zero;
                return;
            }

            Vector2 move = input.Move;
            Transform view = cameraTransform != null ? cameraTransform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            motor.DesiredDirection = forward * move.y + right * move.x;
        }

        void OnDamaged(HitInfo hit, HitOutcome outcome)
        {
            executor.Interrupt(hitStun, invulnerableAfterHit);
            motor.AddImpulse(hit.direction * hit.knockback);
            style.OnDamaged();
            if (CombatFeedback.Instance != null)
            {
                CombatFeedback.Instance.Shake(0.3f);
                CombatFeedback.Instance.SpawnDamageNumber(transform.position + Vector3.up * 2.2f, damageable.LastDamage,
                    new Color(1f, 0.3f, 0.3f));
            }
        }

        void OnEvaded(HitInfo hit)
        {
            if (executor.TryConsumeJustDodge())
            {
                style.AddBonus(style.Config.justDodgeBonus, "JUST DODGE", new Color(0.6f, 1f, 0.6f));
            }
        }
    }
}
