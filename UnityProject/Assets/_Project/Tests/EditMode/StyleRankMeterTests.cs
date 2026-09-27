using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class StyleRankMeterTests
    {
        StyleRankConfig config;
        StyleRankMeter meter;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<StyleRankConfig>();
            meter = new StyleRankMeter(config);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void StartsAtD()
        {
            Assert.AreEqual(0, meter.RankIndex);
            Assert.AreEqual("D", meter.RankName);
        }

        [Test]
        public void ReachingThreshold_RaisesRank()
        {
            meter.AddPoints(config.rankThresholds[2]);

            Assert.AreEqual("B", meter.RankName);
            Assert.AreEqual(0f, meter.RankProgress, 1e-4f);
        }

        [Test]
        public void RepeatingSameMove_GainsLess()
        {
            float first = meter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);
            float second = meter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);
            float third = meter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);

            Assert.AreEqual(20f, first, 1e-4f);
            Assert.Less(second, first);
            Assert.Less(third, second);
        }

        [Test]
        public void RepeatPenalty_NeverGoesBelowMinimum()
        {
            float gained = 0f;
            for (int i = 0; i < 20; i++) gained = meter.RegisterHit("combo#0", WeaponType.Sword, 20f, false);

            Assert.AreEqual(20f * config.minRepeatMultiplier, gained, 1e-4f);
        }

        [Test]
        public void SwitchingWeapon_GivesVarietyBonus()
        {
            meter.RegisterHit("sword#0", WeaponType.Sword, 20f, false);
            float gained = meter.RegisterHit("hammer#0", WeaponType.Hammer, 20f, false);

            Assert.AreEqual(20f * config.weaponVarietyMultiplier, gained, 1e-4f);
        }

        [Test]
        public void Arsenal_MultipliesGain()
        {
            float gained = meter.RegisterHit("sword#0", WeaponType.Sword, 20f, true);

            Assert.AreEqual(20f * config.arsenalMultiplier, gained, 1e-4f);
        }

        [Test]
        public void Damage_DropsRanks()
        {
            meter.AddPoints(config.rankThresholds[4] + 10f);
            Assert.AreEqual("S", meter.RankName);

            meter.OnDamaged();

            Assert.AreEqual(4 - config.ranksLostOnDamage, meter.RankIndex);
        }

        [Test]
        public void Decay_StartsOnlyAfterIdleDelay()
        {
            meter.AddPoints(300f);

            meter.Tick(config.idleDelay * 0.5f);
            Assert.AreEqual(300f, meter.Points, 1e-4f);

            meter.Tick(config.idleDelay);
            Assert.Less(meter.Points, 300f);
        }

        [Test]
        public void Points_AreClampedToMax()
        {
            meter.AddPoints(config.maxPoints * 3f);

            Assert.AreEqual(config.maxPoints, meter.Points, 1e-4f);
            Assert.AreEqual("SSS", meter.RankName);
        }
    }
}
