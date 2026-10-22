using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class ExplorationSaveTests
    {
        SkillData mHammer;
        SkillData mWire;

        [SetUp]
        public void SetUp()
        {
            ExplorationSave.Reset();
            mHammer = ScriptableObject.CreateInstance<SkillData>();
            mHammer.name = "Test_Hammer";
            mWire = ScriptableObject.CreateInstance<SkillData>();
            mWire.name = "Test_Wire";
        }

        [TearDown]
        public void TearDown()
        {
            ExplorationSave.Reset();
            Object.DestroyImmediate(mHammer);
            Object.DestroyImmediate(mWire);
        }

        [Test]
        public void Unlock_OnlyCountsNewSkills()
        {
            Assert.IsFalse(ExplorationSave.HasStarted);
            Assert.IsTrue(ExplorationSave.Unlock(mHammer));
            Assert.IsFalse(ExplorationSave.Unlock(mHammer), "同じスキルを2回手に入れたことになった");
            Assert.IsTrue(ExplorationSave.IsUnlocked(mHammer));
            Assert.IsFalse(ExplorationSave.IsUnlocked(mWire));
            Assert.AreEqual(1, ExplorationSave.UnlockedCount);
        }

        [Test]
        public void Unlock_RaisesEventOnlyTheFirstTime()
        {
            int aRaised = 0;
            void OnUnlocked(SkillData _) => aRaised++;
            ExplorationSave.SkillUnlocked += OnUnlocked;
            ExplorationSave.Unlock(mWire);
            ExplorationSave.Unlock(mWire);
            ExplorationSave.SkillUnlocked -= OnUnlocked;
            Assert.AreEqual(1, aRaised);
        }

        [Test]
        public void Progress_SurvivesSaveAndLoad()
        {
            ExplorationSave.Unlock(mHammer);
            ExplorationSave.SetFlag("wall_hall_north");
            ExplorationSave.Checkpoint = "cp_tower";
            ExplorationSave.TotalSkills = 15;
            string aJson = ExplorationSave.Serialize();

            ExplorationSave.LoadFrom("");
            Assert.AreEqual(0, ExplorationSave.UnlockedCount);

            ExplorationSave.LoadFrom(aJson);
            Assert.IsTrue(ExplorationSave.IsUnlocked(mHammer));
            Assert.IsTrue(ExplorationSave.HasFlag("wall_hall_north"));
            Assert.IsFalse(ExplorationSave.HasFlag("wall_barracks_secret"));
            Assert.AreEqual("cp_tower", ExplorationSave.Checkpoint);
            Assert.AreEqual(15, ExplorationSave.TotalSkills);
        }

        [Test]
        public void BrokenData_StartsFresh()
        {
            ExplorationSave.LoadFrom("{ not json");
            Assert.AreEqual(0, ExplorationSave.UnlockedCount);
            Assert.IsNull(ExplorationSave.Checkpoint);
        }

        [Test]
        public void Reset_ForgetsEverything()
        {
            ExplorationSave.Unlock(mHammer);
            ExplorationSave.SetFlag("pickup_hammer_combo");
            ExplorationSave.Checkpoint = "cp_hall";
            ExplorationSave.Reset();
            Assert.AreEqual(0, ExplorationSave.UnlockedCount);
            Assert.IsFalse(ExplorationSave.HasFlag("pickup_hammer_combo"));
            Assert.IsNull(ExplorationSave.Checkpoint);
        }
    }
}
