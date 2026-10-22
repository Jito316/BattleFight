using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>コンボ・チェンジドライブのゲージ・エリート・ランクによる強化の計算</summary>
    public class FunLogicTests
    {
        [Test]
        public void Combo_CountsHits_AndFinishesAfterTheWindow()
        {
            var aCombo = new ComboCounter(2f);
            aCombo.AddHit(0f);
            aCombo.AddHit(1f, 3);
            Assert.AreEqual(4, aCombo.Count);
            Assert.AreEqual(0, aCombo.Tick(2.5f), "時間内なのに途切れた");
            Assert.AreEqual(4, aCombo.Tick(3.1f));
            Assert.AreEqual(0, aCombo.Count);
            Assert.AreEqual(4, aCombo.LastFinished);
            Assert.AreEqual(4, aCombo.Best);
        }

        [Test]
        public void Combo_BreaksImmediately_AndRatesBigCombos()
        {
            var aCombo = new ComboCounter();
            for (int aIndex = 0; aIndex < 30; aIndex++) aCombo.AddHit(aIndex * 0.1f);
            Assert.AreEqual(30, aCombo.Break(3f));
            Assert.AreEqual(0, aCombo.Break(3.1f), "途切れたあとにもう一度途切れた");
            Assert.AreEqual("", ComboCounter.Rating(9));
            Assert.AreEqual("NICE", ComboCounter.Rating(10));
            Assert.AreEqual("GREAT", ComboCounter.Rating(30));
            Assert.AreEqual("UNREAL", ComboCounter.Rating(120));
        }

        [Test]
        public void DriveGauge_FillsFasterWithWeaknessAndSwapStrike()
        {
            var aPlain = new DriveGauge();
            aPlain.AddFromAttack(1, 0, false);
            var aClever = new DriveGauge();
            aClever.AddFromAttack(1, 1, true);
            Assert.Greater(aClever.Value, aPlain.Value * 3f);

            // 範囲攻撃でたくさん当てても、1回で増えるのは上限まで
            var aArea = new DriveGauge();
            aArea.AddFromAttack(20, 0, false);
            Assert.AreEqual(DriveGauge.MaxPerAttack, aArea.Value, 1e-5f);
        }

        [Test]
        public void DriveGauge_CanOnlyBeUsedWhenFull()
        {
            var aGauge = new DriveGauge();
            aGauge.Add(0.9f);
            Assert.IsFalse(aGauge.TryConsume());
            aGauge.Add(0.5f);
            Assert.AreEqual(1f, aGauge.Value);
            Assert.IsTrue(aGauge.TryConsume());
            Assert.AreEqual(0f, aGauge.Value);
        }

        [Test]
        public void Elite_ChangesACopy_AndLeavesTheOriginalAlone()
        {
            var aSource = ScriptableObject.CreateInstance<EnemyProfile>();
            aSource.DisplayName = "雑兵";
            aSource.MaxHealth = 100f;
            aSource.Scale = 1f;
            aSource.Attacks = new System.Collections.Generic.List<EnemyAttack> { new EnemyAttack { Damage = 10f } };

            var aGiant = EliteMaker.Create(aSource, EliteAffix.Giant);
            Assert.AreEqual(EliteAffix.Giant, aGiant.Elite);
            Assert.AreSame(aSource, aGiant.EliteSource);
            Assert.Greater(aGiant.MaxHealth, aSource.MaxHealth);
            Assert.Greater(aGiant.Scale, aSource.Scale);
            Assert.Greater(aGiant.Attacks[0].Damage, 10f);
            Assert.AreEqual(10f, aSource.Attacks[0].Damage, "元の敵の攻撃が変わってしまった");
            Assert.AreEqual(100f, aSource.MaxHealth);

            var aSplit = EliteMaker.CreateSplit(aSource);
            Assert.AreEqual(EliteAffix.None, aSplit.Elite);
            Assert.Less(aSplit.MaxHealth, aSource.MaxHealth);
            Assert.Less(aSplit.Scale, aSource.Scale);

            Object.DestroyImmediate(aGiant);
            Object.DestroyImmediate(aSplit);
            Object.DestroyImmediate(aSource);
        }

        [Test]
        public void Elite_NeverAppliesToBosses()
        {
            var aBoss = ScriptableObject.CreateInstance<EnemyProfile>();
            aBoss.IsBoss = true;
            Assert.AreEqual(EliteAffix.None, EliteMaker.Roll(aBoss, 1f, new System.Random(1)));
            aBoss.IsBoss = false;
            Assert.AreNotEqual(EliteAffix.None, EliteMaker.Roll(aBoss, 1f, new System.Random(1)));
            Assert.AreEqual(EliteAffix.None, EliteMaker.Roll(aBoss, 0f, new System.Random(1)));
            Object.DestroyImmediate(aBoss);
        }

        [Test]
        public void HigherRank_MeansMoreDamage()
        {
            var aConfig = ScriptableObject.CreateInstance<StyleRankConfig>();
            Assert.AreEqual(1f, aConfig.DamageMultiplier(0));
            Assert.AreEqual(1.3f, aConfig.DamageMultiplier(6), 1e-5f);
            Assert.AreEqual(1.3f, aConfig.DamageMultiplier(99), 1e-5f, "ランクの範囲外で落ちる");
            for (int aRank = 1; aRank < aConfig.RankNames.Length; aRank++)
            {
                Assert.Greater(aConfig.DamageMultiplier(aRank), aConfig.DamageMultiplier(aRank - 1));
            }
            Object.DestroyImmediate(aConfig);
        }
    }
}
