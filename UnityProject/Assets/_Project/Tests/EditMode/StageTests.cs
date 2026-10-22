using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class StageTests
    {
        readonly List<Object> mCreated = new List<Object>();

        EnemyProfile Profile(string iName)
        {
            var aProfile = ScriptableObject.CreateInstance<EnemyProfile>();
            aProfile.name = iName;
            mCreated.Add(aProfile);
            return aProfile;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var aCreated in mCreated) Object.DestroyImmediate(aCreated);
            mCreated.Clear();
        }

        [Test]
        public void Endless_EarlyWavesOnlyUseUnlockedEnemies()
        {
            var aWeak = Profile("weak");
            var aStrong = Profile("strong");
            var aPool = new List<EndlessEntry>
            {
                new EndlessEntry { Profile = aWeak, Cost = 1f, MinWave = 1 },
                new EndlessEntry { Profile = aStrong, Cost = 3f, MinWave = 4 },
            };

            var aWave1 = EndlessWaveGenerator.Generate(1, aPool, null, 5, new System.Random(1));
            CollectionAssert.IsNotEmpty(aWave1);
            CollectionAssert.DoesNotContain(aWave1, aStrong);
        }

        [Test]
        public void Endless_StaysWithinBudgetAndCap()
        {
            var aWeak = Profile("weak");
            var aPool = new List<EndlessEntry> { new EndlessEntry { Profile = aWeak, Cost = 1f, MinWave = 1 } };

            for (int aWave = 1; aWave <= 30; aWave++)
            {
                var aEnemies = EndlessWaveGenerator.Generate(aWave, aPool, null, 0, new System.Random(aWave));
                Assert.LessOrEqual(aEnemies.Count, EndlessWaveGenerator.MaxEnemiesPerWave);
                Assert.LessOrEqual(aEnemies.Count, Mathf.FloorToInt(EndlessWaveGenerator.BudgetFor(aWave)));
            }
        }

        [Test]
        public void Endless_LaterWavesAreBigger()
        {
            var aWeak = Profile("weak");
            var aPool = new List<EndlessEntry> { new EndlessEntry { Profile = aWeak, Cost = 1f, MinWave = 1 } };

            int aEarly = EndlessWaveGenerator.Generate(1, aPool, null, 0, new System.Random(3)).Count;
            int aLate = EndlessWaveGenerator.Generate(6, aPool, null, 0, new System.Random(3)).Count;
            Assert.Greater(aLate, aEarly);
        }

        [Test]
        public void Endless_BossWaves_AlternateBosses()
        {
            var aWeak = Profile("weak");
            var aBossA = Profile("bossA");
            var aBossB = Profile("bossB");
            var aPool = new List<EndlessEntry> { new EndlessEntry { Profile = aWeak, Cost = 1f, MinWave = 1 } };
            var aBosses = new[] { aBossA, aBossB };

            Assert.IsFalse(EndlessWaveGenerator.IsBossWave(4, 5));
            Assert.IsTrue(EndlessWaveGenerator.IsBossWave(5, 5));
            Assert.AreSame(aBossA, EndlessWaveGenerator.Generate(5, aPool, aBosses, 5, new System.Random(0))[0]);
            Assert.AreSame(aBossB, EndlessWaveGenerator.Generate(10, aPool, aBosses, 5, new System.Random(0))[0]);
            Assert.AreSame(aBossA, EndlessWaveGenerator.Generate(15, aPool, aBosses, 5, new System.Random(0))[0]);
        }

        [Test]
        public void Records_KeepBestTimeAndRank()
        {
            const string aId = "Test_Stage_Records";
            StageRecords.Clear(aId);
            try
            {
                Assert.IsFalse(StageRecords.IsCleared(aId));
                Assert.IsTrue(StageRecords.RecordClear(aId, 90f, 2));
                Assert.IsTrue(StageRecords.RecordClear(aId, 80f, 1), "タイム更新");
                Assert.IsFalse(StageRecords.RecordClear(aId, 85f, 2), "どちらも更新していない");
                Assert.IsTrue(StageRecords.RecordClear(aId, 95f, 4), "ランク更新");

                Assert.IsTrue(StageRecords.IsCleared(aId));
                Assert.AreEqual(80f, StageRecords.BestTime(aId), 1e-4f);
                Assert.AreEqual(4, StageRecords.BestRank(aId));

                Assert.IsTrue(StageRecords.RecordWave(aId, 7));
                Assert.IsFalse(StageRecords.RecordWave(aId, 5));
                Assert.AreEqual(7, StageRecords.BestWave(aId));
            }
            finally
            {
                StageRecords.Clear(aId);
            }
        }
    }
}
