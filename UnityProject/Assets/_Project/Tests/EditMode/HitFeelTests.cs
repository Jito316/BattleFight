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

        [Test]
        public void LongerCombos_EmphasizeHitsMore_UpToALimit()
        {
            Assert.AreEqual(1f, HitEmphasis.ComboScale(0));
            Assert.Greater(HitEmphasis.ComboScale(20), HitEmphasis.ComboScale(5));
            Assert.AreEqual(HitEmphasis.MaxComboScale, HitEmphasis.ComboScale(HitEmphasis.FullComboCount), 1e-5f);
            Assert.AreEqual(HitEmphasis.MaxComboScale, HitEmphasis.ComboScale(999), 1e-5f, "上限を超えて大きくなる");
        }

        [Test]
        public void Finale_IsTheLastStepOfAMultiStepSkill()
        {
            var aCombo = UnityEngine.ScriptableObject.CreateInstance<SkillData>();
            aCombo.Steps = new System.Collections.Generic.List<SkillStep> { new SkillStep(), new SkillStep(), new SkillStep() };
            var aSingle = UnityEngine.ScriptableObject.CreateInstance<SkillData>();
            Assert.IsFalse(HitEmphasis.IsFinale(aCombo, 0, false));
            Assert.IsTrue(HitEmphasis.IsFinale(aCombo, 2, false));
            Assert.IsFalse(HitEmphasis.IsFinale(aSingle, 0, false), "1段だけの技まで締めになっている");
            Assert.IsTrue(HitEmphasis.IsFinale(aSingle, 0, true), "1段のフィニッシャーは締め");
            UnityEngine.Object.DestroyImmediate(aCombo);
            UnityEngine.Object.DestroyImmediate(aSingle);
        }

        [Test]
        public void ImportantHits_ShowBiggerNumbers()
        {
            float aNormal = HitEmphasis.DamageNumberScale(false, false, false, false, false);
            Assert.AreEqual(1f, aNormal);
            Assert.Greater(HitEmphasis.DamageNumberScale(true, false, false, false, false), aNormal, "弱点が大きくならない");
            Assert.Greater(HitEmphasis.DamageNumberScale(false, true, false, false, false),
                HitEmphasis.DamageNumberScale(true, false, false, false, false), "スワップストライクがいちばん大きくない");
        }

        [Test]
        public void Lunge_AndKnockback_DependOnTheWeapon()
        {
            Assert.AreEqual(0f, WeaponFeel.For(WeaponType.Gun).Lunge, "銃で踏み込んでいる");
            Assert.Greater(WeaponFeel.For(WeaponType.Gauntlet).Lunge, WeaponFeel.For(WeaponType.Sword).Lunge);
            Assert.Greater(WeaponFeel.For(WeaponType.Hammer).KnockbackScale, WeaponFeel.For(WeaponType.Sword).KnockbackScale);
        }

        [Test]
        public void LandingStrength_GrowsWithFallSpeed()
        {
            Assert.AreEqual(0f, PlayerJumpFeel.LandStrength(1f), "ふわっとした着地が重くなっている");
            Assert.AreEqual(1f, PlayerJumpFeel.LandStrength(50f), 1e-5f);
            Assert.Greater(PlayerJumpFeel.LandStrength(15f), PlayerJumpFeel.LandStrength(8f));
        }
    }
}
