using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class StageTests
    {
        readonly List<Object> created = new List<Object>();

        EnemyProfile Profile(string name)
        {
            var profile = ScriptableObject.CreateInstance<EnemyProfile>();
            profile.name = name;
            created.Add(profile);
            return profile;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Endless_EarlyWavesOnlyUseUnlockedEnemies()
        {
            var weak = Profile("weak");
            var strong = Profile("strong");
            var pool = new List<EndlessEntry>
            {
                new EndlessEntry { profile = weak, cost = 1f, minWave = 1 },
                new EndlessEntry { profile = strong, cost = 3f, minWave = 4 },
            };

            var wave1 = EndlessWaveGenerator.Generate(1, pool, null, 5, new System.Random(1));
            CollectionAssert.IsNotEmpty(wave1);
            CollectionAssert.DoesNotContain(wave1, strong);
        }

        [Test]
        public void Endless_StaysWithinBudgetAndCap()
        {
            var weak = Profile("weak");
            var pool = new List<EndlessEntry> { new EndlessEntry { profile = weak, cost = 1f, minWave = 1 } };

            for (int wave = 1; wave <= 30; wave++)
            {
                var enemies = EndlessWaveGenerator.Generate(wave, pool, null, 0, new System.Random(wave));
                Assert.LessOrEqual(enemies.Count, EndlessWaveGenerator.MaxEnemiesPerWave);
                Assert.LessOrEqual(enemies.Count, Mathf.FloorToInt(EndlessWaveGenerator.BudgetFor(wave)));
            }
        }

        [Test]
        public void Endless_LaterWavesAreBigger()
        {
            var weak = Profile("weak");
            var pool = new List<EndlessEntry> { new EndlessEntry { profile = weak, cost = 1f, minWave = 1 } };

            int early = EndlessWaveGenerator.Generate(1, pool, null, 0, new System.Random(3)).Count;
            int late = EndlessWaveGenerator.Generate(6, pool, null, 0, new System.Random(3)).Count;
            Assert.Greater(late, early);
        }

        [Test]
        public void Endless_BossWaves_AlternateBosses()
        {
            var weak = Profile("weak");
            var bossA = Profile("bossA");
            var bossB = Profile("bossB");
            var pool = new List<EndlessEntry> { new EndlessEntry { profile = weak, cost = 1f, minWave = 1 } };
            var bosses = new[] { bossA, bossB };

            Assert.IsFalse(EndlessWaveGenerator.IsBossWave(4, 5));
            Assert.IsTrue(EndlessWaveGenerator.IsBossWave(5, 5));
            Assert.AreSame(bossA, EndlessWaveGenerator.Generate(5, pool, bosses, 5, new System.Random(0))[0]);
            Assert.AreSame(bossB, EndlessWaveGenerator.Generate(10, pool, bosses, 5, new System.Random(0))[0]);
            Assert.AreSame(bossA, EndlessWaveGenerator.Generate(15, pool, bosses, 5, new System.Random(0))[0]);
        }

        [Test]
        public void Records_KeepBestTimeAndRank()
        {
            const string id = "Test_Stage_Records";
            StageRecords.Clear(id);
            try
            {
                Assert.IsFalse(StageRecords.IsCleared(id));
                Assert.IsTrue(StageRecords.RecordClear(id, 90f, 2));
                Assert.IsTrue(StageRecords.RecordClear(id, 80f, 1), "タイム更新");
                Assert.IsFalse(StageRecords.RecordClear(id, 85f, 2), "どちらも更新していない");
                Assert.IsTrue(StageRecords.RecordClear(id, 95f, 4), "ランク更新");

                Assert.IsTrue(StageRecords.IsCleared(id));
                Assert.AreEqual(80f, StageRecords.BestTime(id), 1e-4f);
                Assert.AreEqual(4, StageRecords.BestRank(id));

                Assert.IsTrue(StageRecords.RecordWave(id, 7));
                Assert.IsFalse(StageRecords.RecordWave(id, 5));
                Assert.AreEqual(7, StageRecords.BestWave(id));
            }
            finally
            {
                StageRecords.Clear(id);
            }
        }
    }
}
