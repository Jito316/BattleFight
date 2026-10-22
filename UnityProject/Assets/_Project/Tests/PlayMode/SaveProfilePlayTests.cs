using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>ステージ選択のセーブスロット: 切り替え・探索のやり直し(確認つき)・スロットを消す</summary>
    public class SaveProfilePlayTests : InputTestFixture
    {
        /// <summary>テスト用: メモリに保存する(エディタの PlayerPrefs を汚さない)</summary>
        class MemoryStore : ISaveStore
        {
            readonly Dictionary<string, object> mValues = new Dictionary<string, object>();

            public bool HasKey(string iKey) => mValues.ContainsKey(iKey);
            public string GetString(string iKey, string iDefault) => mValues.TryGetValue(iKey, out var aValue) && aValue is string aText ? aText : iDefault;
            public void SetString(string iKey, string iValue) => mValues[iKey] = iValue;
            public int GetInt(string iKey, int iDefault) => mValues.TryGetValue(iKey, out var aValue) && aValue is int aNumber ? aNumber : iDefault;
            public void SetInt(string iKey, int iValue) => mValues[iKey] = iValue;
            public float GetFloat(string iKey, float iDefault) => mValues.TryGetValue(iKey, out var aValue) && aValue is float aNumber ? aNumber : iDefault;
            public void SetFloat(string iKey, float iValue) => mValues[iKey] = iValue;
            public void DeleteKey(string iKey) => mValues.Remove(iKey);
            public void Save() { }
        }

        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        StageSelectUI mSelect;
        SkillData mSkill;

        public override void Setup()
        {
            base.Setup();
            ArenaDirector.EliteChanceOverride = 0f;
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            SaveProfiles.Store = new MemoryStore();
        }

        public override void TearDown()
        {
            SaveProfiles.Store = new PlayerPrefsStore();
            ExplorationSave.LoadFrom("");
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            base.TearDown();
        }

        IEnumerator Load()
        {
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mSelect = Object.FindFirstObjectByType<StageSelectUI>();
            mSkill = Object.FindFirstObjectByType<SkillSlotController>().Database.Find("Skill_Sword_Combo");
        }

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
        }

        int ExplorationIndex()
        {
            for (int aStageIndex = 0; aStageIndex < mDirector.Stages.Count; aStageIndex++)
            {
                if (mDirector.Stages[aStageIndex] != null && mDirector.Stages[aStageIndex].Kind == StageKind.Exploration) return aStageIndex;
            }
            return -1;
        }

        IEnumerator HighlightExploration()
        {
            int aTarget = ExplorationIndex();
            Assert.GreaterOrEqual(aTarget, 0, "探索のステージがない");
            for (int aIndex = 0; aIndex < 20 && mSelect.Highlighted != aTarget; aIndex++) yield return Tap(mKeyboard.sKey);
            Assert.AreEqual(aTarget, mSelect.Highlighted);
        }

        static string Text(string iName) =>
            Object.FindFirstObjectByType<StageSelectUI>().GetComponent<UIDocument>().rootVisualElement.Q<Label>(iName).text;

        [UnityTest]
        public IEnumerator SwitchingSlots_ShowsThatSlotsProgress()
        {
            yield return Load();
            ExplorationSave.Unlock(mSkill);
            Assert.IsTrue(ExplorationSave.HasStarted);

            yield return Tap(mKeyboard.eKey);
            Assert.AreEqual(2, SaveProfiles.Current, "E でスロット 2 に切り替わらない");
            Assert.IsFalse(ExplorationSave.HasStarted, "スロット 2 にスロット 1 の探索が見えている");
            StringAssert.Contains("スロット2", Text("footer"));

            yield return Tap(mKeyboard.qKey);
            Assert.AreEqual(1, SaveProfiles.Current);
            Assert.IsTrue(ExplorationSave.HasStarted);
        }

        [UnityTest]
        public IEnumerator StartOver_AsksFirst_AndCancelKeepsTheData()
        {
            yield return Load();
            ExplorationSave.Unlock(mSkill);
            yield return HighlightExploration();

            // Backspace → 確認が出る。Esc でやめると消えない
            yield return Tap(mKeyboard.backspaceKey);
            Assert.IsTrue(mSelect.IsConfirming, "確認が出ない");
            yield return Tap(mKeyboard.escapeKey);
            Assert.IsFalse(mSelect.IsConfirming);
            Assert.IsTrue(ExplorationSave.HasStarted, "やめたのに消えた");

            // 最初は「やめる」が選ばれているので、Enter だけでは消えない
            yield return Tap(mKeyboard.backspaceKey);
            yield return Tap(mKeyboard.enterKey);
            Assert.IsTrue(ExplorationSave.HasStarted, "「やめる」を選んだまま Enter で消えた");

            // → で「消す」を選んで Enter
            yield return Tap(mKeyboard.backspaceKey);
            yield return Tap(mKeyboard.rightArrowKey);
            yield return Tap(mKeyboard.enterKey);
            Assert.IsFalse(mSelect.IsConfirming);
            Assert.IsFalse(ExplorationSave.HasStarted, "「消す」を選んでも消えない");
            Assert.AreEqual(ArenaDirector.GameState.StageSelect, mDirector.State, "Enter でステージが始まってしまった");
        }

        [UnityTest]
        public IEnumerator DeletingASlot_AsksFirst_AndErasesIt()
        {
            yield return Load();
            yield return Tap(mKeyboard.eKey);
            ExplorationSave.Unlock(mSkill);
            Assert.IsTrue(SaveProfiles.IsUsed(2));

            yield return Tap(mKeyboard.deleteKey);
            Assert.IsTrue(mSelect.IsConfirming);
            yield return Tap(mKeyboard.rightArrowKey);
            yield return Tap(mKeyboard.enterKey);
            Assert.IsFalse(SaveProfiles.IsUsed(2), "スロット 2 が消えていない");
            Assert.IsFalse(ExplorationSave.HasStarted);
            Assert.AreEqual(2, SaveProfiles.Current, "消したあともスロット 2 のまま");
        }
    }
}
