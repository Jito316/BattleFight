using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace BattleFight
{
    /// <summary>
    /// ステージ選択画面(UI Toolkit)。ArenaDirector がステージ選択中のときだけ出る。レイアウトは StageSelect.uxml。
    /// 上にセーブスロット(SaveProfiles)を並べ、切り替え・消すができる。消す前には確認を出す。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StageSelectUI : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("director")] ArenaDirector mDirector;
        [SerializeField, FormerlySerializedAs("rankConfig")] StyleRankConfig mRankConfig;

        VisualElement mRoot;
        VisualElement mWindow;
        VisualElement mList;
        VisualElement mProfileList;
        Label mFooter;
        readonly List<(VisualElement row, Label record, Button reset)> mRows = new List<(VisualElement, Label, Button)>();
        readonly List<(VisualElement slot, Label info)> mSlots = new List<(VisualElement, Label)>();
        int mHighlighted;
        string mNotice;
        float mNoticeTime = -99f;

        // 消す前の確認
        VisualElement mConfirm;
        Label mConfirmTitle;
        Label mConfirmText;
        Button mConfirmOk;
        Button mConfirmCancel;
        Action mConfirmAction;
        bool mConfirmOnOk;

        bool mVisible => mDirector != null && mDirector.State == ArenaDirector.GameState.StageSelect && !LoadoutEditorUI.IsOpen;
        /// <summary>操作を受け付けるか(設定画面を開いている間は受け付けない)</summary>
        bool mInteractive => mVisible && !SettingsUI.IsOpen;

        public int Highlighted => mHighlighted;
        /// <summary>消す前の確認を出しているか</summary>
        public bool IsConfirming => mConfirmAction != null;

        void OnEnable()
        {
            mRoot = GetComponent<UIDocument>().rootVisualElement;
            mWindow = mRoot.Q("stage-select");
            mList = mRoot.Q("stage-list");
            mProfileList = mRoot.Q("profile-list");
            mFooter = mRoot.Q<Label>("footer");
            mConfirm = mRoot.Q("confirm");
            mConfirmTitle = mRoot.Q<Label>("confirm-title");
            mConfirmText = mRoot.Q<Label>("confirm-text");
            mConfirmOk = mRoot.Q<Button>("confirm-ok");
            mConfirmCancel = mRoot.Q<Button>("confirm-cancel");
            mConfirmOk.clicked += () => CloseConfirm(true);
            mConfirmCancel.clicked += () => CloseConfirm(false);
            mRoot.Q<Button>("profile-delete").clicked += AskDeleteSlot;
            mRoot.Q<Button>("open-settings").clicked += () => { if (mInteractive && SettingsUI.Instance != null) SettingsUI.Instance.Open(); };
            BuildSlots();
            BuildRows();
        }

        void BuildSlots()
        {
            mProfileList.Clear();
            mSlots.Clear();
            for (int aIndex = 1; aIndex <= SaveProfiles.SlotCount; aIndex++)
            {
                var aSlot = Classed(new VisualElement(), "profile-slot");
                aSlot.Add(Classed(new Label(GameText.Format("select.slot", aIndex)), "profile-slot__title"));
                var aInfo = Classed(new Label(), "profile-slot__info");
                aSlot.Add(aInfo);
                int aNumber = aIndex;
                aSlot.RegisterCallback<ClickEvent>(_ => { if (mInteractive && !IsConfirming) SelectSlot(aNumber); });
                mProfileList.Add(aSlot);
                mSlots.Add((aSlot, aInfo));
            }
        }

        void BuildRows()
        {
            mList.Clear();
            mRows.Clear();
            if (mDirector == null || mDirector.Stages == null) return;
            for (int aStageIndex = 0; aStageIndex < mDirector.Stages.Count; aStageIndex++)
            {
                var aStage = mDirector.Stages[aStageIndex];
                var aRow = Classed(new VisualElement(), "stage-row");
                var aStripe = Classed(new VisualElement(), "stage-row__stripe");
                aStripe.style.backgroundColor = aStage != null ? KindColor(aStage.Kind) : Color.gray;
                aRow.Add(aStripe);
                var aBody = Classed(new VisualElement(), "stage-row__body");
                var aHead = Classed(new VisualElement(), "stage-row__head");
                aHead.Add(Classed(new Label(aStage != null ? aStage.DisplayName : "-"), "stage-row__title"));
                var aSide = Classed(new VisualElement(), "stage-row__side");
                var aRecord = Classed(new Label(), "stage-row__record");
                aSide.Add(aRecord);
                Button aReset = null;
                if (aStage != null && aStage.Kind == StageKind.Exploration)
                {
                    aReset = Classed(new Button { text = GameText.Get("select.start_over") }, "stage-row__reset");
                    // 行のクリック(ステージ開始)まで届かないようにする
                    aReset.RegisterCallback<ClickEvent>(iEvent =>
                    {
                        iEvent.StopPropagation();
                        if (mInteractive && !IsConfirming) AskStartOver(aStage);
                    });
                    aSide.Add(aReset);
                }
                aHead.Add(aSide);
                aBody.Add(aHead);
                aBody.Add(Classed(new Label(aStage != null ? aStage.Description : ""), "stage-row__desc"));
                aRow.Add(aBody);

                int aIndex = aStageIndex;
                // マウスを動かしたときだけ選択を変える(止まったマウスがキー操作の選択を上書きしないように)
                aRow.RegisterCallback<PointerMoveEvent>(_ => { if (!IsConfirming) mHighlighted = aIndex; });
                aRow.RegisterCallback<ClickEvent>(_ => { if (mInteractive && !IsConfirming) mDirector.StartStage(aIndex); });
                mList.Add(aRow);
                mRows.Add((aRow, aRecord, aReset));
            }
        }

        void Update()
        {
            bool aVisible = mVisible;
            mWindow.style.display = aVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!aVisible)
            {
                if (IsConfirming) CloseConfirm(false);
                return;
            }
            if (mDirector.Stages == null || mDirector.Stages.Count == 0) return;
            if (mRows.Count != mDirector.Stages.Count) BuildRows();

            var aKeyboard = Keyboard.current;
            var aGamepad = Gamepad.current;
            if (SettingsUI.IsOpen)
            {
                // 設定画面を開いている間は、ステージ選択の操作をしない
            }
            else if (IsConfirming)
            {
                UpdateConfirm(aKeyboard, aGamepad);
            }
            else
            {
                UpdateSelect(aKeyboard, aGamepad);
            }
            // 開始したステージに切り替わったら、もう書き換えない
            if (mDirector.State != ArenaDirector.GameState.StageSelect) return;
            RefreshTexts();
        }

        void UpdateSelect(Keyboard iKeyboard, Gamepad iGamepad)
        {
            int aCount = mDirector.Stages.Count;
            bool aUp = (iKeyboard != null && (iKeyboard.wKey.wasPressedThisFrame || iKeyboard.upArrowKey.wasPressedThisFrame))
                      || (iGamepad != null && iGamepad.dpad.up.wasPressedThisFrame);
            bool aDown = (iKeyboard != null && (iKeyboard.sKey.wasPressedThisFrame || iKeyboard.downArrowKey.wasPressedThisFrame))
                        || (iGamepad != null && iGamepad.dpad.down.wasPressedThisFrame);
            bool aConfirm = (iKeyboard != null && (iKeyboard.enterKey.wasPressedThisFrame || iKeyboard.numpadEnterKey.wasPressedThisFrame))
                           || (iGamepad != null && iGamepad.buttonSouth.wasPressedThisFrame);
            bool aPrevSlot = (iKeyboard != null && iKeyboard.qKey.wasPressedThisFrame) || (iGamepad != null && iGamepad.leftShoulder.wasPressedThisFrame);
            bool aNextSlot = (iKeyboard != null && iKeyboard.eKey.wasPressedThisFrame) || (iGamepad != null && iGamepad.rightShoulder.wasPressedThisFrame);
            bool aDeleteSlot = (iKeyboard != null && iKeyboard.deleteKey.wasPressedThisFrame) || (iGamepad != null && iGamepad.buttonNorth.wasPressedThisFrame);

            if (aUp) mHighlighted = (mHighlighted + aCount - 1) % aCount;
            if (aDown) mHighlighted = (mHighlighted + 1) % aCount;
            if (aPrevSlot) SelectSlot((SaveProfiles.Current + SaveProfiles.SlotCount - 2) % SaveProfiles.SlotCount + 1);
            if (aNextSlot) SelectSlot(SaveProfiles.Current % SaveProfiles.SlotCount + 1);

            // 探索を選んでいるとき: Backspace で、確認してからはじめからにする
            var aStage = mDirector.Stages[mHighlighted];
            bool aExploration = aStage != null && aStage.Kind == StageKind.Exploration;
            if (aExploration && iKeyboard != null && iKeyboard.backspaceKey.wasPressedThisFrame && ExplorationSave.HasStarted)
            {
                AskStartOver(aStage);
                return;
            }
            if (aDeleteSlot)
            {
                AskDeleteSlot();
                return;
            }
            // □ボタン: 設定画面(キーボードは O。どの画面からでも開ける)
            if (iGamepad != null && iGamepad.buttonWest.wasPressedThisFrame && SettingsUI.Instance != null)
            {
                SettingsUI.Instance.Open();
                return;
            }
            if (aConfirm) mDirector.StartStage(mHighlighted);
        }

        void RefreshTexts()
        {
            for (int aRowIndex = 0; aRowIndex < mRows.Count; aRowIndex++)
            {
                var (aRow, aRecord, aReset) = mRows[aRowIndex];
                aRow.EnableInClassList("stage-row--selected", aRowIndex == mHighlighted);
                string aText = mDirector.Stages[aRowIndex] != null ? RecordText(mDirector.Stages[aRowIndex]) : "";
                if (aRecord.text != aText) aRecord.text = aText;
                if (aReset != null) aReset.style.display = ExplorationSave.HasStarted ? DisplayStyle.Flex : DisplayStyle.None;
            }
            for (int aSlotIndex = 0; aSlotIndex < mSlots.Count; aSlotIndex++)
            {
                var (aSlot, aInfo) = mSlots[aSlotIndex];
                aSlot.EnableInClassList("profile-slot--current", aSlotIndex + 1 == SaveProfiles.Current);
                string aText = SlotText(aSlotIndex + 1);
                if (aInfo.text != aText) aInfo.text = aText;
            }

            var aStage = mDirector.Stages[mHighlighted];
            bool aExploration = aStage != null && aStage.Kind == StageKind.Exploration;
            bool aNotice = Time.unscaledTime - mNoticeTime < 2.5f;
            mFooter.EnableInClassList("select__footer--notice", aNotice);
            mFooter.text = aNotice
                ? mNotice
                : GameText.Get("select.footer") + (aExploration && ExplorationSave.HasStarted ? GameText.Get("select.footer.start_over") : "");
        }

        static string SlotText(int iSlot)
        {
            if (!SaveProfiles.IsUsed(iSlot)) return GameText.Get("select.slot.empty");
            var aLast = SaveProfiles.LastPlayed(iSlot);
            return aLast.HasValue ? GameText.Format("select.slot.last_played", aLast.Value) : GameText.Get("select.slot.has_data");
        }

        void SelectSlot(int iSlot)
        {
            if (iSlot == SaveProfiles.Current) return;
            SaveProfiles.Select(iSlot);
            ShowNotice(GameText.Format("select.slot.switched", iSlot));
        }

        void ShowNotice(string iText)
        {
            mNotice = iText;
            mNoticeTime = Time.unscaledTime;
        }

        // ---------- 消す前の確認 ----------

        void AskStartOver(StageData iStage)
        {
            OpenConfirm(GameText.Get("select.start_over.title"),
                GameText.Format("select.start_over.text", SaveProfiles.Current),
                () =>
                {
                    ExplorationSave.StartOver(iStage.name);
                    ShowNotice(GameText.Get("select.start_over.done"));
                });
        }

        void AskDeleteSlot()
        {
            if (!mInteractive || IsConfirming) return;
            int aSlot = SaveProfiles.Current;
            if (!SaveProfiles.IsUsed(aSlot))
            {
                ShowNotice(GameText.Format("select.slot.is_empty", aSlot));
                return;
            }
            OpenConfirm(GameText.Format("select.slot.delete.title", aSlot),
                GameText.Get("select.slot.delete.text"),
                () =>
                {
                    SaveProfiles.Delete(aSlot);
                    ShowNotice(GameText.Format("select.slot.deleted", aSlot));
                });
        }

        void OpenConfirm(string iTitle, string iText, Action iOnOk)
        {
            mConfirmTitle.text = iTitle;
            mConfirmText.text = iText;
            mConfirmAction = iOnOk;
            // 間違って消さないよう、最初は「やめる」を選んでおく
            mConfirmOnOk = false;
            mConfirm.AddToClassList("confirm--open");
            RefreshConfirmButtons();
        }

        void CloseConfirm(bool iOk)
        {
            var aAction = mConfirmAction;
            mConfirmAction = null;
            mConfirm.RemoveFromClassList("confirm--open");
            if (iOk) aAction?.Invoke();
        }

        void UpdateConfirm(Keyboard iKeyboard, Gamepad iGamepad)
        {
            bool aLeft = (iKeyboard != null && (iKeyboard.aKey.wasPressedThisFrame || iKeyboard.leftArrowKey.wasPressedThisFrame))
                        || (iGamepad != null && iGamepad.dpad.left.wasPressedThisFrame);
            bool aRight = (iKeyboard != null && (iKeyboard.dKey.wasPressedThisFrame || iKeyboard.rightArrowKey.wasPressedThisFrame))
                         || (iGamepad != null && iGamepad.dpad.right.wasPressedThisFrame);
            bool aDecide = (iKeyboard != null && (iKeyboard.enterKey.wasPressedThisFrame || iKeyboard.numpadEnterKey.wasPressedThisFrame
                                                  || iKeyboard.spaceKey.wasPressedThisFrame))
                          || (iGamepad != null && iGamepad.buttonSouth.wasPressedThisFrame);
            bool aCancel = (iKeyboard != null && (iKeyboard.escapeKey.wasPressedThisFrame || iKeyboard.backspaceKey.wasPressedThisFrame))
                          || (iGamepad != null && iGamepad.buttonEast.wasPressedThisFrame);

            if (aLeft) mConfirmOnOk = false;
            if (aRight) mConfirmOnOk = true;
            RefreshConfirmButtons();
            if (aCancel) CloseConfirm(false);
            else if (aDecide) CloseConfirm(mConfirmOnOk);
        }

        void RefreshConfirmButtons()
        {
            mConfirmCancel.EnableInClassList("confirm__button--selected", !mConfirmOnOk);
            mConfirmOk.EnableInClassList("confirm__button--selected", mConfirmOnOk);
        }

        string RecordText(StageData iStage)
        {
            switch (iStage.Kind)
            {
                case StageKind.Training:
                    return GameText.Get("select.record.training");
                case StageKind.Exploration:
                    if (!ExplorationSave.HasStarted) return GameText.Get("select.start_over");
                    string aCleared = StageRecords.IsCleared(iStage.name) ? GameText.Get("select.record.cleared_mark") : "";
                    return GameText.Format("select.record.exploration", aCleared, ExplorationSave.UnlockedCount,
                        Mathf.Max(ExplorationSave.TotalSkills, ExplorationSave.UnlockedCount));
                case StageKind.Endless:
                    int aWave = StageRecords.BestWave(iStage.name);
                    return aWave > 0 ? GameText.Format("select.record.endless", aWave) : GameText.Get("select.record.none");
                default:
                    if (!StageRecords.IsCleared(iStage.name)) return GameText.Get("select.record.not_cleared");
                    int aRank = StageRecords.BestRank(iStage.name);
                    string aRankName = mRankConfig != null && aRank >= 0 ? mRankConfig.RankNames[Mathf.Min(aRank, mRankConfig.RankNames.Length - 1)] : "-";
                    return GameText.Format("select.record.cleared", StageRecords.BestTime(iStage.name), aRankName);
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
