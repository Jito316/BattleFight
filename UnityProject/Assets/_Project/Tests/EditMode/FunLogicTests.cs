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
            var combo = new ComboCounter(2f);
            combo.AddHit(0f);
            combo.AddHit(1f, 3);
            Assert.AreEqual(4, combo.Count);
            Assert.AreEqual(0, combo.Tick(2.5f), "時間内なのに途切れた");
            Assert.AreEqual(4, combo.Tick(3.1f));
            Assert.AreEqual(0, combo.Count);
            Assert.AreEqual(4, combo.LastFinished);
            Assert.AreEqual(4, combo.Best);
        }

        [Test]
        public void Combo_BreaksImmediately_AndRatesBigCombos()
        {
            var combo = new ComboCounter();
            for (int i = 0; i < 30; i++) combo.AddHit(i * 0.1f);
            Assert.AreEqual(30, combo.Break(3f));
            Assert.AreEqual(0, combo.Break(3.1f), "途切れたあとにもう一度途切れた");
            Assert.AreEqual("", ComboCounter.Rating(9));
            Assert.AreEqual("NICE", ComboCounter.Rating(10));
            Assert.AreEqual("GREAT", ComboCounter.Rating(30));
            Assert.AreEqual("UNREAL", ComboCounter.Rating(120));
        }

        [Test]
        public void DriveGauge_FillsFasterWithWeaknessAndSwapStrike()
        {
            var plain = new DriveGauge();
            plain.AddFromAttack(1, 0, false);
            var clever = new DriveGauge();
            clever.AddFromAttack(1, 1, true);
            Assert.Greater(clever.Value, plain.Value * 3f);

            // 範囲攻撃でたくさん当てても、1回で増えるのは上限まで
            var area = new DriveGauge();
            area.AddFromAttack(20, 0, false);
            Assert.AreEqual(DriveGauge.MaxPerAttack, area.Value, 1e-5f);
        }

        [Test]
        public void DriveGauge_CanOnlyBeUsedWhenFull()
        {
            var gauge = new DriveGauge();
            gauge.Add(0.9f);
            Assert.IsFalse(gauge.TryConsume());
            gauge.Add(0.5f);
            Assert.AreEqual(1f, gauge.Value);
            Assert.IsTrue(gauge.TryConsume());
            Assert.AreEqual(0f, gauge.Value);
        }

        [Test]
        public void Elite_ChangesACopy_AndLeavesTheOriginalAlone()
        {
            var source = ScriptableObject.CreateInstance<EnemyProfile>();
            source.displayName = "雑兵";
            source.maxHealth = 100f;
            source.scale = 1f;
            source.attacks = new System.Collections.Generic.List<EnemyAttack> { new EnemyAttack { damage = 10f } };

            var giant = EliteMaker.Create(source, EliteAffix.Giant);
            Assert.AreEqual(EliteAffix.Giant, giant.elite);
            Assert.AreSame(source, giant.eliteSource);
            Assert.Greater(giant.maxHealth, source.maxHealth);
            Assert.Greater(giant.scale, source.scale);
            Assert.Greater(giant.attacks[0].damage, 10f);
            Assert.AreEqual(10f, source.attacks[0].damage, "元の敵の攻撃が変わってしまった");
            Assert.AreEqual(100f, source.maxHealth);

            var split = EliteMaker.CreateSplit(source);
            Assert.AreEqual(EliteAffix.None, split.elite);
            Assert.Less(split.maxHealth, source.maxHealth);
            Assert.Less(split.scale, source.scale);

            Object.DestroyImmediate(giant);
            Object.DestroyImmediate(split);
            Object.DestroyImmediate(source);
        }

        [Test]
        public void Elite_NeverAppliesToBosses()
        {
            var boss = ScriptableObject.CreateInstance<EnemyProfile>();
            boss.isBoss = true;
            Assert.AreEqual(EliteAffix.None, EliteMaker.Roll(boss, 1f, new System.Random(1)));
            boss.isBoss = false;
            Assert.AreNotEqual(EliteAffix.None, EliteMaker.Roll(boss, 1f, new System.Random(1)));
            Assert.AreEqual(EliteAffix.None, EliteMaker.Roll(boss, 0f, new System.Random(1)));
            Object.DestroyImmediate(boss);
        }

        [Test]
        public void HigherRank_MeansMoreDamage()
        {
            var config = ScriptableObject.CreateInstance<StyleRankConfig>();
            Assert.AreEqual(1f, config.DamageMultiplier(0));
            Assert.AreEqual(1.3f, config.DamageMultiplier(6), 1e-5f);
            Assert.AreEqual(1.3f, config.DamageMultiplier(99), 1e-5f, "ランクの範囲外で落ちる");
            for (int rank = 1; rank < config.rankNames.Length; rank++)
            {
                Assert.Greater(config.DamageMultiplier(rank), config.DamageMultiplier(rank - 1));
            }
            Object.DestroyImmediate(config);
        }
    }
}
