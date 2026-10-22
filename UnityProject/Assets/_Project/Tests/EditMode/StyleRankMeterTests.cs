using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class StyleRankMeterTests
    {
        StyleRankConfig mConfig;
        StyleRankMeter mMeter;

        [SetUp]
        public void SetUp()
        {
            mConfig = ScriptableObject.CreateInstance<StyleRankConfig>();
            mMeter = new StyleRankMeter(mConfig);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(mConfig);

        [Test]
        public void StartsAtD()
        {
            Assert.AreEqual(0, mMeter.RankIndex);
            Assert.AreEqual("D", mMeter.RankName);
        }

        [Test]
        public void ReachingThreshold_RaisesRank()
        {
            mMeter.AddPoints(mConfig.RankThresholds[2]);

            Assert.AreEqual("B", mMeter.RankName);
            Assert.AreEqual(0f, mMeter.RankProgress, 1e-4f);
        }

        [Test]
        public void RepeatingSameMove_GainsLess()
        {
            float aFirst = mMeter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);
            float aSecond = mMeter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);
            float aThird = mMeter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);

            Assert.AreEqual(20f, aFirst, 1e-4f);
            Assert.Less(aSecond, aFirst);
            Assert.Less(aThird, aSecond);
        }

        [Test]
        public void RepeatPenalty_NeverGoesBelowMinimum()
        {
            float aGained = 0f;
            for (int aI = 0; aI < 20; aI++) aGained = mMeter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);

            Assert.AreEqual(20f * mConfig.MinRepeatMultiplier, aGained, 1e-4f);
        }

        [Test]
        public void SwitchingWeapon_GivesVarietyBonus()
        {
            mMeter.RegisterHit("sword#0", WeaponType.Sword, 20f, false);
            float aGained = mMeter.RegisterHit("hammer#0", WeaponType.Hammer, 20f, false);

            Assert.AreEqual(20f * mConfig.WeaponVarietyMultiplier, aGained, 1e-4f);
        }

        [Test]
        public void Arsenal_MultipliesGain()
        {
            float aGained = mMeter.RegisterHit("sword#0", WeaponType.Sword, 20f, true);

            Assert.AreEqual(20f * mConfig.ArsenalMultiplier, aGained, 1e-4f);
        }

        [Test]
        public void Damage_DropsRanks()
        {
            mMeter.AddPoints(mConfig.RankThresholds[4] + 10f);
            Assert.AreEqual("S", mMeter.RankName);

            mMeter.OnDamaged();

            Assert.AreEqual(4 - mConfig.RanksLostOnDamage, mMeter.RankIndex);
        }

        [Test]
        public void Decay_StartsOnlyAfterIdleDelay()
        {
            mMeter.AddPoints(300f);

            mMeter.Tick(mConfig.IdleDelay * 0.5f);
            Assert.AreEqual(300f, mMeter.Points, 1e-4f);

            mMeter.Tick(mConfig.IdleDelay);
            Assert.Less(mMeter.Points, 300f);
        }

        [Test]
        public void Points_AreClampedToMax()
        {
            mMeter.AddPoints(mConfig.MaxPoints * 3f);

            Assert.AreEqual(mConfig.MaxPoints, mMeter.Points, 1e-4f);
            Assert.AreEqual("SSS", mMeter.RankName);
        }
    }
}
