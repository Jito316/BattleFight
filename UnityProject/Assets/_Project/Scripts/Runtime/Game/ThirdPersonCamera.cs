using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 三人称のオービットカメラ。ロックオン中は対象の方へ自動で回り込む。
    /// ヒットストップ中も動くように unscaledDeltaTime で動かす。
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("target")] Transform mTarget;
        [SerializeField, FormerlySerializedAs("input")] PlayerInputReader mInput;
        [SerializeField, FormerlySerializedAs("lockOn")] LockOnSystem mLockOn;

        [SerializeField, FormerlySerializedAs("distance")] float mDistance = 7f;
        [SerializeField, FormerlySerializedAs("focusHeight")] float mFocusHeight = 1.6f;
        [SerializeField, FormerlySerializedAs("mouseSensitivity")] float mMouseSensitivity = 0.15f;
        [SerializeField, FormerlySerializedAs("stickSensitivity")] float mStickSensitivity = 180f;
        [SerializeField, FormerlySerializedAs("minPitch")] float mMinPitch = -15f;
        [SerializeField, FormerlySerializedAs("maxPitch")] float mMaxPitch = 65f;
        [SerializeField, FormerlySerializedAs("lockOnTurnSpeed")] float mLockOnTurnSpeed = 8f;
        [SerializeField, FormerlySerializedAs("lockOnPitch")] float mLockOnPitch = 15f;
        [SerializeField, FormerlySerializedAs("followSharpness")] float mFollowSharpness = 20f;

        float mYaw;
        float mPitch = 18f;
        float mShake;
        Vector3 mFocus;
        /// <summary>当たった向きに押される量(ばねで戻る)</summary>
        Vector3 mKick;
        /// <summary>ズームの量(画角を何度狭めるか)</summary>
        float mZoom;
        float mBaseFieldOfView;
        Camera mCamera;

        /// <summary>今のズームの量(テスト用)</summary>
        public float Zoom => mZoom;

        void Start()
        {
            mCamera = GetComponent<Camera>();
            if (mCamera != null) mBaseFieldOfView = mCamera.fieldOfView;
            if (mTarget != null)
            {
                mYaw = mTarget.eulerAngles.y;
                mFocus = mTarget.position + Vector3.up * mFocusHeight;
            }
#if !UNITY_WEBGL || UNITY_EDITOR
            // ブラウザではユーザー操作なしのポインターロック要求が拒否されるので、クリック時だけ要求する
            SetCursorLocked(true);
#endif
        }

        void Update()
        {
            if (GamePause.IsPaused) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            // メニュー(ステージ選択・リザルト)を出している間はクリックでカーソルを奪わない
            var aDirector = ArenaDirector.Instance;
            bool aInMenu = aDirector != null && (aDirector.State == ArenaDirector.GameState.StageSelect
                                              || aDirector.State == ArenaDirector.GameState.Cleared
                                              || aDirector.State == ArenaDirector.GameState.GameOver);
            if (!aInMenu && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }
        }

        public static void SetCursorLocked(bool iLocked)
        {
            Cursor.lockState = iLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !iLocked;
        }

        public void AddShake(float iAmount) => mShake = Mathf.Min(0.5f, mShake + iAmount);

        /// <summary>当たった向き(水平)へ、カメラを一瞬押す。ランダムな揺れより「どちらから殴ったか」が伝わる</summary>
        public void AddKick(Vector3 iDirection, float iAmount)
        {
            iDirection.y = 0f;
            if (iDirection.sqrMagnitude < 1e-4f) return;
            mKick = Vector3.ClampMagnitude(mKick + iDirection.normalized * iAmount, 0.6f);
        }

        /// <summary>一瞬だけズームする(重い一撃・技の締め)。iDegrees は画角を狭める角度</summary>
        public void AddZoom(float iDegrees) => mZoom = Mathf.Min(12f, Mathf.Max(mZoom, iDegrees));

        static readonly RaycastHit[] mCameraHits = new RaycastHit[16];

        /// <summary>
        /// 壁の向こうに回り込まないよう、注視点からカメラまでの間にある地形の手前で止める。
        /// プレイヤーや敵(Damageable を持つもの)は無視する。
        /// </summary>
        float ObstructedDistance(Vector3 iFrom, Vector3 iDirection, float iMaxDistance)
        {
            const float aRadius = 0.3f;
            int aCount = Physics.SphereCastNonAlloc(iFrom, aRadius, iDirection, mCameraHits, iMaxDistance, ~0, QueryTriggerInteraction.Ignore);
            float aNearest = iMaxDistance;
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aHit = mCameraHits[aIndex];
                if (aHit.distance <= 0f || aHit.collider.GetComponentInParent<Damageable>() != null) continue;
                if (mTarget != null && aHit.collider.transform.IsChildOf(mTarget)) continue;
                aNearest = Mathf.Min(aNearest, aHit.distance);
            }
            return Mathf.Max(1.2f, aNearest - 0.1f);
        }

        void LateUpdate()
        {
            if (mTarget == null || GamePause.IsPaused) return;
            float aDeltaTime = Time.unscaledDeltaTime;

            Vector2 aLook = mInput.StickLook * (mStickSensitivity * aDeltaTime);
            if (Cursor.lockState == CursorLockMode.Locked) aLook += mInput.MouseDelta * mMouseSensitivity;

            var aLockTarget = mLockOn != null ? mLockOn.Target : null;
            if (aLockTarget != null)
            {
                Vector3 aOffset = aLockTarget.CenterPoint - mTarget.position;
                aOffset.y = 0f;
                if (aOffset.sqrMagnitude > 0.01f)
                {
                    float aDesiredYaw = Mathf.Atan2(aOffset.x, aOffset.z) * Mathf.Rad2Deg;
                    mYaw = Mathf.LerpAngle(mYaw, aDesiredYaw, 1f - Mathf.Exp(-mLockOnTurnSpeed * aDeltaTime));
                }
                mPitch = Mathf.Lerp(mPitch, mLockOnPitch, 1f - Mathf.Exp(-4f * aDeltaTime));
            }
            else
            {
                mYaw += aLook.x;
                mPitch = Mathf.Clamp(mPitch - aLook.y, mMinPitch, mMaxPitch);
            }

            Vector3 aDesiredFocus = mTarget.position + Vector3.up * mFocusHeight;
            if (aLockTarget != null) aDesiredFocus = Vector3.Lerp(aDesiredFocus, aLockTarget.CenterPoint, 0.25f);
            mFocus = Vector3.Lerp(mFocus, aDesiredFocus, 1f - Mathf.Exp(-mFollowSharpness * aDeltaTime));

            Quaternion aRotation = Quaternion.Euler(mPitch, mYaw, 0f);
            Vector3 aBack = -(aRotation * Vector3.forward);
            Vector3 aPosition = mFocus + aBack * ObstructedDistance(mFocus, aBack, mDistance);
            aPosition.y = Mathf.Max(aPosition.y, 0.4f);
            aPosition += Random.insideUnitSphere * mShake + mKick;
            mShake = Mathf.Lerp(mShake, 0f, 1f - Mathf.Exp(-10f * aDeltaTime));
            mKick = Vector3.Lerp(mKick, Vector3.zero, 1f - Mathf.Exp(-14f * aDeltaTime));
            mZoom = Mathf.Lerp(mZoom, 0f, 1f - Mathf.Exp(-9f * aDeltaTime));
            if (mCamera != null && mBaseFieldOfView > 0f) mCamera.fieldOfView = mBaseFieldOfView - mZoom;

            transform.SetPositionAndRotation(aPosition, Quaternion.LookRotation(mFocus - aPosition));
        }
    }
}
