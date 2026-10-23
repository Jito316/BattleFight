using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 武器の振りの動き。-1 が振りかぶり、+1 が振り抜き。
    /// 構え: 大きく引く(-1 を少し超える) / 攻撃判定: 一気に振り抜く(+1 を少し超える) / 硬直: 振り抜いた形を少し保ってから戻る
    /// </summary>
    public static class SwingCurve
    {
        /// <summary>振りかぶりの深さ(-1 を超えて引く)</summary>
        public const float WindupDepth = 1.15f;
        /// <summary>振り抜きの行き過ぎ(+1 を超えて振る)</summary>
        public const float FollowThrough = 1.25f;

        /// <summary>構え: 引き始めは速く、引き切る手前でゆっくり</summary>
        public static float Startup(float iProgress) => -WindupDepth * EaseOut(iProgress);

        /// <summary>攻撃判定: はじめに一気に振り抜く(切れ味)</summary>
        public static float Active(float iProgress) => Mathf.LerpUnclamped(-WindupDepth, FollowThrough, EaseOutCubic(iProgress));

        /// <summary>硬直: 振り抜いた形を少し残し(残心)、後半でなめらかに戻す</summary>
        public static float Recovery(float iProgress)
        {
            float aHold = Mathf.Clamp01((iProgress - 0.25f) / 0.75f);
            return Mathf.LerpUnclamped(FollowThrough, 0f, SmoothStep(aHold));
        }

        static float EaseOut(float iT)
        {
            iT = Mathf.Clamp01(iT);
            return 1f - (1f - iT) * (1f - iT);
        }

        static float EaseOutCubic(float iT)
        {
            iT = Mathf.Clamp01(iT);
            float aInverse = 1f - iT;
            return 1f - aInverse * aInverse * aInverse;
        }

        static float SmoothStep(float iT) => iT * iT * (3f - 2f * iT);
    }
}
