using NUnit.Framework;

namespace BattleFight.Tests
{
    public class SwapChainMeterTests
    {
        SwapChainMeter meter;

        [SetUp]
        public void SetUp() => meter = new SwapChainMeter(linkWindow: 2f, keepTime: 4f, maxChain: 3, bonusPerChain: 0.1f);

        [Test]
        public void HitWithoutSwap_DoesNotChain()
        {
            Assert.IsFalse(meter.OnHit(1f));
            Assert.AreEqual(0, meter.Chain);
            Assert.AreEqual(1f, meter.DamageMultiplier, 1e-4f);
        }

        [Test]
        public void SwapThenHit_ChainsOncePerSwap()
        {
            meter.OnSwap(0f);
            Assert.IsTrue(meter.OnHit(0.5f));
            Assert.IsFalse(meter.OnHit(0.6f), "同じ切り替えで2回は上がらない");
            Assert.AreEqual(1, meter.Chain);
            Assert.AreEqual(1.1f, meter.DamageMultiplier, 1e-4f);

            meter.OnSwap(1f);
            Assert.IsTrue(meter.OnHit(1.2f));
            Assert.AreEqual(2, meter.Chain);
        }

        [Test]
        public void HitAfterLinkWindow_DoesNotChain()
        {
            meter.OnSwap(0f);
            Assert.IsFalse(meter.OnHit(2.5f));
            Assert.AreEqual(0, meter.Chain);
        }

        [Test]
        public void Chain_StopsAtMax()
        {
            for (int i = 0; i < 5; i++)
            {
                meter.OnSwap(i);
                meter.OnHit(i + 0.1f);
            }
            Assert.AreEqual(3, meter.Chain);
            Assert.AreEqual(1.3f, meter.DamageMultiplier, 1e-4f);
        }

        [Test]
        public void Chain_BreaksAfterKeepTime()
        {
            meter.OnSwap(0f);
            meter.OnHit(0.1f);
            meter.Tick(3f);
            Assert.AreEqual(1, meter.Chain);
            Assert.Greater(meter.Remaining(3f), 0f);

            meter.Tick(4.5f);
            Assert.AreEqual(0, meter.Chain);
        }

        [Test]
        public void Reset_ClearsChainAndPendingSwap()
        {
            meter.OnSwap(0f);
            meter.OnHit(0.1f);
            meter.OnSwap(0.5f);
            meter.Reset();

            Assert.AreEqual(0, meter.Chain);
            Assert.IsFalse(meter.OnHit(0.6f), "リセット前の切り替えは無効");
        }
    }
}
