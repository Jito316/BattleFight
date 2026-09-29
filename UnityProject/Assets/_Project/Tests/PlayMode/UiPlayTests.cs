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
        const string SceneName = "Prototype_Arena";

        Keyboard keyboard;
        ArenaDirector director;
        SkillSlotController slots;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            base.TearDown();
        }

        IEnumerator Load()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            director = Object.FindFirstObjectByType<ArenaDirector>();
            slots = Object.FindFirstObjectByType<SkillSlotController>();
        }

        static VisualElement RootOf<T>() where T : Component =>
            Object.FindFirstObjectByType<T>().GetComponent<UIDocument>().rootVisualElement;

        /// <summary>C# から出し入れしている要素が、今出ているか(レイアウトの計算を待たずに見る)</summary>
        static bool Displayed(VisualElement element) => element.style.display.keyword != StyleKeyword.Undefined
            ? element.style.display.keyword != StyleKeyword.None
            : element.style.display.value != DisplayStyle.None;

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        static IEnumerator WaitUntil(Func<bool> condition, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout) Assert.Fail("タイムアウトしました");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator StageSelect_ListsEveryStage_AndStartsTheHighlightedOne()
        {
            yield return Load();
            var root = RootOf<StageSelectUI>();
            yield return null;

            var rows = root.Query(className: "stage-row").ToList();
            Assert.AreEqual(director.Stages.Count, rows.Count, "ステージの数だけ行がない");
            Assert.IsTrue(Displayed(root.Q("stage-select")));
            StringAssert.Contains(director.Stages[0].displayName, rows[0].Q<Label>(className: "stage-row__title").text);
            Assert.IsTrue(rows[0].ClassListContains("stage-row--selected"));

            yield return Tap(keyboard.sKey);
            Assert.IsTrue(rows[1].ClassListContains("stage-row--selected"), "S で次の行が選ばれない");
            yield return Tap(keyboard.enterKey);
            Assert.AreEqual(director.Stages[1], director.CurrentStage);
            yield return null;
            Assert.IsFalse(Displayed(root.Q("stage-select")), "始めてもステージ選択が消えない");
        }

        [UnityTest]
        public IEnumerator Hud_SlotCards_ShowTheCurrentSkills()
        {
            yield return Load();
            slots.SetMode(SwapMode.Preset);
            director.StartStage(1);
            yield return null;
            yield return null;
            var root = RootOf<BattleHud>();

            Assert.IsTrue(Displayed(root.Q("hud")), "戦闘が始まっても HUD が出ない");
            Assert.AreEqual(slots.GetCurrent(SlotType.AttackA).displayName, root.Q("slot-a").Q<Label>("name").text);
            Assert.AreEqual(slots.GetCurrent(SlotType.Movement).displayName, root.Q("slot-m").Q<Label>("name").text);

            yield return Tap(keyboard.digit2Key);
            yield return null;
            Assert.AreEqual("魔弾", root.Q("slot-a").Q<Label>("name").text, "プリセットを切り替えてもパネルが変わらない");
            Assert.IsTrue(root.Q("preset-2").Q("chip").ClassListContains("preset--current"));
        }

        [UnityTest]
        public IEnumerator Result_ShowsButtons_AfterGameOver()
        {
            yield return Load();
            director.StartStage(1);
            yield return WaitUntil(() => director.State == ArenaDirector.GameState.Fighting, 5f);
            Object.FindFirstObjectByType<SkillExecutor>().GetComponent<Damageable>().Kill();
            yield return null;
            yield return null;

            var root = RootOf<BattleHud>();
            Assert.IsTrue(Displayed(root.Q("result")));
            Assert.AreEqual("GAME OVER", root.Q<Label>("result-title").text);
            Assert.AreEqual("もう一度 [R]", root.Q<Button>("result-retry").text);
            Assert.IsFalse(Displayed(root.Q<Button>("result-next")), "ゲームオーバーで「次のステージ」が出ている");
        }

        [UnityTest]
        public IEnumerator LoadoutEditor_InExploration_HidesSkillsNotCollected()
        {
            yield return Load();
            int explore = Enumerable.Range(0, director.Stages.Count).First(i => director.Stages[i].kind == StageKind.Exploration);
            director.StartStage(explore);
            yield return WaitUntil(() => director.State == ArenaDirector.GameState.Fighting, 5f);

            yield return Tap(keyboard.pKey);
            Assert.IsTrue(LoadoutEditorUI.IsOpen);
            var root = RootOf<LoadoutEditorUI>();
            Assert.IsTrue(Displayed(root.Q("editor")));

            var names = root.Query<Label>(className: "skill-row__name").ToList().Select(l => l.text).ToList();
            CollectionAssert.Contains(names, "連斬");
            CollectionAssert.DoesNotContain(names, "重撃", "手に入れていないスキルの名前が出ている");
            Assert.Greater(root.Query(className: "skill-row--locked").ToList().Count, 0);
            Assert.IsFalse(Displayed(root.Q("mode")), "探索中に切り替え方式のボタンが出ている");

            yield return Tap(keyboard.pKey);
            Assert.IsFalse(LoadoutEditorUI.IsOpen);
            Assert.IsFalse(Displayed(root.Q("editor")));
        }
    }
}
