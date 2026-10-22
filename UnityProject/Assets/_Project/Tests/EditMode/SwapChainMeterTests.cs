using NUnit.Framework;

namespace BattleFight.Tests
{
    public class SwapChainMeterTests
    {
        SwapChainMeter mMeter;

        [SetUp]
        public void SetUp() => mMeter = new SwapChainMeter(iLinkWindow: 2f, iKeepTime: 4f, iMaxChain: 3, iBonusPerChain: 0.1f);

        [Test]
        public void HitWithoutSwap_DoesNotChain()
        {
            Assert.IsFalse(mMeter.OnHit(1f));
            Assert.AreEqual(0, mMeter.Chain);
            Assert.AreEqual(1f, mMeter.DamageMultiplier, 1e-4f);
        }

        [Test]
        public void SwapThenHit_ChainsOncePerSwap()
        {
            mMeter.OnSwap(0f);
            Assert.IsTrue(mMeter.OnHit(0.5f));
            Assert.IsFalse(mMeter.OnHit(0.6f), "同じ切り替えで2回は上がらない");
            Assert.AreEqual(1, mMeter.Chain);
            Assert.AreEqual(1.1f, mMeter.DamageMultiplier, 1e-4f);

            mMeter.OnSwap(1f);
            Assert.IsTrue(mMeter.OnHit(1.2f));
            Assert.AreEqual(2, mMeter.Chain);
        }

        [Test]
        public void HitAfterLinkWindow_DoesNotChain()
        {
            mMeter.OnSwap(0f);
            Assert.IsFalse(mMeter.OnHit(2.5f));
            Assert.AreEqual(0, mMeter.Chain);
        }

        [Test]
        public void Chain_StopsAtMax()
        {
            for (int aI = 0; aI < 5; aI++)
            {
                mMeter.OnSwap(aI);
                mMeter.OnHit(aI + 0.1f);
            }
            Assert.AreEqual(3, mMeter.Chain);
            Assert.AreEqual(1.3f, mMeter.DamageMultiplier, 1e-4f);
        }

        [Test]
        public void Chain_BreaksAfterKeepTime()
        {
            mMeter.OnSwap(0f);
            mMeter.OnHit(0.1f);
            mMeter.Tick(3f);
            Assert.AreEqual(1, mMeter.Chain);
            Assert.Greater(mMeter.Remaining(3f), 0f);

            mMeter.Tick(4.5f);
            Assert.AreEqual(0, mMeter.Chain);
        }

        [Test]
        public void Reset_ClearsChainAndPendingSwap()
        {
            mMeter.OnSwap(0f);
            mMeter.OnHit(0.1f);
            mMeter.OnSwap(0.5f);
            mMeter.Reset();

            Assert.AreEqual(0, mMeter.Chain);
            Assert.IsFalse(mMeter.OnHit(0.6f), "リセット前の切り替えは無効");
        }
    }
}
