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
        [SerializeField, Tooltip("ロックオン中に、カメラを右へずらす量(プレイヤーが標的を隠さないように)")] float mShoulderOffset = 1.1f;

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
        /// <summary>右肩へのずれ(0 〜 1)。ロックオンの付け外しで、なめらかに変える</summary>
        float mShoulder;
        /// <summary>大きな相手を見るために、いつもより引いている距離</summary>
        float mExtraDistance;

        /// <summary>この距離(体の表面から)より近い大きな敵がいたら、ロックオンしていなくても引く</summary>
        const float NearBigEnemyGap = 5f;

        /// <summary>この大きさの相手の全体が入るように、いつもより引く距離</summary>
        static float ExtraDistanceFor(float iRadius) => Mathf.Clamp((iRadius - 0.5f) * 4f, 0f, 4f);

        /// <summary>壁で 1m 近づけられるごとに、上へ逃がす高さ</summary>
        const float WallLiftPerMeter = 0.6f;
        const float MaxWallLift = 2.2f;
        /// <summary>今、壁をよけて上へ逃がしている高さ</summary>
        float mWallLift;
        /// <summary>壁や大きな敵で縮めている、今のカメラの距離(戻るときだけゆっくり)</summary>
        float mObstructedDistance = -1f;

        /// <summary>壁・大きな敵で縮めるときの、カメラの距離の下限</summary>
        const float MinWallDistance = 1.6f;
        const float MinBigEnemyDistance = 2.5f;
        /// <summary>上へ逃がした先がぶつかるときの、カメラの距離の下限(近すぎるときは体を隠す)</summary>
        const float LiftedMinDistance = 0.8f;
        /// <summary>カメラがプレイヤーの体にここまで近づいたら、体を隠す(影は残す)</summary>
        const float HideBodyDistance = 1.3f;
        /// <summary>プレイヤーの体を隠しているか</summary>
        public bool IsBodyHidden { get; private set; }

        /// <summary>カメラが近すぎるときに、プレイヤーの体(と持っている武器)を影だけにする</summary>
        void SetBodyHidden(bool iHidden)
        {
            if (IsBodyHidden == iHidden) return;
            IsBodyHidden = iHidden;
            var aFrom = iHidden ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            var aTo = iHidden ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var aRenderer in mTarget.GetComponentsInChildren<Renderer>())
            {
                if (aRenderer.shadowCastingMode == aFrom) aRenderer.shadowCastingMode = aTo;
            }
        }

        /// <summary>この高さ以上の敵は、壁と同じようにカメラが前へ回り込む(体でプレイヤーが隠れないように)</summary>
        const float BigEnemyHeight = 2.5f;

        /// <summary>今、壁や敵をよけて上へ逃がしている高さ(テスト用)</summary>
        public float WallLift => mWallLift;

        /// <summary>今のカメラの距離(テスト用)</summary>
        public float CurrentDistance => mDistance + mExtraDistance;

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

        Vector3 mFocusPoint;
        float mFocusStart = -99f;
        float mFocusDuration;

        /// <summary>倒した瞬間などに、その場所へカメラを寄せている途中か</summary>
        public bool IsFocusing => Time.unscaledTime - mFocusStart < mFocusDuration;

        /// <summary>しばらく iPoint の方へカメラを寄せ、ズームする(ボス・ウェーブ最後の 1 体を倒した瞬間)</summary>
        public void FocusOn(Vector3 iPoint, float iDuration, float iZoom)
        {
            mFocusPoint = iPoint;
            mFocusStart = Time.unscaledTime;
            mFocusDuration = Mathf.Max(0.05f, iDuration);
            AddZoom(iZoom);
        }

        /// <summary>カメラを一瞬沈ませる(着地の重さ)</summary>
        public void AddDip(float iAmount) => mKick = Vector3.ClampMagnitude(mKick + Vector3.down * iAmount, 0.6f);

        /// <summary>今のカメラの押され方(テスト用)</summary>
        public Vector3 KickOffset => mKick;

        /// <summary>一瞬だけズームする(重い一撃・技の締め)。iDegrees は画角を狭める角度</summary>
        public void AddZoom(float iDegrees) => mZoom = Mathf.Min(12f, Mathf.Max(mZoom, iDegrees));

        static readonly RaycastHit[] mCameraHits = new RaycastHit[16];

        /// <summary>
        /// 壁の向こうに回り込まないよう、注視点からカメラまでの間にある地形の手前で止める。
        /// プレイヤーや敵(Damageable を持つもの)は無視する。
        /// </summary>
        float ObstructedDistance(Vector3 iFrom, Vector3 iDirection, float iMaxDistance, float iMinWallDistance, out float oWallDistance)
        {
            const float aRadius = 0.3f;
            int aCount = Physics.SphereCastNonAlloc(iFrom, aRadius, iDirection, mCameraHits, iMaxDistance, ~0, QueryTriggerInteraction.Ignore);
            float aNearestWall = iMaxDistance;
            float aNearestEnemy = iMaxDistance;
            for (int aIndex = 0; aIndex < aCount; aIndex++)
            {
                var aHit = mCameraHits[aIndex];
                if (aHit.distance <= 0f) continue;
                if (mTarget != null && aHit.collider.transform.IsChildOf(mTarget)) continue;
                // 敵の体はふつうは通り抜けるが、大きな敵は壁と同じに扱う
                if (aHit.collider.GetComponentInParent<Damageable>() != null)
                {
                    var aEnemy = aHit.collider.GetComponentInParent<EnemyController>();
                    if (aEnemy == null || aEnemy.IsDead || aEnemy.Height < BigEnemyHeight) continue;
                    aNearestEnemy = Mathf.Min(aNearestEnemy, aHit.distance);
                    continue;
                }
                aNearestWall = Mathf.Min(aNearestWall, aHit.distance);
            }
            // 大きな敵の手前には、近づきすぎない(それより近い敵なら、カメラが敵の体の中に入る。中からは敵の面が見えないので隠れない)
            float aWall = Mathf.Max(iMinWallDistance, aNearestWall - 0.1f);
            float aEnemyLimit = Mathf.Max(MinBigEnemyDistance, aNearestEnemy - 0.1f);
            oWallDistance = aWall;
            return Mathf.Min(aWall, aEnemyLimit);
        }

        void LateUpdate()
        {
            if (mTarget == null || GamePause.IsPaused) return;
            float aDeltaTime = Time.unscaledDeltaTime;

            Vector2 aLook = mInput.StickLook * (mStickSensitivity * aDeltaTime);
            if (Cursor.lockState == CursorLockMode.Locked) aLook += mInput.MouseDelta * mMouseSensitivity;
            aLook *= GameSettings.CameraSensitivity;

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
            if (IsFocusing)
            {
                // 寄って、また戻る(山なり)
                float aProgress = (Time.unscaledTime - mFocusStart) / mFocusDuration;
                float aWeight = 0.55f * Mathf.Sin(Mathf.Clamp01(aProgress) * Mathf.PI);
                aDesiredFocus = Vector3.Lerp(aDesiredFocus, mFocusPoint, Mathf.Clamp01(aWeight));
                // 寄せている間はズームを保つ
                mZoom = Mathf.Max(mZoom, 7f * (1f - aProgress));
            }
            mFocus = Vector3.Lerp(mFocus, aDesiredFocus, 1f - Mathf.Exp(-mFollowSharpness * aDeltaTime));

            Quaternion aRotation = Quaternion.Euler(mPitch, mYaw, 0f);
            // ロックオン中は、見る点ごと右へずらす(プレイヤーは画面の左寄り、標的は真ん中寄りに見える)
            mShoulder = Mathf.MoveTowards(mShoulder, aLockTarget != null ? 1f : 0f, 3f * aDeltaTime);
            Vector3 aViewFocus = mFocus + aRotation * Vector3.right * (mShoulderOffset * mShoulder);
            Vector3 aBack = -(aRotation * Vector3.forward);
            // 大きな相手(ボス・巨大なエリート)にロックオンしたら、全体が入るよう引く
            float aWantExtra = aLockTarget != null ? ExtraDistanceFor(aLockTarget.Radius) : 0f;
            // ロックオンしていなくても、すぐ近くの大きな敵で画面がふさがらないよう少し引く
            foreach (var aEnemy in EnemyController.Active)
            {
                if (aEnemy == null || aEnemy.IsDead) continue;
                float aGap = Vector3.Distance(aEnemy.transform.position, mTarget.position) - aEnemy.Radius;
                if (aGap <= NearBigEnemyGap) aWantExtra = Mathf.Max(aWantExtra, ExtraDistanceFor(aEnemy.Radius) * 0.7f);
            }
            mExtraDistance = Mathf.Lerp(mExtraDistance, aWantExtra, 1f - Mathf.Exp(-3f * aDeltaTime));
            float aWantDistance = mDistance + mExtraDistance;
            float aDistance = ObstructedDistance(aViewFocus, aBack, aWantDistance, MinWallDistance, out float aWallDistance);
            // 近づくのはすぐ、離れるのはゆっくり(大きな敵が動いても、カメラががたつかないように)
            if (mObstructedDistance < 0f || aDistance < mObstructedDistance) mObstructedDistance = aDistance;
            else mObstructedDistance = Mathf.Lerp(mObstructedDistance, aDistance, 1f - Mathf.Exp(-3f * aDeltaTime));
            aDistance = mObstructedDistance;
            // 壁で近づけられたぶん、上へ逃がして見下ろす(プレイヤーの体で画面がふさがらないように)
            // 大きな敵のときは上げない(上げると水平の距離が縮み、かえってプレイヤーに寄ってしまう)
            float aWantLift = Mathf.Clamp((aWantDistance - aWallDistance) * WallLiftPerMeter, 0f, MaxWallLift);
            mWallLift = Mathf.Lerp(mWallLift, aWantLift, 1f - Mathf.Exp(-4f * aDeltaTime));
            // 上へ逃がした先が、天井や台にめり込まないよう、もう一度ぶつかるところを調べて手前で止める
            Vector3 aCameraOffset = aBack * aDistance + Vector3.up * mWallLift;
            float aLength = aCameraOffset.magnitude;
            Vector3 aOffsetDirection = aCameraOffset / aLength;
            // (大きな敵の体の中に入るのはかまわないので、壁だけで止める)
            ObstructedDistance(aViewFocus, aOffsetDirection, aLength, LiftedMinDistance, out float aLiftedWallDistance);
            Vector3 aPosition = aViewFocus + aOffsetDirection * Mathf.Min(aLength, aLiftedWallDistance);
            aPosition.y = Mathf.Max(aPosition.y, 0.4f);
            aPosition += Random.insideUnitSphere * mShake + mKick;
            mShake = Mathf.Lerp(mShake, 0f, 1f - Mathf.Exp(-10f * aDeltaTime));
            mKick = Vector3.Lerp(mKick, Vector3.zero, 1f - Mathf.Exp(-14f * aDeltaTime));
            mZoom = Mathf.Lerp(mZoom, 0f, 1f - Mathf.Exp(-9f * aDeltaTime));
            if (mCamera != null && mBaseFieldOfView > 0f) mCamera.fieldOfView = mBaseFieldOfView - mZoom;

            transform.SetPositionAndRotation(aPosition, Quaternion.LookRotation(aViewFocus - aPosition));
            SetBodyHidden(Vector3.Distance(aPosition, mTarget.position + Vector3.up * 1.2f) < HideBodyDistance);
        }
    }
}
