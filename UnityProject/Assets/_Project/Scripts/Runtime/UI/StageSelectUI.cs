using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace BattleFight
{
    /// <summary>ステージ選択画面(UI Toolkit)。ArenaDirector がステージ選択中のときだけ出る。レイアウトは StageSelect.uxml。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class StageSelectUI : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("director")] ArenaDirector mDirector;
        [SerializeField, FormerlySerializedAs("rankConfig")] StyleRankConfig mRankConfig;

        VisualElement mRoot;
        VisualElement mWindow;
        VisualElement mList;
        Label mFooter;
        readonly List<(VisualElement row, Label record)> mRows = new List<(VisualElement, Label)>();
        int mHighlighted;
        float mResetNoticeTime = -99f;

        bool mVisible => mDirector != null && mDirector.State == ArenaDirector.GameState.StageSelect && !LoadoutEditorUI.IsOpen;

        public int Highlighted => mHighlighted;

        void OnEnable()
        {
            mRoot = GetComponent<UIDocument>().rootVisualElement;
            mWindow = mRoot.Q("stage-select");
            mList = mRoot.Q("stage-list");
            mFooter = mRoot.Q<Label>("footer");
            BuildRows();
        }

        void BuildRows()
        {
            mList.Clear();
            mRows.Clear();
            if (mDirector == null || mDirector.Stages == null) return;
            for (int aI = 0; aI < mDirector.Stages.Count; aI++)
            {
                var aStage = mDirector.Stages[aI];
                var aRow = Classed(new VisualElement(), "stage-row");
                var aStripe = Classed(new VisualElement(), "stage-row__stripe");
                aStripe.style.backgroundColor = aStage != null ? KindColor(aStage.Kind) : Color.gray;
                aRow.Add(aStripe);
                var aBody = Classed(new VisualElement(), "stage-row__body");
                var aHead = Classed(new VisualElement(), "stage-row__head");
                aHead.Add(Classed(new Label(aStage != null ? aStage.DisplayName : "-"), "stage-row__title"));
                var aRecord = Classed(new Label(), "stage-row__record");
                aHead.Add(aRecord);
                aBody.Add(aHead);
                aBody.Add(Classed(new Label(aStage != null ? aStage.Description : ""), "stage-row__desc"));
                aRow.Add(aBody);

                int aIndex = aI;
                // マウスを動かしたときだけ選択を変える(止まったマウスがキー操作の選択を上書きしないように)
                aRow.RegisterCallback<PointerMoveEvent>(_ => mHighlighted = aIndex);
                aRow.RegisterCallback<ClickEvent>(_ => { if (mVisible) mDirector.StartStage(aIndex); });
                mList.Add(aRow);
                mRows.Add((aRow, aRecord));
            }
        }

        void Update()
        {
            bool aVisible = mVisible;
            mWindow.style.display = aVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!aVisible || mDirector.Stages == null || mDirector.Stages.Count == 0) return;
            if (mRows.Count != mDirector.Stages.Count) BuildRows();

            var aKeyboard = Keyboard.current;
            var aGamepad = Gamepad.current;
            int aCount = mDirector.Stages.Count;
            bool aUp = (aKeyboard != null && (aKeyboard.wKey.wasPressedThisFrame || aKeyboard.upArrowKey.wasPressedThisFrame))
                      || (aGamepad != null && aGamepad.dpad.up.wasPressedThisFrame);
            bool aDown = (aKeyboard != null && (aKeyboard.sKey.wasPressedThisFrame || aKeyboard.downArrowKey.wasPressedThisFrame))
                        || (aGamepad != null && aGamepad.dpad.down.wasPressedThisFrame);
            bool aConfirm = (aKeyboard != null && (aKeyboard.enterKey.wasPressedThisFrame || aKeyboard.numpadEnterKey.wasPressedThisFrame))
                           || (aGamepad != null && aGamepad.buttonSouth.wasPressedThisFrame);

            if (aUp) mHighlighted = (mHighlighted + aCount - 1) % aCount;
            if (aDown) mHighlighted = (mHighlighted + 1) % aCount;

            // 探索を選んでいるとき: Backspace で記録を消して、はじめからにする
            var aStage = mDirector.Stages[mHighlighted];
            bool aExploration = aStage != null && aStage.Kind == StageKind.Exploration;
            if (aExploration && aKeyboard != null && aKeyboard.backspaceKey.wasPressedThisFrame)
            {
                ExplorationSave.Reset();
                PlayerPrefs.DeleteKey(LoadoutStorage.ExplorationPrefsKey);
                StageRecords.Clear(aStage.name);
                mResetNoticeTime = Time.unscaledTime;
            }

            for (int aI = 0; aI < mRows.Count; aI++)
            {
                var (aRow, aRecord) = mRows[aI];
                aRow.EnableInClassList("stage-row--selected", aI == mHighlighted);
                string aText = mDirector.Stages[aI] != null ? RecordText(mDirector.Stages[aI]) : "";
                if (aRecord.text != aText) aRecord.text = aText;
            }

            bool aNotice = Time.unscaledTime - mResetNoticeTime < 2.5f;
            mFooter.EnableInClassList("select__footer--notice", aNotice);
            mFooter.text = aNotice
                ? "探索の記録を消しました。はじめから遊べます"
                : "クリック / Enter(Aボタン)で開始   W・S / 十字キーで選択   [P] スキル編成" + (aExploration ? "   [Backspace] 探索をはじめから" : "");

            if (aConfirm) mDirector.StartStage(mHighlighted);
        }

        string RecordText(StageData iStage)
        {
            switch (iStage.Kind)
            {
                case StageKind.Training:
                    return "練習用";
                case StageKind.Exploration:
                    if (!ExplorationSave.HasStarted) return "はじめから";
                    string aCleared = StageRecords.IsCleared(iStage.name) ? "クリア済み  " : "";
                    return $"{aCleared}スキル {ExplorationSave.UnlockedCount} / {Mathf.Max(ExplorationSave.TotalSkills, ExplorationSave.UnlockedCount)}";
                case StageKind.Endless:
                    int aWave = StageRecords.BestWave(iStage.name);
                    return aWave > 0 ? $"最高 WAVE {aWave}" : "記録なし";
                default:
                    if (!StageRecords.IsCleared(iStage.name)) return "未クリア";
                    int aRank = StageRecords.BestRank(iStage.name);
                    string aRankName = mRankConfig != null && aRank >= 0 ? mRankConfig.RankNames[Mathf.Min(aRank, mRankConfig.RankNames.Length - 1)] : "-";
                    return $"クリア済み  {StageRecords.BestTime(iStage.name):0.0}秒 / {aRankName}";
            }
        }

        static Color KindColor(StageKind iKind) => iKind switch
        {
            StageKind.Training => new Color(0.4f, 0.9f, 0.5f),
            StageKind.Endless => new Color(1f, 0.4f, 0.4f),
            StageKind.Exploration => new Color(0.8f, 0.55f, 1f),
            _ => new Color(0.4f, 0.7f, 1f),
        };

        static T Classed<T>(T iElement, string iClassName) where T : VisualElement
        {
            iElement.AddToClassList(iClassName);
            if (iElement is Label) iElement.pickingMode = PickingMode.Ignore;
            return iElement;
        }
    }
}
