using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BattleFight
{
    /// <summary>ステージ選択画面(UI Toolkit)。ArenaDirector がステージ選択中のときだけ出る。レイアウトは StageSelect.uxml。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class StageSelectUI : MonoBehaviour
    {
        [SerializeField] ArenaDirector director;
        [SerializeField] StyleRankConfig rankConfig;

        VisualElement root, window, list;
        Label footer;
        readonly List<(VisualElement row, Label record)> rows = new List<(VisualElement, Label)>();
        int highlighted;
        float resetNoticeTime = -99f;

        bool Visible => director != null && director.State == ArenaDirector.GameState.StageSelect && !LoadoutEditorUI.IsOpen;

        public int Highlighted => highlighted;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            window = root.Q("stage-select");
            list = root.Q("stage-list");
            footer = root.Q<Label>("footer");
            BuildRows();
        }

        void BuildRows()
        {
            list.Clear();
            rows.Clear();
            if (director == null || director.Stages == null) return;
            for (int i = 0; i < director.Stages.Count; i++)
            {
                var stage = director.Stages[i];
                var row = Classed(new VisualElement(), "stage-row");
                var stripe = Classed(new VisualElement(), "stage-row__stripe");
                stripe.style.backgroundColor = stage != null ? KindColor(stage.kind) : Color.gray;
                row.Add(stripe);
                var body = Classed(new VisualElement(), "stage-row__body");
                var head = Classed(new VisualElement(), "stage-row__head");
                head.Add(Classed(new Label(stage != null ? stage.displayName : "-"), "stage-row__title"));
                var record = Classed(new Label(), "stage-row__record");
                head.Add(record);
                body.Add(head);
                body.Add(Classed(new Label(stage != null ? stage.description : ""), "stage-row__desc"));
                row.Add(body);

                int index = i;
                // マウスを動かしたときだけ選択を変える(止まったマウスがキー操作の選択を上書きしないように)
                row.RegisterCallback<PointerMoveEvent>(_ => highlighted = index);
                row.RegisterCallback<ClickEvent>(_ => { if (Visible) director.StartStage(index); });
                list.Add(row);
                rows.Add((row, record));
            }
        }

        void Update()
        {
            bool visible = Visible;
            window.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible || director.Stages == null || director.Stages.Count == 0) return;
            if (rows.Count != director.Stages.Count) BuildRows();

            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            int count = director.Stages.Count;
            bool up = (keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
                      || (gamepad != null && gamepad.dpad.up.wasPressedThisFrame);
            bool down = (keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
                        || (gamepad != null && gamepad.dpad.down.wasPressedThisFrame);
            bool confirm = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                           || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

            if (up) highlighted = (highlighted + count - 1) % count;
            if (down) highlighted = (highlighted + 1) % count;

            // 探索を選んでいるとき: Backspace で記録を消して、はじめからにする
            var stage = director.Stages[highlighted];
            bool exploration = stage != null && stage.kind == StageKind.Exploration;
            if (exploration && keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
            {
                ExplorationSave.Reset();
                PlayerPrefs.DeleteKey(LoadoutStorage.ExplorationPrefsKey);
                StageRecords.Clear(stage.name);
                resetNoticeTime = Time.unscaledTime;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var (row, record) = rows[i];
                row.EnableInClassList("stage-row--selected", i == highlighted);
                string text = director.Stages[i] != null ? RecordText(director.Stages[i]) : "";
                if (record.text != text) record.text = text;
            }

            bool notice = Time.unscaledTime - resetNoticeTime < 2.5f;
            footer.EnableInClassList("select__footer--notice", notice);
            footer.text = notice
                ? "探索の記録を消しました。はじめから遊べます"
                : "クリック / Enter(Aボタン)で開始   W・S / 十字キーで選択   [P] スキル編成" + (exploration ? "   [Backspace] 探索をはじめから" : "");

            if (confirm) director.StartStage(highlighted);
        }

        string RecordText(StageData stage)
        {
            switch (stage.kind)
            {
                case StageKind.Training:
                    return "練習用";
                case StageKind.Exploration:
                    if (!ExplorationSave.HasStarted) return "はじめから";
                    string cleared = StageRecords.IsCleared(stage.name) ? "クリア済み  " : "";
                    return $"{cleared}スキル {ExplorationSave.UnlockedCount} / {Mathf.Max(ExplorationSave.TotalSkills, ExplorationSave.UnlockedCount)}";
                case StageKind.Endless:
                    int wave = StageRecords.BestWave(stage.name);
                    return wave > 0 ? $"最高 WAVE {wave}" : "記録なし";
                default:
                    if (!StageRecords.IsCleared(stage.name)) return "未クリア";
                    int rank = StageRecords.BestRank(stage.name);
                    string rankName = rankConfig != null && rank >= 0 ? rankConfig.rankNames[Mathf.Min(rank, rankConfig.rankNames.Length - 1)] : "-";
                    return $"クリア済み  {StageRecords.BestTime(stage.name):0.0}秒 / {rankName}";
            }
        }

        static Color KindColor(StageKind kind) => kind switch
        {
            StageKind.Training => new Color(0.4f, 0.9f, 0.5f),
            StageKind.Endless => new Color(1f, 0.4f, 0.4f),
            StageKind.Exploration => new Color(0.8f, 0.55f, 1f),
            _ => new Color(0.4f, 0.7f, 1f),
        };

        static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            if (element is Label) element.pickingMode = PickingMode.Ignore;
            return element;
        }
    }
}
