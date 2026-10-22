using System;
using System.Collections;
using System.Linq;
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
    /// <summary>UI Toolkit の画面(ステージ選択・HUD・スキル編成・リザルト)に、正しい中身が出ているかを確かめる。</summary>
    public class UiPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        SkillSlotController mSlots;

        public override void Setup()
        {
            base.Setup();
            // エリートはランダムに出るので、ここでは出さない(FunPlayTests で確かめる)
            ArenaDirector.EliteChanceOverride = 0f;
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            base.TearDown();
        }

        IEnumerator Load()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mSlots = Object.FindFirstObjectByType<SkillSlotController>();
        }

        static VisualElement RootOf<T>() where T : Component =>
            Object.FindFirstObjectByType<T>().GetComponent<UIDocument>().rootVisualElement;

        /// <summary>C# から出し入れしている要素が、今出ているか(レイアウトの計算を待たずに見る)</summary>
        static bool Displayed(VisualElement iElement) => iElement.style.display.keyword != StyleKeyword.Undefined
            ? iElement.style.display.keyword != StyleKeyword.None
            : iElement.style.display.value != DisplayStyle.None;

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
        }

        static IEnumerator WaitUntil(Func<bool> iCondition, float iTimeout)
        {
            float aStart = Time.realtimeSinceStartup;
            while (!iCondition())
            {
                if (Time.realtimeSinceStartup - aStart > iTimeout) Assert.Fail("タイムアウトしました");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator StageSelect_ListsEveryStage_AndStartsTheHighlightedOne()
        {
            yield return Load();
            var aRoot = RootOf<StageSelectUI>();
            yield return null;

            var aRows = aRoot.Query(className: "stage-row").ToList();
            Assert.AreEqual(mDirector.Stages.Count, aRows.Count, "ステージの数だけ行がない");
            Assert.IsTrue(Displayed(aRoot.Q("stage-select")));
            StringAssert.Contains(mDirector.Stages[0].DisplayName, aRows[0].Q<Label>(className: "stage-row__title").text);
            Assert.IsTrue(aRows[0].ClassListContains("stage-row--selected"));

            yield return Tap(mKeyboard.sKey);
            Assert.IsTrue(aRows[1].ClassListContains("stage-row--selected"), "S で次の行が選ばれない");
            yield return Tap(mKeyboard.enterKey);
            Assert.AreEqual(mDirector.Stages[1], mDirector.CurrentStage);
            yield return null;
            Assert.IsFalse(Displayed(aRoot.Q("stage-select")), "始めてもステージ選択が消えない");
        }

        [UnityTest]
        public IEnumerator Hud_SlotCards_ShowTheCurrentSkills()
        {
            yield return Load();
            mSlots.SetMode(SwapMode.Preset);
            mDirector.StartStage(1);
            yield return null;
            yield return null;
            var aRoot = RootOf<BattleHud>();

            Assert.IsTrue(Displayed(aRoot.Q("hud")), "戦闘が始まっても HUD が出ない");
            Assert.AreEqual(mSlots.GetCurrent(SlotType.AttackA).DisplayName, aRoot.Q("slot-a").Q<Label>("name").text);
            Assert.AreEqual(mSlots.GetCurrent(SlotType.Movement).DisplayName, aRoot.Q("slot-m").Q<Label>("name").text);

            yield return Tap(mKeyboard.digit2Key);
            yield return null;
            Assert.AreEqual("魔弾", aRoot.Q("slot-a").Q<Label>("name").text, "プリセットを切り替えてもパネルが変わらない");
            Assert.IsTrue(aRoot.Q("preset-2").Q("chip").ClassListContains("preset--current"));
        }

        [UnityTest]
        public IEnumerator Result_ShowsButtons_AfterGameOver()
        {
            yield return Load();
            mDirector.StartStage(1);
            yield return WaitUntil(() => mDirector.State == ArenaDirector.GameState.Fighting, 5f);
            Object.FindFirstObjectByType<SkillExecutor>().GetComponent<Damageable>().Kill();
            yield return null;
            yield return null;

            var aRoot = RootOf<BattleHud>();
            Assert.IsTrue(Displayed(aRoot.Q("result")));
            Assert.AreEqual("GAME OVER", aRoot.Q<Label>("result-title").text);
            Assert.AreEqual("もう一度 [R]", aRoot.Q<Button>("result-retry").text);
            Assert.IsFalse(Displayed(aRoot.Q<Button>("result-next")), "ゲームオーバーで「次のステージ」が出ている");
        }

        [UnityTest]
        public IEnumerator LoadoutEditor_InExploration_HidesSkillsNotCollected()
        {
            yield return Load();
            int aExplore = Enumerable.Range(0, mDirector.Stages.Count).First(iIndex => mDirector.Stages[iIndex].Kind == StageKind.Exploration);
            mDirector.StartStage(aExplore);
            yield return WaitUntil(() => mDirector.State == ArenaDirector.GameState.Fighting, 5f);

            yield return Tap(mKeyboard.pKey);
            Assert.IsTrue(LoadoutEditorUI.IsOpen);
            var aRoot = RootOf<LoadoutEditorUI>();
            Assert.IsTrue(Displayed(aRoot.Q("editor")));

            var aNames = aRoot.Query<Label>(className: "skill-row__name").ToList().Select(iLabel => iLabel.text).ToList();
            CollectionAssert.Contains(aNames, "連斬");
            CollectionAssert.DoesNotContain(aNames, "重撃", "手に入れていないスキルの名前が出ている");
            Assert.Greater(aRoot.Query(className: "skill-row--locked").ToList().Count, 0);
            Assert.IsFalse(Displayed(aRoot.Q("mode")), "探索中に切り替え方式のボタンが出ている");

            yield return Tap(mKeyboard.pKey);
            Assert.IsFalse(LoadoutEditorUI.IsOpen);
            Assert.IsFalse(Displayed(aRoot.Q("editor")));
        }
    }
}
