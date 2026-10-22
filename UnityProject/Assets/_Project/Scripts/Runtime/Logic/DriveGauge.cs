using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// チェンジドライブ(切り替えの必殺技)のゲージ。0〜1。
    /// 切り替えて当てるほど早くたまる(ヒット・スワップストライク・弱点で増える)。満タンで使える。
    /// </summary>
    public class DriveGauge
    {
        public const float PerHit = 0.012f;
        public const float PerWeakHit = 0.02f;
        public const float PerSwapStrike = 0.08f;
        /// <summary>1回の攻撃(複数の敵に当たっても)で増える上限。範囲攻撃だけでたまりすぎないように</summary>
        public const float MaxPerAttack = 0.06f;

        public float Value { get; private set; }
        public bool IsFull => Value >= 1f;

        public void Add(float iAmount) => Value = Mathf.Clamp01(Value + Mathf.Max(0f, iAmount));

        /// <summary>1回の攻撃で当たったぶんを足す</summary>
        public void AddFromAttack(int iHits, int iWeakHits, bool iSwapStrike)
        {
            if (iHits <= 0) return;
            float aAmount = iHits * PerHit + iWeakHits * PerWeakHit;
            if (iSwapStrike) aAmount += PerSwapStrike;
            Add(Mathf.Min(aAmount, MaxPerAttack + (iSwapStrike ? PerSwapStrike : 0f)));
        }

        /// <summary>満タンなら使って空にする</summary>
        public bool TryConsume()
        {
            if (!IsFull) return false;
            Value = 0f;
            return true;
        }
    }
}
