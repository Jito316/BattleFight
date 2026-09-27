using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight
{
    /// <summary>ステージ選択画面(IMGUI)。ArenaDirector がステージ選択中のときだけ出る。</summary>
    public class StageSelectUI : MonoBehaviour
    {
        const float RefHeight = 1080f;
        const float RowHeight = 118f;

        [SerializeField] ArenaDirector director;
        [SerializeField] StyleRankConfig rankConfig;
        [SerializeField] Font font;

        int highlighted;
        GUIStyle label;
        GUIStyle wrap;

        bool Visible => director != null && director.State == ArenaDirector.GameState.StageSelect && !LoadoutEditorUI.IsOpen;

        void Update()
        {
            if (!Visible || director.Stages == null || director.Stages.Count == 0) return;

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
            if (confirm) director.StartStage(highlighted);
        }

        void OnGUI()
        {
            if (!Visible || director.Stages == null) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20 };
                wrap = new GUIStyle(label) { wordWrap = true, fontSize = 18 };
                if (font != null)
                {
                    label.font = font;
                    wrap.font = font;
                }
            }

            float scale = Screen.height / RefHeight;
            float width = Screen.width / scale;
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Box(new Rect(0, 0, width, RefHeight), new Color(0f, 0f, 0f, 0.5f));
            float panelWidth = Mathf.Min(1100f, width - 80f);
            var panel = new Rect((width - panelWidth) * 0.5f, 60f, panelWidth, RefHeight - 120f);
            Box(panel, new Color(0.08f, 0.09f, 0.12f, 0.95f));

            Text(new Rect(panel.x, panel.y + 24, panel.width, 70), "<b>BattleFight</b>", 56, Color.white, TextAnchor.UpperCenter);
            Text(new Rect(panel.x, panel.y + 96, panel.width, 30), "ステージを選んでください", 22, new Color(1f, 1f, 1f, 0.7f),
                TextAnchor.UpperCenter);

            float y = panel.y + 150f;
            for (int i = 0; i < director.Stages.Count; i++)
            {
                var stage = director.Stages[i];
                if (stage == null) continue;
                var row = new Rect(panel.x + 40, y, panel.width - 80, RowHeight - 12);
                bool hover = row.Contains(Event.current.mousePosition);
                if (hover) highlighted = i;
                bool selected = i == highlighted;

                Box(row, selected ? new Color(0.18f, 0.25f, 0.36f) : new Color(0.13f, 0.14f, 0.18f));
                Box(new Rect(row.x, row.y, 8, row.height), KindColor(stage.kind));
                if (selected) Outline(row, new Color(0.4f, 0.75f, 1f), 3f);

                Text(new Rect(row.x + 24, row.y + 10, row.width - 360, 36), $"<b>{stage.displayName}</b>", 28, Color.white);
                GUI.Label(new Rect(row.x + 24, row.y + 50, row.width - 380, row.height - 54), stage.description, wrap);
                Text(new Rect(row.xMax - 340, row.y + 14, 320, 30), RecordText(stage), 18, new Color(1f, 0.9f, 0.5f),
                    TextAnchor.UpperRight);

                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) director.StartStage(i);
                y += RowHeight;
            }

            Text(new Rect(panel.x, panel.yMax - 44, panel.width, 30),
                "クリック / Enter(Aボタン)で開始   W・S / 十字キーで選択   [P] スキル編成", 18, new Color(1f, 1f, 1f, 0.6f),
                TextAnchor.UpperCenter);

            GUI.matrix = previous;
        }

        string RecordText(StageData stage)
        {
            switch (stage.kind)
            {
                case StageKind.Training:
                    return "練習用";
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
            _ => new Color(0.4f, 0.7f, 1f),
        };

        void Text(Rect rect, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            label.fontSize = size;
            label.alignment = anchor;
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.Label(rect, text, label);
            GUI.color = previousColor;
        }

        static void Box(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void Outline(Rect rect, Color color, float thickness)
        {
            Box(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Box(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Box(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Box(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
