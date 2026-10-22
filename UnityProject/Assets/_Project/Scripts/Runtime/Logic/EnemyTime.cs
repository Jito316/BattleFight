using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 敵だけの時間の速さ(ジャスト回避の「見切り」で敵だけ遅くする)。プレイヤーには効かない。
    /// 敵の動き・攻撃のタイミング・敵の弾は、Time.deltaTime にこれを掛けて進める。
    /// </summary>
    public static class EnemyTime
    {
        public const float SlowScale = 0.25f;
        public const float SlowDuration = 1.5f;

        static float mSlowUntil = float.NegativeInfinity;
        static float mSlowStartedAt = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mSlowUntil = float.NegativeInfinity;
            mSlowStartedAt = float.NegativeInfinity;
        }

        public static bool IsSlowed => Time.unscaledTime < mSlowUntil;
        public static float Scale => IsSlowed ? SlowScale : 1f;
        public static float DeltaTime => Time.deltaTime * Scale;

        /// <summary>残りの割合(1 = 始まった直後、0 = 終わり)</summary>
        public static float Remaining => IsSlowed ? Mathf.Clamp01((mSlowUntil - Time.unscaledTime) / SlowDuration) : 0f;
        public static float StartedAt => mSlowStartedAt;

        public static void Slow(float iDuration = SlowDuration)
        {
            mSlowUntil = Mathf.Max(mSlowUntil, Time.unscaledTime + iDuration);
            mSlowStartedAt = Time.unscaledTime;
        }

        public static void Clear() => mSlowUntil = float.NegativeInfinity;
    }
}
