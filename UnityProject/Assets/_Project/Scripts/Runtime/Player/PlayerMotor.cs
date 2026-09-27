using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// CharacterController ベースの移動。
    /// 技の実行中は LocomotionEnabled を切り、SkillExecutor が Displace で動かす。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 7.5f;
        [SerializeField] float acceleration = 60f;
        [SerializeField] float airAcceleration = 25f;
        [SerializeField] float jumpVelocity = 10f;
        [SerializeField] float gravity = -30f;
        [SerializeField] float maxFallSpeed = -40f;
        [SerializeField] float turnSpeed = 1080f;
        [SerializeField] float impulseDamping = 8f;

        CharacterController controller;
        Vector3 planarVelocity;
        Vector3 impulseVelocity;
        Vector3 pendingDisplacement;
        float verticalVelocity;

        /// <summary>カメラ基準の入力方向(長さ0〜1)</summary>
        public Vector3 DesiredDirection { get; set; }
        public bool LocomotionEnabled { get; set; } = true;
        public float GravityScale { get; set; } = 1f;
        public bool IsGrounded { get; private set; }
        public float VerticalVelocity => verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public bool TryJump()
        {
            if (!IsGrounded) return false;
            verticalVelocity = jumpVelocity;
            IsGrounded = false;
            return true;
        }

        public void SetVerticalVelocity(float velocity) => verticalVelocity = velocity;

        public void Displace(Vector3 delta) => pendingDisplacement += delta;

        public void AddImpulse(Vector3 velocity) => impulseVelocity += velocity;

        /// <summary>その場で移動する(ブリンク用)。壁などの当たり判定は効く。</summary>
        public void Teleport(Vector3 delta) => controller.Move(delta);

        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            Vector3 desired = DesiredDirection;
            desired.y = 0f;
            Vector3 targetVelocity = LocomotionEnabled ? Vector3.ClampMagnitude(desired, 1f) * moveSpeed : Vector3.zero;
            float accel = !LocomotionEnabled ? acceleration * 2f : IsGrounded ? acceleration : airAcceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, accel * dt);

            if (LocomotionEnabled && desired.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(desired);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * dt);
            }

            verticalVelocity = Mathf.Max(verticalVelocity + gravity * GravityScale * dt, maxFallSpeed);
            impulseVelocity = Vector3.Lerp(impulseVelocity, Vector3.zero, 1f - Mathf.Exp(-impulseDamping * dt));

            Vector3 motion = (planarVelocity + impulseVelocity + Vector3.up * verticalVelocity) * dt + pendingDisplacement;
            pendingDisplacement = Vector3.zero;

            var flags = controller.Move(motion);
            IsGrounded = (flags & CollisionFlags.Below) != 0;
            if (IsGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        }
    }
}
