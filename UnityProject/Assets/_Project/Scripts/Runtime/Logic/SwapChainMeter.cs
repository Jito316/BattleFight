using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// チェンジチェイン: 切り替えてから攻撃を当てるたびに1段上がり、段数に応じてダメージが増える。
    /// ・切り替えた後 <see cref="linkWindow"/> 秒以内に当てると +1(同じ切り替えで何度当てても +1 だけ)
    /// ・最後につないでから <see cref="keepTime"/> 秒切り替え+ヒットがないと途切れる
    /// ・被弾すると途切れる
    /// MonoBehaviour に依存しないのでテストできる。
    /// </summary>
    public class SwapChainMeter
    {
        readonly float linkWindow;
        readonly float keepTime;
        readonly int maxChain;
        readonly float bonusPerChain;

        float swappedAt = float.NegativeInfinity;
        bool linkPending;
        float lastLinkAt = float.NegativeInfinity;

        public SwapChainMeter(float linkWindow = 2.5f, float keepTime = 4f, int maxChain = 5, float bonusPerChain = 0.1f)
        {
            this.linkWindow = linkWindow;
            this.keepTime = keepTime;
            this.maxChain = maxChain;
            this.bonusPerChain = bonusPerChain;
        }

        public int Chain { get; private set; }
        public int MaxChain => maxChain;
        public float DamageMultiplier => 1f + bonusPerChain * Chain;

        /// <summary>途切れるまでの残り(1 → 0)。チェインがなければ 0</summary>
        public float Remaining(float now) => Chain == 0 ? 0f : Mathf.Clamp01(1f - (now - lastLinkAt) / keepTime);

        public void OnSwap(float now)
        {
            swappedAt = now;
            linkPending = true;
        }

        /// <summary>攻撃が当たったとき。チェインが上がったら true</summary>
        public bool OnHit(float now)
        {
            if (!linkPending || now - swappedAt > linkWindow) return false;
            linkPending = false;
            lastLinkAt = now;
            if (Chain >= maxChain) return false;
            Chain++;
            return true;
        }

        public void Tick(float now)
        {
            if (Chain > 0 && now - lastLinkAt > keepTime) Reset();
        }

        public void Reset()
        {
            Chain = 0;
            linkPending = false;
        }
    }
}
