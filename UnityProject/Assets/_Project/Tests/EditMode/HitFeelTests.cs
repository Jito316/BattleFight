using System;
using NUnit.Framework;

namespace BattleFight.Tests
{
    /// <summary>武器ごとの手ごたえと、コードで作る効果音</summary>
    public class HitFeelTests
    {
        [Test]
        public void HeavyWeapons_StopAndShakeMore_ThanLightOnes()
        {
            var aHammer = WeaponFeel.For(WeaponType.Hammer);
            var aSword = WeaponFeel.For(WeaponType.Sword);
            var aGun = WeaponFeel.For(WeaponType.Gun);
            Assert.Greater(aHammer.Hitstop(0.05f, false), aSword.Hitstop(0.05f, false));
            Assert.Greater(aSword.Hitstop(0.05f, false), aGun.Hitstop(0.05f, false));
            Assert.Greater(aHammer.ShakeScale, aSword.ShakeScale);
            Assert.Greater(aHammer.ReactionScale, aGun.ReactionScale);
            Assert.Greater(aGun.Recoil, 0f, "銃に反動がない");
            Assert.AreEqual(0f, aSword.Recoil);
        }

        [Test]
        public void Hitstop_IsLongerOnArmorBreak_ButNeverTooLong()
        {
            var aSword = WeaponFeel.For(WeaponType.Sword);
            Assert.Greater(aSword.Hitstop(0.05f, true), aSword.Hitstop(0.05f, false));
            Assert.AreEqual(WeaponFeel.MaxHitstop, WeaponFeel.For(WeaponType.Hammer).Hitstop(0.2f, true), 1e-5f, "上限を超えて止まる");
        }

        [Test]
        public void EverySound_IsGenerated_AndNotTooLoud()
        {
            foreach (Sfx aSfx in Enum.GetValues(typeof(Sfx)))
            {
                var aSamples = SfxSynth.Generate(aSfx);
                Assert.Greater(aSamples.Length, SfxSynth.SampleRate / 50, $"{aSfx} が短すぎる");
                Assert.Less(aSamples.Length, SfxSynth.SampleRate, $"{aSfx} が長すぎる");
                float aPeak = 0f;
                foreach (var aSample in aSamples) aPeak = Math.Max(aPeak, Math.Abs(aSample));
                Assert.AreEqual(0.9f, aPeak, 0.01f, $"{aSfx} の音量がそろっていない");
            }
        }

        [Test]
        public void Sounds_AreTheSameEveryTime()
        {
            CollectionAssert.AreEqual(SfxSynth.Generate(Sfx.HitSword), SfxSynth.Generate(Sfx.HitSword));
            Assert.AreEqual(Sfx.HitHammer, SfxSynth.HitFor(WeaponType.Hammer));
            Assert.AreEqual(Sfx.HitSword, SfxSynth.HitFor(WeaponType.Sword));
        }
    }
}
