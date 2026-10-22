using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// チェンジチェイン: 切り替えてから攻撃を当てるたびに1段上がり、段数に応じてダメージが増える。
    /// ・切り替えた後 <see cref="mLinkWindow"/> 秒以内に当てると +1(同じ切り替えで何度当てても +1 だけ)
    /// ・最後につないでから <see cref="mKeepTime"/> 秒切り替え+ヒットがないと途切れる
    /// ・被弾すると途切れる
    /// MonoBehaviour に依存しないのでテストできる。
    /// </summary>
    public class SwapChainMeter
    {
        readonly float mLinkWindow;
        readonly float mKeepTime;
        readonly int mMaxChain;
        readonly float mBonusPerChain;

        float mSwappedAt = float.NegativeInfinity;
        bool mLinkPending;
        float mLastLinkAt = float.NegativeInfinity;

        public SwapChainMeter(float iLinkWindow = 2.5f, float iKeepTime = 4f, int iMaxChain = 5, float iBonusPerChain = 0.1f)
        {
            mLinkWindow = iLinkWindow;
            mKeepTime = iKeepTime;
            mMaxChain = iMaxChain;
            mBonusPerChain = iBonusPerChain;
        }

        public int Chain { get; private set; }
        public int MaxChain => mMaxChain;
        public float DamageMultiplier => 1f + mBonusPerChain * Chain;

        /// <summary>途切れるまでの残り(1 → 0)。チェインがなければ 0</summary>
        public float Remaining(float iNow) => Chain == 0 ? 0f : Mathf.Clamp01(1f - (iNow - mLastLinkAt) / mKeepTime);

        public void OnSwap(float iNow)
        {
            mSwappedAt = iNow;
            mLinkPending = true;
        }

        /// <summary>攻撃が当たったとき。チェインが上がったら true</summary>
        public bool OnHit(float iNow)
        {
            if (!mLinkPending || iNow - mSwappedAt > mLinkWindow) return false;
            mLinkPending = false;
            mLastLinkAt = iNow;
            if (Chain >= mMaxChain) return false;
            Chain++;
            return true;
        }

        public void Tick(float iNow)
        {
            if (Chain > 0 && iNow - mLastLinkAt > mKeepTime) Reset();
        }

        public void Reset()
        {
            Chain = 0;
            mLinkPending = false;
        }
    }
}
