using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight
{
    /// <summary>
    /// ゲームパッドの振動。iLow は重い揺れ(左のモーター)、iHigh は細かい揺れ(右のモーター)。
    /// 決めた時間がたったら止める。ポーズ中・ウィンドウからフォーカスが外れたときも止める。
    /// WebGL ではブラウザによって振動しないことがある(そのときは何も起きない)。
    /// </summary>
    public class Rumble : MonoBehaviour
    {
        static Rumble mInstance;
        float mUntil;
        bool mActive;

        /// <summary>最後に振動させた強さ(テスト用。0 〜 1)</summary>
        public static float LastStrength { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mInstance = null;
            LastStrength = 0f;
        }

        public static void Play(float iLow, float iHigh, float iDuration)
        {
            if (!Application.isPlaying) return;
            LastStrength = Mathf.Max(iLow, iHigh);
            var aGamepad = Gamepad.current;
            if (aGamepad == null) return;
            if (mInstance == null) mInstance = new GameObject("Rumble").AddComponent<Rumble>();
            aGamepad.SetMotorSpeeds(Mathf.Clamp01(iLow), Mathf.Clamp01(iHigh));
            mInstance.mUntil = Mathf.Max(mInstance.mUntil, Time.unscaledTime + iDuration);
            mInstance.mActive = true;
        }

        void Update()
        {
            if (!mActive) return;
            if (Time.unscaledTime >= mUntil || GamePause.IsPaused) Stop();
        }

        void OnApplicationFocus(bool iFocused)
        {
            if (!iFocused) Stop();
        }

        void OnDestroy()
        {
            Stop();
            if (mInstance == this) mInstance = null;
        }

        void Stop()
        {
            mActive = false;
            if (Gamepad.current != null) Gamepad.current.ResetHaptics();
        }
    }
}
