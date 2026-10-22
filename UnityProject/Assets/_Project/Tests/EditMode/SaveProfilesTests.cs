using System.Collections.Generic;
using NUnit.Framework;

namespace BattleFight.Tests
{
    /// <summary>テスト用: メモリに保存する</summary>
    public class MemorySaveStore : ISaveStore
    {
        public readonly Dictionary<string, object> Values = new Dictionary<string, object>();

        public bool HasKey(string iKey) => Values.ContainsKey(iKey);
        public string GetString(string iKey, string iDefault) => Values.TryGetValue(iKey, out var aValue) && aValue is string aText ? aText : iDefault;
        public void SetString(string iKey, string iValue) => Values[iKey] = iValue;
        public int GetInt(string iKey, int iDefault) => Values.TryGetValue(iKey, out var aValue) && aValue is int aNumber ? aNumber : iDefault;
        public void SetInt(string iKey, int iValue) => Values[iKey] = iValue;
        public float GetFloat(string iKey, float iDefault) => Values.TryGetValue(iKey, out var aValue) && aValue is float aNumber ? aNumber : iDefault;
        public void SetFloat(string iKey, float iValue) => Values[iKey] = iValue;
        public void DeleteKey(string iKey) => Values.Remove(iKey);
        public void Save() { }
    }

    /// <summary>セーブスロット: スロットごとに分かれる・消せる・前のデータがスロット 1 へ移る</summary>
    public class SaveProfilesTests
    {
        MemorySaveStore mStore;

        [SetUp]
        public void SetUp()
        {
            mStore = new MemorySaveStore();
            SaveProfiles.Store = mStore;
        }

        [TearDown]
        public void TearDown()
        {
            SaveProfiles.Store = new PlayerPrefsStore();
            ExplorationSave.LoadFrom("");
        }

        [Test]
        public void Slots_KeepTheirOwnData()
        {
            Assert.AreEqual(1, SaveProfiles.Current);
            StageRecords.RecordClear("Stage_1", 90f, 3);
            Assert.IsTrue(StageRecords.IsCleared("Stage_1"));

            SaveProfiles.Select(2);
            Assert.IsFalse(StageRecords.IsCleared("Stage_1"), "スロット 2 にスロット 1 の記録が見えている");
            Assert.IsFalse(SaveProfiles.IsUsed(2));
            StageRecords.RecordWave("Stage_Endless", 7);
            Assert.IsTrue(SaveProfiles.IsUsed(2));

            SaveProfiles.Select(1);
            Assert.IsTrue(StageRecords.IsCleared("Stage_1"));
            Assert.AreEqual(0, StageRecords.BestWave("Stage_Endless"));
        }

        [Test]
        public void SelectedSlot_IsRemembered()
        {
            SaveProfiles.Select(3);
            Assert.AreEqual(3, mStore.GetInt(SaveProfiles.CurrentKey, 0));

            // 読み込み直しても(ページを開き直しても)スロット 3 のまま
            SaveProfiles.Store = mStore;
            Assert.AreEqual(3, SaveProfiles.Current);
        }

        [Test]
        public void ExplorationProgress_FollowsTheSlot()
        {
            var aSkill = UnityEngine.ScriptableObject.CreateInstance<SkillData>();
            aSkill.name = "Skill_Test";
            ExplorationSave.Reset();
            Assert.IsTrue(ExplorationSave.Unlock(aSkill));

            SaveProfiles.Select(2);
            Assert.IsFalse(ExplorationSave.IsUnlocked(aSkill), "スロットを変えても前のスロットの探索が残っている");
            SaveProfiles.Select(1);
            Assert.IsTrue(ExplorationSave.IsUnlocked(aSkill));
            UnityEngine.Object.DestroyImmediate(aSkill);
        }

        [Test]
        public void Delete_ErasesOnlyThatSlot()
        {
            StageRecords.RecordClear("Stage_1", 90f, 3);
            SaveProfiles.Select(2);
            StageRecords.RecordClear("Stage_1", 50f, 5);
            int aChanged = 0;
            SaveProfiles.Changed += () => aChanged++;

            SaveProfiles.Delete(2);
            Assert.AreEqual(1, aChanged, "今のスロットを消したのに、読み直しを知らせていない");
            Assert.IsFalse(SaveProfiles.IsUsed(2));
            Assert.IsFalse(StageRecords.IsCleared("Stage_1"));
            Assert.IsNull(SaveProfiles.LastPlayed(2));

            SaveProfiles.Select(1);
            Assert.IsTrue(StageRecords.IsCleared("Stage_1"), "ほかのスロットまで消えた");
            Assert.AreEqual(90f, StageRecords.BestTime("Stage_1"), 1e-4f);
        }

        [Test]
        public void StartOver_ClearsOnlyExploration()
        {
            StageRecords.RecordClear("Stage_1", 90f, 3);
            StageRecords.RecordClear("Stage_10_Explore", 300f, 2);
            SaveProfiles.SetString(LoadoutStorage.SaveName, "{}");
            SaveProfiles.SetString(LoadoutStorage.ExplorationSaveName, "{}");

            ExplorationSave.StartOver("Stage_10_Explore");
            Assert.IsFalse(StageRecords.IsCleared("Stage_10_Explore"));
            Assert.IsFalse(SaveProfiles.HasKey(LoadoutStorage.ExplorationSaveName));
            Assert.IsTrue(StageRecords.IsCleared("Stage_1"), "探索以外の記録まで消えた");
            Assert.IsTrue(SaveProfiles.HasKey(LoadoutStorage.SaveName), "アリーナの編成まで消えた");
        }

        [Test]
        public void LegacyData_MovesIntoSlotOne()
        {
            // スロットを分ける前の保存データ
            mStore.SetString("BattleFight.Explore.v1", "{\"Skills\":[\"Skill_Sword_Combo\"]}");
            mStore.SetString("BattleFight.Loadout.v1", "{\"Mode\":1}");
            mStore.SetInt("BattleFight.Stage.Stage_1.cleared", 1);
            mStore.SetFloat("BattleFight.Stage.Stage_1.time", 75f);
            SaveProfiles.Store = mStore;

            Assert.AreEqual(1, SaveProfiles.Current);
            Assert.AreEqual("{\"Mode\":1}", SaveProfiles.GetString(LoadoutStorage.SaveName));
            Assert.IsTrue(StageRecords.IsCleared("Stage_1"));
            Assert.AreEqual(75f, StageRecords.BestTime("Stage_1"), 1e-4f);
            Assert.IsTrue(SaveProfiles.IsUsed(1));
            Assert.IsFalse(mStore.HasKey("BattleFight.Explore.v1"), "前のキーが残っている");
            Assert.IsFalse(mStore.HasKey("BattleFight.Stage.Stage_1.cleared"));

            // 移したあとにスロット 2 を選んでも、前のデータはスロット 2 に入らない
            SaveProfiles.Select(2);
            Assert.IsFalse(StageRecords.IsCleared("Stage_1"));
        }
    }
}
