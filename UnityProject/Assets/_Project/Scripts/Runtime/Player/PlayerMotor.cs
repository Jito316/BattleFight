using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// CharacterController ベースの移動。
    /// 技の実行中は LocomotionEnabled を切り、SkillExecutor が Displace で動かす。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("moveSpeed")] float mMoveSpeed = 7.5f;
        [SerializeField, FormerlySerializedAs("acceleration")] float mAcceleration = 60f;
        [SerializeField, FormerlySerializedAs("airAcceleration")] float mAirAcceleration = 25f;
        [SerializeField, FormerlySerializedAs("jumpVelocity")] float mJumpVelocity = 10f;
        [SerializeField, FormerlySerializedAs("gravity")] float mGravity = -30f;
        [SerializeField, FormerlySerializedAs("maxFallSpeed")] float mMaxFallSpeed = -40f;
        [SerializeField, FormerlySerializedAs("turnSpeed")] float mTurnSpeed = 1080f;
        [SerializeField, FormerlySerializedAs("impulseDamping")] float mImpulseDamping = 8f;

        CharacterController mController;
        Vector3 mPlanarVelocity;
        Vector3 mImpulseVelocity;
        Vector3 mPendingDisplacement;
        float mVerticalVelocity;

        /// <summary>カメラ基準の入力方向(長さ0〜1)</summary>
        public Vector3 DesiredDirection { get; set; }
        public bool LocomotionEnabled { get; set; } = true;
        /// <summary>ヒットストップ中: 動きを止める(速さは覚えておき、止まり終わったら続きから動く)</summary>
        public bool Frozen { get; set; }
        public float GravityScale { get; set; } = 1f;
        public bool IsGrounded { get; private set; }
        public float VerticalVelocity => mVerticalVelocity;

        void Awake()
        {
            mController = GetComponent<CharacterController>();
        }

        public bool TryJump()
        {
            if (!IsGrounded) return false;
            mVerticalVelocity = mJumpVelocity;
            IsGrounded = false;
            return true;
        }

        public void SetVerticalVelocity(float iVelocity) => mVerticalVelocity = iVelocity;

        public void Displace(Vector3 iDelta) => mPendingDisplacement += iDelta;

        public void AddImpulse(Vector3 iVelocity) => mImpulseVelocity += iVelocity;

        /// <summary>その場で移動する(ブリンク用)。壁などの当たり判定は効く。</summary>
        public void Teleport(Vector3 iDelta) => mController.Move(iDelta);

        public void FaceDirection(Vector3 iDirection)
        {
            iDirection.y = 0f;
            if (iDirection.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.LookRotation(iDirection);
        }

        void Update()
        {
            if (Frozen) return;
            float aDeltaTime = Time.deltaTime;

            Vector3 aDesired = DesiredDirection;
            aDesired.y = 0f;
            Vector3 aTargetVelocity = LocomotionEnabled ? Vector3.ClampMagnitude(aDesired, 1f) * mMoveSpeed : Vector3.zero;
            float aAccel = !LocomotionEnabled ? mAcceleration * 2f : IsGrounded ? mAcceleration : mAirAcceleration;
            mPlanarVelocity = Vector3.MoveTowards(mPlanarVelocity, aTargetVelocity, aAccel * aDeltaTime);

            if (LocomotionEnabled && aDesired.sqrMagnitude > 0.01f)
            {
                var aTargetRotation = Quaternion.LookRotation(aDesired);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, aTargetRotation, mTurnSpeed * aDeltaTime);
            }

            mVerticalVelocity = Mathf.Max(mVerticalVelocity + mGravity * GravityScale * aDeltaTime, mMaxFallSpeed);
            mImpulseVelocity = Vector3.Lerp(mImpulseVelocity, Vector3.zero, 1f - Mathf.Exp(-mImpulseDamping * aDeltaTime));

            Vector3 aMotion = (mPlanarVelocity + mImpulseVelocity + Vector3.up * mVerticalVelocity) * aDeltaTime + mPendingDisplacement;
            mPendingDisplacement = Vector3.zero;

            var aFlags = mController.Move(aMotion);
            IsGrounded = (aFlags & CollisionFlags.Below) != 0;
            if (IsGrounded && mVerticalVelocity < 0f) mVerticalVelocity = -2f;
            if ((aFlags & CollisionFlags.Above) != 0 && mVerticalVelocity > 0f) mVerticalVelocity = 0f;
        }
    }
}
