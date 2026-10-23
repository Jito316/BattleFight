using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BattleFight
{
    /// <summary>
    /// 設定画面(UI Toolkit)。音量・画面の揺れ・画面の光・ズーム・振動・カメラの感度を変える(GameSettings)。
    /// O キー(ステージ選択では「設定」ボタン・□ボタンでも)で開き、開いている間はゲームを止める。
    /// M キーは、画面を開いていなくてもミュートを切り替える。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsUI : MonoBehaviour
    {
        /// <summary>1 回の操作で変える量</summary>
        const float mStep = 0.1f;

        /// <summary>設定の 1 行: 表示名・今の値(0 〜 1 の棒の長さ)・値の文字・左右で変える処理</summary>
        class Row
        {
            public string Label;
            public Func<float> Fill;
            public Func<string> Value;
            public Action<int> Change;
            public VisualElement Element;
            public VisualElement FillBar;
            public Label ValueLabel;
        }

        VisualElement mRoot;
        VisualElement mWindow;
        Label mHint;
        readonly List<Row> mRows = new List<Row>();
        int mSelected;
        bool mPausedByMe;

        public static SettingsUI Instance { get; private set; }
        public static bool IsOpen { get; private set; }
        /// <summary>今選んでいる行(テスト用)</summary>
        public int Selected => mSelected;

        void OnEnable()
        {
            Instance = this;
            mRoot = GetComponent<UIDocument>().rootVisualElement;
            mWindow = mRoot.Q("settings");
            mHint = mRoot.Q<Label>("settings-hint");
            mRoot.Q<Button>("settings-reset").clicked += () =>
            {
                GameSettings.ResetToDefaults();
                Refresh();
            };
            mRoot.Q<Button>("settings-close").clicked += Close;
            BuildRows();
            mWindow.RemoveFromClassList("settings--open");
            IsOpen = false;
        }

        void OnDisable()
        {
            if (IsOpen) Close();
            if (Instance == this) Instance = null;
        }

        static string Percent(float iValue) => $"{Mathf.RoundToInt(iValue * 100f)}%";

        void BuildRows()
        {
            mRows.Clear();
            mRows.Add(new Row
            {
                Label = GameText.Get("settings.volume"),
                Fill = () => GameSettings.Muted ? 0f : GameSettings.Volume,
                Value = () => GameSettings.Muted ? GameText.Get("settings.muted") : Percent(GameSettings.Volume),
                Change = iDirection =>
                {
                    GameSettings.Muted = false;
                    GameSettings.Volume += iDirection * mStep;
                },
            });
            mRows.Add(Strength(GameText.Get("settings.shake"), () => GameSettings.Shake, iValue => GameSettings.Shake = iValue));
            mRows.Add(Strength(GameText.Get("settings.flash"), () => GameSettings.Flash, iValue => GameSettings.Flash = iValue));
            mRows.Add(Strength(GameText.Get("settings.zoom"), () => GameSettings.Zoom, iValue => GameSettings.Zoom = iValue));
            mRows.Add(new Row
            {
                Label = GameText.Get("settings.vibration"),
                Fill = () => GameSettings.Vibration ? 1f : 0f,
                Value = () => GameText.Get(GameSettings.Vibration ? "settings.on" : "settings.off"),
                Change = _ => GameSettings.Vibration = !GameSettings.Vibration,
            });
            mRows.Add(new Row
            {
                Label = GameText.Get("settings.camera"),
                Fill = () => Mathf.InverseLerp(GameSettings.MinCameraSensitivity, GameSettings.MaxCameraSensitivity, GameSettings.CameraSensitivity),
                Value = () => $"×{GameSettings.CameraSensitivity:0.0}",
                Change = iDirection => GameSettings.CameraSensitivity += iDirection * mStep,
            });

            var aContainer = mRoot.Q("settings-rows");
            aContainer.Clear();
            for (int aIndex = 0; aIndex < mRows.Count; aIndex++)
            {
                var aRow = mRows[aIndex];
                aRow.Element = Classed(new VisualElement(), "setting-row");
                aRow.Element.Add(Classed(new Label(aRow.Label), "setting-row__label"));
                var aLeft = Classed(new Button { text = "<" }, "setting-row__arrow");
                var aBar = Classed(new VisualElement(), "setting-row__bar");
                aRow.FillBar = Classed(new VisualElement(), "setting-row__fill");
                aBar.Add(aRow.FillBar);
                var aRight = Classed(new Button { text = ">" }, "setting-row__arrow");
                aRow.ValueLabel = Classed(new Label(), "setting-row__value");
                aRow.Element.Add(aLeft);
                aRow.Element.Add(aBar);
                aRow.Element.Add(aRight);
                aRow.Element.Add(aRow.ValueLabel);

                int aRowIndex = aIndex;
                aLeft.clicked += () => ChangeRow(aRowIndex, -1);
                aRight.clicked += () => ChangeRow(aRowIndex, 1);
                aRow.Element.RegisterCallback<PointerMoveEvent>(_ => mSelected = aRowIndex);
                aContainer.Add(aRow.Element);
            }
            Refresh();
        }

        static Row Strength(string iLabel, Func<float> iGet, Action<float> iSet) => new Row
        {
            Label = iLabel,
            Fill = iGet,
            Value = () => iGet() <= 0f ? GameText.Get("settings.none") : Percent(iGet()),
            Change = iDirection => iSet(iGet() + iDirection * mStep),
        };

        void ChangeRow(int iRow, int iDirection)
        {
            mSelected = iRow;
            mRows[iRow].Change(iDirection);
            SoundPlayer.Play(Sfx.Swap, 0.4f, 1.1f + 0.05f * iDirection);
            Refresh();
        }

        public void Open()
        {
            if (IsOpen || LoadoutEditorUI.IsOpen) return;
            var aDirector = ArenaDirector.Instance;
            // リザルト画面では Start が「もう一度」なので開かない
            if (aDirector != null && (aDirector.State == ArenaDirector.GameState.Cleared || aDirector.State == ArenaDirector.GameState.GameOver)) return;
            IsOpen = true;
            mSelected = 0;
            mWindow.AddToClassList("settings--open");
            mPausedByMe = !GamePause.IsPaused;
            if (mPausedByMe) GamePause.Set(true);
            ThirdPersonCamera.SetCursorLocked(false);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            mWindow.RemoveFromClassList("settings--open");
            if (mPausedByMe) GamePause.Set(false);
            mPausedByMe = false;
        }

        void Update()
        {
            var aKeyboard = Keyboard.current;
            var aGamepad = Gamepad.current;
            // M: いつでもミュートを切り替える
            if (aKeyboard != null && aKeyboard.mKey.wasPressedThisFrame)
            {
                GameSettings.Muted = !GameSettings.Muted;
                Refresh();
            }

            bool aToggle = aKeyboard != null && aKeyboard.oKey.wasPressedThisFrame;
            if (!IsOpen)
            {
                if (aToggle) Open();
                return;
            }

            bool aClose = aToggle || (aKeyboard != null && aKeyboard.escapeKey.wasPressedThisFrame) || (aGamepad != null && aGamepad.buttonEast.wasPressedThisFrame);
            if (aClose)
            {
                Close();
                return;
            }
            bool aUp = (aKeyboard != null && (aKeyboard.wKey.wasPressedThisFrame || aKeyboard.upArrowKey.wasPressedThisFrame))
                      || (aGamepad != null && aGamepad.dpad.up.wasPressedThisFrame);
            bool aDown = (aKeyboard != null && (aKeyboard.sKey.wasPressedThisFrame || aKeyboard.downArrowKey.wasPressedThisFrame))
                        || (aGamepad != null && aGamepad.dpad.down.wasPressedThisFrame);
            bool aLeft = (aKeyboard != null && (aKeyboard.aKey.wasPressedThisFrame || aKeyboard.leftArrowKey.wasPressedThisFrame))
                        || (aGamepad != null && aGamepad.dpad.left.wasPressedThisFrame);
            bool aRight = (aKeyboard != null && (aKeyboard.dKey.wasPressedThisFrame || aKeyboard.rightArrowKey.wasPressedThisFrame))
                         || (aGamepad != null && aGamepad.dpad.right.wasPressedThisFrame);
            if (aUp) mSelected = (mSelected + mRows.Count - 1) % mRows.Count;
            if (aDown) mSelected = (mSelected + 1) % mRows.Count;
            if (aLeft) ChangeRow(mSelected, -1);
            if (aRight) ChangeRow(mSelected, 1);
            Refresh();
        }

        void Refresh()
        {
            if (mRows.Count == 0) return;
            for (int aIndex = 0; aIndex < mRows.Count; aIndex++)
            {
                var aRow = mRows[aIndex];
                aRow.Element.EnableInClassList("setting-row--selected", aIndex == mSelected);
                aRow.FillBar.style.width = Length.Percent(Mathf.Clamp01(aRow.Fill()) * 100f);
                string aValue = aRow.Value();
                if (aRow.ValueLabel.text != aValue) aRow.ValueLabel.text = aValue;
            }
            if (mHint != null)
            {
                mHint.text = GameText.Get("settings.hint");
            }
        }

        static T Classed<T>(T iElement, string iClassName) where T : VisualElement
        {
            iElement.AddToClassList(iClassName);
            if (iElement is Label) iElement.pickingMode = PickingMode.Ignore;
            return iElement;
        }
    }
}
