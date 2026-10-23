using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>ヒットストップ、画面揺れ、ダメージ表示、衝撃波などの手応えの演出</summary>
    public partial class CombatFeedback : MonoBehaviour
    {
        public struct DamageNumber
        {
            public Vector3 Position;
            public string Text;
            public Color Color;
            public float Time;
            /// <summary>文字の大きさ(1 = ふつう)。弱点・締め・スワップストライクほど大きい</summary>
            public float Scale;
        }

        /// <summary>インパクトフレーム(画面が白く光る)が消えるまでの時間</summary>
        public const float FlashDuration = 0.09f;
        /// <summary>画面の縁が武器の色に光る演出が消えるまでの時間</summary>
        public const float VignetteDuration = 0.35f;

        public const float DamageNumberLifetime = 0.8f;

        [SerializeField, FormerlySerializedAs("cameraRig")] ThirdPersonCamera mCameraRig;
        [SerializeField, Tooltip("衝撃波に使うマテリアル"), FormerlySerializedAs("effectMaterial")] Material mEffectMaterial;
        [SerializeField, FormerlySerializedAs("hitStopTimeScale")] float mHitStopTimeScale = 0.05f;
        [SerializeField, FormerlySerializedAs("maxHitStop")] float mMaxHitStop = 0.15f;

        float mHitStopEnd;
        float mSlowMotionEnd;
        float mSlowMotionScale = 1f;
        float mFlashStrength;
        float mFlashTime = -99f;
        float mVignetteStrength;
        float mVignetteTime = -99f;

        public static CombatFeedback Instance { get; private set; }
        /// <summary>実行時に作るエフェクトや弾に使うマテリアル(シェーダーがビルドに残るようシーンから参照)</summary>
        public Material EffectMaterial => mEffectMaterial;
        public List<DamageNumber> DamageNumbers { get; } = new List<DamageNumber>();

        /// <summary>今のインパクトフレームの明るさ(0 〜 1)。HUD が全面に白を重ねる</summary>
        public float FlashAmount => mFlashStrength * Mathf.Clamp01(1f - (Time.unscaledTime - mFlashTime) / FlashDuration);
        /// <summary>今の画面の縁の光の強さ(0 〜 1)と色</summary>
        public float VignetteAmount => mVignetteStrength * Mathf.Clamp01(1f - (Time.unscaledTime - mVignetteTime) / VignetteDuration);
        public Color VignetteColor { get; private set; } = Color.white;
        public ThirdPersonCamera CameraRig => mCameraRig;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GamePause.IsPaused) GamePause.Set(false);
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (!GamePause.IsPaused) Time.timeScale = CurrentTimeScale();
            DamageNumbers.RemoveAll(iNumber => Time.unscaledTime - iNumber.Time > DamageNumberLifetime);
        }

        /// <summary>ヒットストップ(ごく短く止める)とスローモーション(しばらく遅くする)のうち、遅い方</summary>
        float CurrentTimeScale()
        {
            float aScale = 1f;
            if (Time.unscaledTime < mSlowMotionEnd) aScale = mSlowMotionScale;
            if (Time.unscaledTime < mHitStopEnd) aScale = Mathf.Min(aScale, mHitStopTimeScale);
            return aScale;
        }

        public bool InSlowMotion => Time.unscaledTime < mSlowMotionEnd;

        public void HitStop(float iDuration)
        {
            if (iDuration <= 0f || GamePause.IsPaused) return;
            mHitStopEnd = Mathf.Max(mHitStopEnd, Time.unscaledTime + Mathf.Min(iDuration, mMaxHitStop));
            Time.timeScale = CurrentTimeScale();
        }

        /// <summary>全体をしばらく遅くする(ウェーブ最後の1体を倒した瞬間、チェンジドライブなど)</summary>
        public void SlowMotion(float iScale, float iDuration)
        {
            if (GamePause.IsPaused) return;
            mSlowMotionScale = Mathf.Clamp(iScale, 0.05f, 1f);
            mSlowMotionEnd = Time.unscaledTime + iDuration;
            Time.timeScale = CurrentTimeScale();
        }
    }
}
