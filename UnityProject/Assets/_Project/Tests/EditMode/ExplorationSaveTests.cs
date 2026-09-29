using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class ExplorationSaveTests
    {
        SkillData hammer;
        SkillData wire;

        [SetUp]
        public void SetUp()
        {
            ExplorationSave.Reset();
            hammer = ScriptableObject.CreateInstance<SkillData>();
            hammer.name = "Test_Hammer";
            wire = ScriptableObject.CreateInstance<SkillData>();
            wire.name = "Test_Wire";
        }

        [TearDown]
        public void TearDown()
        {
            ExplorationSave.Reset();
            Object.DestroyImmediate(hammer);
            Object.DestroyImmediate(wire);
        }

        [Test]
        public void Unlock_OnlyCountsNewSkills()
        {
            Assert.IsFalse(ExplorationSave.HasStarted);
            Assert.IsTrue(ExplorationSave.Unlock(hammer));
            Assert.IsFalse(ExplorationSave.Unlock(hammer), "同じスキルを2回手に入れたことになった");
            Assert.IsTrue(ExplorationSave.IsUnlocked(hammer));
            Assert.IsFalse(ExplorationSave.IsUnlocked(wire));
            Assert.AreEqual(1, ExplorationSave.UnlockedCount);
        }

        [Test]
        public void Unlock_RaisesEventOnlyTheFirstTime()
        {
            int raised = 0;
            void OnUnlocked(SkillData _) => raised++;
            ExplorationSave.SkillUnlocked += OnUnlocked;
            ExplorationSave.Unlock(wire);
            ExplorationSave.Unlock(wire);
            ExplorationSave.SkillUnlocked -= OnUnlocked;
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Progress_SurvivesSaveAndLoad()
        {
            ExplorationSave.Unlock(hammer);
            ExplorationSave.SetFlag("wall_hall_north");
            ExplorationSave.Checkpoint = "cp_tower";
            ExplorationSave.TotalSkills = 15;
            string json = ExplorationSave.Serialize();

            ExplorationSave.LoadFrom("");
            Assert.AreEqual(0, ExplorationSave.UnlockedCount);

            ExplorationSave.LoadFrom(json);
            Assert.IsTrue(ExplorationSave.IsUnlocked(hammer));
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
            ExplorationSave.Unlock(hammer);
            ExplorationSave.SetFlag("pickup_hammer_combo");
            ExplorationSave.Checkpoint = "cp_hall";
            ExplorationSave.Reset();
            Assert.AreEqual(0, ExplorationSave.UnlockedCount);
            Assert.IsFalse(ExplorationSave.HasFlag("pickup_hammer_combo"));
            Assert.IsNull(ExplorationSave.Checkpoint);
        }
    }
}
