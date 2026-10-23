using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace BattleFight
{
    /// <summary>
    /// 戦闘中の HUD(UI Toolkit)。レイアウトは Hud.uxml、見た目は Hud.uss / Common.uss。
    /// ここでは毎フレーム、ゲームの状態を要素に反映する(文字は変わったときだけ入れ替える)。
    /// 画面は 1920x1080 基準で、PanelSettings が画面の高さに合わせて拡大縮小する。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BattleHud : MonoBehaviour
    {
        static readonly string[] mSlotLabels = { "攻撃A", "攻撃B", "移動" };
        static readonly string[] mSlotNames = { "slot-a", "slot-b", "slot-m" };
        static readonly string[] mKeyboardSlotKeys = { "1", "2", "3", "4" };
        static readonly string[] mGamepadSlotKeys = { "←", "→", "↓", "↑" };
        static readonly Color[] mRankColors =
        {
            new Color(0.6f, 0.6f, 0.6f),
            new Color(0.4f, 0.7f, 1f),
            new Color(0.4f, 1f, 0.5f),
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.6f, 0.2f),
            new Color(1f, 0.3f, 0.3f),
            new Color(1f, 0.3f, 1f),
        };

        // 操作説明: (操作, キー)。キーは「・」で区切ると複数の枠になる。2列に並べ、補足のある「切り替え」は最後に1行で出す
        static readonly (string action, string keys)[] mKeyboardGuide =
        {
            ("移動", "W・A・S・D"), ("カメラ", "マウス"), ("攻撃A", "左クリック・J"), ("攻撃B", "右クリック・E・K"),
            ("移動スキル", "Shift・L"), ("ジャンプ", "Space"), ("ロックオン", "Tab・中クリック"), ("フィニッシャー", "F"),
            ("スキル編成", "P"), ("もう一度 / 選択へ", "R・T"), ("チェンジドライブ", "V"), ("ガード", "Q"), ("標的の切り替え", "Z・C"), ("切り替え", "1・2・3・4"),
        };
        static readonly (string action, string keys)[] mGamepadGuide =
        {
            ("移動", "左スティック"), ("カメラ", "右スティック"), ("攻撃A", "□"), ("攻撃B", "△"),
            ("移動スキル", "○"), ("ジャンプ", "×"), ("ロックオン", "L1"), ("フィニッシャー", "R1"),
            ("スキル編成", "Select"), ("もう一度", "Start"), ("チェンジドライブ", "L2"), ("ガード", "R2"), ("標的の切り替え", "右スティック左右"), ("切り替え", "十字キー"),
        };
        static readonly string[] mTips =
        {
            "技の硬直中にそのスロットを切り替えると、硬直をキャンセル",
            "切り替えた直後の一撃は強化(SWAP STRIKE)",
            "同じ技の連発はスタイルが伸びない",
            "敵の頭上の「弱」の武器で攻撃すると 1.5倍",
            "切り替え → ヒットで CHAIN(最大×5、ダメージ +50%)",
            "切り替えた瞬間、新しい武器で周りを攻撃(3秒ごと)",
            "居合は長押しで溜め。ワイヤーは視界内で一番近い ◎ へ飛ぶ",
            "移動スキルで攻撃をギリギリに避けると、敵だけが遅くなる(見切り)",
            "切り替えて当てると DRIVE がたまる。満タンで全プリセットの武器で連続攻撃",
            "スタイルランクが高いほど攻撃力が上がる(SSS で +30%)",
            "ガードは前からの攻撃を受け止める。押した直後に受けるとパリィ(弾き返して敵がよろける)",
        };

        [SerializeField, FormerlySerializedAs("slots")] SkillSlotController mSlots;
        [SerializeField, FormerlySerializedAs("executor")] SkillExecutor mExecutor;
        [SerializeField, FormerlySerializedAs("style")] StyleRankSystem mStyle;
        [SerializeField, FormerlySerializedAs("player")] Damageable mPlayer;
        [SerializeField, FormerlySerializedAs("lockOn")] LockOnSystem mLockOn;
        [SerializeField, FormerlySerializedAs("director")] ArenaDirector mDirector;
        [SerializeField, FormerlySerializedAs("view")] Camera mView;

        UIDocument mDocument;
        VisualElement mRoot;
        VisualElement mHud;
        bool mShowHelp = true;
        bool mGuideForGamepad;
        int mLastRank;
        readonly SkillData[] mShownSkills = new SkillData[3];
        readonly StringBuilder mBuilder = new StringBuilder();

        EnemyController mPhaseEnemy;
        float mPhaseAnnouncedAt = -99f;

        // 要素
        Label mHpValue;
        Label mShards;
        Label mMode;
        Label mGuideHint;
        Label mGuideTitle;
        Label mGuideClose;
        Label mTitle;
        Label mSubtitle;
        VisualElement mHpBar;
        VisualElement mHpFill;
        VisualElement mGuide;
        VisualElement mGuideRows;
        VisualElement mGuideTips;
        VisualElement mBoss;
        VisualElement mBossFill;
        VisualElement mBossArmor;
        VisualElement mBossArmorFill;
        Label mBossName;
        Label mBossPhase;
        Label mBossWeak;
        Label mRank;
        Label mChainCount;
        Label mChainBonus;
        Label mChainHint;
        Label mDebugLabel;
        Label mBonus;
        Label mChangeAttackLabel;
        VisualElement mRankFill;
        VisualElement mAnnouncements;
        VisualElement mChain;
        VisualElement mChainFill;
        VisualElement mSwapStrike;
        VisualElement mSwapStrikeFill;
        VisualElement mPresetsRoot;
        VisualElement mChangeAttack;
        VisualElement mChangeAttackFill;
        Label mReady;
        Label mPhaseTitle;
        Label mPhaseSub;
        Label mToast;
        Label mRankPower;
        Label mComboCount;
        Label mComboResult;
        Label mDriveLabel;
        VisualElement mWitch;
        VisualElement mCombo;
        VisualElement mComboFill;
        VisualElement mDrive;
        VisualElement mDriveFill;
        int mLastComboCount;
        VisualElement mPhaseBanner;
        VisualElement mToastWrap;
        VisualElement mResult;
        VisualElement mWorldLayer;
        VisualElement mImpactFlash;
        VisualElement mImpactVignette;
        VisualElement mDangerVignette;
        VisualElement mSlowVignette;
        /// <summary>今の、見切りの間の紫の縁の濃さ(テスト用)</summary>
        public float SlowAmount { get; private set; }

        /// <summary>HP がこの割合より少なくなると、画面の縁が赤く脈打つ</summary>
        public const float DangerHealthRatio = 0.3f;
        /// <summary>今の、HP が少ないときの赤い縁の濃さ(テスト用)</summary>
        public float DangerAmount { get; private set; }
        Texture2D mVignetteTexture;
        Label mResultTitle;
        Label mResultBody;
        Button mResultNext;
        Button mResultRetry;
        Button mResultSelect;
        readonly SlotView[] mSlotViews = new SlotView[3];
        readonly PresetView[] mPresetViews = new PresetView[SkillSlotController.PresetCount];

        // 3D の位置に出すもの(使い回す)
        readonly List<EnemyTag> mEnemyTags = new List<EnemyTag>();
        readonly List<Label> mWorldLabels = new List<Label>();
        readonly List<Label> mDamageLabels = new List<Label>();
        readonly List<Label> mAnnouncementLabels = new List<Label>();
        Label mLockMark;
        Label mGrappleMark;

        class SlotView
        {
            public VisualElement Card;
            public Label Popup;
            public Label Label;
            public Label Key;
            public Label Cancel;
            public Label Name;
            public Label Weapon;
            public Label Desc;
        }

        class PresetView
        {
            public VisualElement Chip;
            public Label Key;
            public Label Name;
        }

        class EnemyTag
        {
            public VisualElement Root;
            public VisualElement Fill;
            public VisualElement Armor;
            public VisualElement ArmorFill;
            public Label Weak;
            public Label Alert;
            public Label Skill;
            public Label Elite;
        }

        /// <summary>操作説明を表示しているか</summary>
        public bool IsHelpShown => mShowHelp;

        /// <summary>最後に触ったのがゲームパッドか(操作説明とキー表示をそのデバイスのものにする)</summary>
        public bool UsingGamepad { get; private set; }

        /// <summary>スロットのパネルに今出ているスキル(実際に使われるもの。プリセット方式ではプリセットの中身)</summary>
        public SkillData ShownSkill(SlotType iSlot) => mShownSkills[(int)iSlot];

        void OnEnable()
        {
            mShowHelp = GameSettings.ShowGuide;
            mDocument = GetComponent<UIDocument>();
            mRoot = mDocument.rootVisualElement;
            Bind();

            mSlots.SlotChanged += OnSlotChanged;
            EnemyController.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            mSlots.SlotChanged -= OnSlotChanged;
            EnemyController.PhaseChanged -= OnPhaseChanged;
        }

        void Bind()
        {
            mHud = mRoot.Q("hud");
            mImpactFlash = mRoot.Q("impact-flash");
            mImpactVignette = mRoot.Q("impact-vignette");
            mDangerVignette = mRoot.Q("danger-vignette");
            mSlowVignette = mRoot.Q("slow-vignette");
            if (mSlowVignette != null)
            {
                if (mVignetteTexture == null) mVignetteTexture = CreateVignetteTexture();
                mSlowVignette.style.backgroundImage = new StyleBackground(mVignetteTexture);
                mSlowVignette.style.unityBackgroundImageTintColor = new Color(0.6f, 0.4f, 1f);
            }
            if (mVignetteTexture == null) mVignetteTexture = CreateVignetteTexture();
            if (mImpactVignette != null) mImpactVignette.style.backgroundImage = new StyleBackground(mVignetteTexture);
            if (mDangerVignette != null)
            {
                mDangerVignette.style.backgroundImage = new StyleBackground(mVignetteTexture);
                mDangerVignette.style.unityBackgroundImageTintColor = new Color(0.85f, 0.05f, 0.05f);
            }
            mHpValue = mRoot.Q<Label>("hp-value");
            mHpFill = mRoot.Q("hp-fill");
            mHpBar = mHpFill.parent;
            mShards = mRoot.Q<Label>("shards");
            mMode = mRoot.Q<Label>("mode");
            mGuideHint = mRoot.Q<Label>("guide-hint");
            mGuide = mRoot.Q("guide");
            mGuideTitle = mRoot.Q<Label>("guide-title");
            mGuideRows = mRoot.Q("guide-rows");
            mGuideTips = mRoot.Q("guide-tips");
            mGuideClose = mRoot.Q<Label>("guide-close");
            mTitle = mRoot.Q<Label>("title");
            mSubtitle = mRoot.Q<Label>("subtitle");
            mBoss = mRoot.Q("boss");
            mBossName = mRoot.Q<Label>("boss-name");
            mBossPhase = mRoot.Q<Label>("boss-phase");
            mBossWeak = mRoot.Q<Label>("boss-weak");
            mBossFill = mRoot.Q("boss-fill");
            mBossArmor = mRoot.Q("boss-armor");
            mBossArmorFill = mRoot.Q("boss-armor-fill");
            mRank = mRoot.Q<Label>("rank");
            mRankFill = mRoot.Q("rank-fill");
            mAnnouncements = mRoot.Q("announcements");
            mChain = mRoot.Q("chain");
            mChainCount = mRoot.Q<Label>("chain-count");
            mChainBonus = mRoot.Q<Label>("chain-bonus");
            mChainFill = mRoot.Q("chain-fill");
            mChainHint = mRoot.Q<Label>("chain-hint");
            mDebugLabel = mRoot.Q<Label>("debug");
            mSwapStrike = mRoot.Q("swap-strike");
            mSwapStrikeFill = mRoot.Q("swap-strike-fill");
            mBonus = mRoot.Q<Label>("bonus");
            mPresetsRoot = mRoot.Q("presets");
            mChangeAttack = mRoot.Q("change-attack");
            mChangeAttackLabel = mRoot.Q<Label>("change-attack-label");
            mChangeAttackFill = mRoot.Q("change-attack-fill");
            mReady = mRoot.Q<Label>("ready");
            mPhaseBanner = mRoot.Q("phase-banner");
            mPhaseTitle = mRoot.Q<Label>("phase-title");
            mPhaseSub = mRoot.Q<Label>("phase-sub");
            mToastWrap = mRoot.Q("toast-wrap");
            mToast = mRoot.Q<Label>("toast");
            mResult = mRoot.Q("result");
            mResultTitle = mRoot.Q<Label>("result-title");
            mResultBody = mRoot.Q<Label>("result-body");
            mResultNext = mRoot.Q<Button>("result-next");
            mResultRetry = mRoot.Q<Button>("result-retry");
            mResultSelect = mRoot.Q<Button>("result-select");
            mWorldLayer = mRoot.Q("world-layer");
            mWitch = mRoot.Q("witch");
            mRankPower = mRoot.Q<Label>("rank-power");
            mCombo = mRoot.Q("combo");
            mComboCount = mRoot.Q<Label>("combo-count");
            mComboFill = mRoot.Q("combo-fill");
            mComboResult = mRoot.Q<Label>("combo-result");
            mDrive = mRoot.Q("drive");
            mDriveLabel = mRoot.Q<Label>("drive-label");
            mDriveFill = mRoot.Q("drive-fill");

            for (int aIndex = 0; aIndex < 3; aIndex++)
            {
                var aInstance = mRoot.Q(mSlotNames[aIndex]);
                mSlotViews[aIndex] = new SlotView
                {
                    Card = aInstance.Q("card"),
                    Popup = aInstance.Q<Label>("popup"),
                    Label = aInstance.Q<Label>("label"),
                    Key = aInstance.Q<Label>("key"),
                    Cancel = aInstance.Q<Label>("cancel"),
                    Name = aInstance.Q<Label>("name"),
                    Weapon = aInstance.Q<Label>("weapon"),
                    Desc = aInstance.Q<Label>("desc"),
                };
                mSlotViews[aIndex].Label.text = mSlotLabels[aIndex];
            }
            for (int aPresetViewIndex = 0; aPresetViewIndex < mPresetViews.Length; aPresetViewIndex++)
            {
                var aInstance = mRoot.Q($"preset-{aPresetViewIndex + 1}");
                mPresetViews[aPresetViewIndex] = new PresetView { Chip = aInstance.Q("chip"), Key = aInstance.Q<Label>("key"), Name = aInstance.Q<Label>("name") };
            }

            mResultNext.clicked += () => mDirector.NextStage();
            mResultRetry.clicked += () => mDirector.Retry();
            mResultSelect.clicked += () => mDirector.BackToStageSelect();

            mLockMark = new Label("◇") { pickingMode = PickingMode.Ignore };
            mLockMark.AddToClassList("world-mark");
            mLockMark.AddToClassList("world-mark--lock");
            mGrappleMark = new Label("◎") { pickingMode = PickingMode.Ignore };
            mGrappleMark.AddToClassList("world-mark");
            mGrappleMark.AddToClassList("world-mark--grapple");
            var aGrappleHint = new Label { name = "grapple-hint", pickingMode = PickingMode.Ignore };
            aGrappleHint.AddToClassList("world-mark__hint");
            mGrappleMark.Add(aGrappleHint);
            mWorldLayer.Add(mLockMark);
            mWorldLayer.Add(mGrappleMark);

            BuildGuide(false);
        }

        // ---------- 入力 ----------

        /// <summary>このフレームに触ったデバイスを見て、表示をキーボード用かゲームパッド用に切り替える</summary>
        void TrackLastDevice()
        {
            if (GamepadTouched(Gamepad.current))
            {
                UsingGamepad = true;
                return;
            }
            var aKeyboard = Keyboard.current;
            var aMouse = Mouse.current;
            if ((aKeyboard != null && aKeyboard.anyKey.wasPressedThisFrame)
                || (aMouse != null && (aMouse.leftButton.wasPressedThisFrame || aMouse.rightButton.wasPressedThisFrame
                    || aMouse.middleButton.wasPressedThisFrame || aMouse.delta.ReadValue().sqrMagnitude > 4f)))
            {
                UsingGamepad = false;
            }
        }

        static bool GamepadTouched(Gamepad iGamepad)
        {
            if (iGamepad == null) return false;
            if (iGamepad.leftStick.ReadValue().sqrMagnitude > 0.25f || iGamepad.rightStick.ReadValue().sqrMagnitude > 0.25f) return true;
            foreach (var aControl in iGamepad.allControls)
            {
                if (aControl is ButtonControl aButton && !aButton.synthetic && aButton.wasPressedThisFrame) return true;
            }
            return false;
        }

        static bool HelpTogglePressed()
        {
            var aKeyboard = Keyboard.current;
            if (aKeyboard != null && (aKeyboard.digit0Key.wasPressedThisFrame || aKeyboard.numpad0Key.wasPressedThisFrame || aKeyboard.f1Key.wasPressedThisFrame)) return true;
            var aGamepad = Gamepad.current;
            return aGamepad != null && aGamepad.rightStickButton.wasPressedThisFrame;
        }

        string SlotKey(int iIndex) => (UsingGamepad ? mGamepadSlotKeys : mKeyboardSlotKeys)[iIndex];

        void OnPhaseChanged(EnemyController iEnemy)
        {
            mPhaseEnemy = iEnemy;
            mPhaseAnnouncedAt = Time.unscaledTime;
        }

        void OnSlotChanged(SlotType iSlot)
        {
            int aIndex = (int)iSlot;
            // 表示中のスキルはすぐ更新する(編成を変えた直後に ShownSkill を読んでも古くならないように)
            mShownSkills[aIndex] = mSlots.GetCurrent(iSlot);
            // 切り替えた瞬間だけ大きくし、新しいスキル名を上に出す(戻りは USS の transition)
            var aView = mSlotViews[aIndex];
            if (aView == null) return;
            var aSkill = mSlots.GetCurrent(iSlot);
            aView.Card.AddToClassList("slot--swapped");
            aView.Card.schedule.Execute(() => aView.Card.RemoveFromClassList("slot--swapped")).StartingIn(60);
            if (aSkill != null)
            {
                aView.Popup.text = $"⇒ {aSkill.DisplayName}";
                aView.Popup.style.color = WeaponColor(aSkill);
                aView.Popup.AddToClassList("slot__popup--show");
                aView.Popup.schedule.Execute(() => aView.Popup.RemoveFromClassList("slot__popup--show")).StartingIn(380);
            }
        }

        void Update()
        {
            TrackLastDevice();
            if (HelpTogglePressed())
            {
                mShowHelp = !mShowHelp;
                GameSettings.ShowGuide = mShowHelp;
            }
            Refresh();
        }

        // ---------- 反映 ----------

        void Refresh()
        {
            if (mRoot == null) return;
            bool aVisible = mDirector.State != ArenaDirector.GameState.StageSelect && !LoadoutEditorUI.IsOpen;
            Show(mHud, aVisible);
            // スロットの表示はテストや他の画面からも読むので、HUD を隠していても更新する
            RefreshSlots();
            if (!aVisible) return;

            RefreshStatus();
            RefreshGuide();
            RefreshHeader();
            RefreshBoss();
            RefreshStyle();
            RefreshBottom();
            RefreshCenter();
            RefreshWorld();
        }

        void RefreshStatus()
        {
            float aRatio = mPlayer.MaxHealth > 0f ? mPlayer.Health / mPlayer.MaxHealth : 0f;
            SetText(mHpValue, $"{Mathf.CeilToInt(mPlayer.Health)} <color=#8A93A3>/ {Mathf.CeilToInt(mPlayer.MaxHealth)}</color>");
            SetFill(mHpFill, aRatio);
            mHpBar.EnableInClassList("low", aRatio < 0.3f);
            SetText(mShards, $"刻片 {mDirector.Shards}");
            string aModeName = mSlots.Mode == SwapMode.Preset ? "プリセット方式" : "ラック方式";
            SetText(mMode, $"{aModeName}  <color=#FFFFFF>[{(UsingGamepad ? "Select" : "P")}]</color> 編成");
        }

        void RefreshGuide()
        {
            if (mGuideForGamepad != UsingGamepad) BuildGuide(UsingGamepad);
            Show(mGuide, mShowHelp);
            Show(mGuideHint, !mShowHelp);
            SetText(mGuideHint, UsingGamepad ? "[R3] 操作説明" : "[0] 操作説明");
        }

        void BuildGuide(bool iGamepad)
        {
            mGuideForGamepad = iGamepad;
            mGuideTitle.text = iGamepad ? "操作(ゲームパッド)" : "操作(キーボード・マウス)";
            mGuideRows.Clear();
            foreach (var (aAction, aKeys) in iGamepad ? mGamepadGuide : mKeyboardGuide)
            {
                var aRow = new VisualElement { pickingMode = PickingMode.Ignore };
                aRow.AddToClassList("guide__row");
                aRow.Add(Classed(new Label(aAction), "guide__action"));
                var aKeyRow = Classed(new VisualElement(), "guide__keys");
                foreach (var aKey in aKeys.Split('・')) aKeyRow.Add(Classed(new Label(aKey), "key"));
                aRow.Add(aKeyRow);
                if (aAction == "切り替え")
                {
                    // 補足があるので1行まるごと使う
                    aRow.AddToClassList("guide__row--wide");
                    aRow.Add(Classed(new Label(iGamepad ? "ラック: ←/→/↓ で各スロット  プリセット: 十字キーで一括" : "ラック: 1/2/3 で各スロット  プリセット: 1〜4 で一括"), "guide__note"));
                }
                mGuideRows.Add(aRow);
            }
            mGuideTips.Clear();
            foreach (var aTip in mTips) mGuideTips.Add(Classed(new Label("・" + aTip), "guide__tip"));
            mGuideClose.text = iGamepad ? "[R3] で閉じる" : "[0] で閉じる";
        }

        void RefreshHeader()
        {
            var aStage = mDirector.CurrentStage;
            if (aStage == null)
            {
                SetText(mTitle, "");
                Show(mSubtitle, false);
                return;
            }

            var aRegion = ExplorationRegion.Active;
            if (aStage.Kind == StageKind.Exploration && aRegion != null)
            {
                // 探索: 今いる部屋と、集めたスキルの数(ボス戦中は体力と重なるので出さない)
                string aRoom = aRegion.CurrentRoomName();
                SetText(mTitle, aRoom.Length > 0 ? $"{aRegion.RegionName}  <color=#8A93A3>-</color>  {aRoom}" : aRegion.RegionName);
                SetText(mSubtitle, $"集めたスキル  {ExplorationSave.UnlockedCount} / {aRegion.TotalSkills}");
                Show(mSubtitle, mDirector.Boss == null);
                return;
            }

            string aText;
            if (aStage.Kind == StageKind.Training || mDirector.WaveNumber <= 0) aText = aStage.DisplayName;
            else if (aStage.Kind == StageKind.Endless) aText = $"{aStage.DisplayName}   <color=#8A93A3>{mDirector.WaveLabel}</color> {mDirector.WaveNumber}";
            else aText = $"{aStage.DisplayName}   <color=#8A93A3>{mDirector.WaveLabel}</color>  {mDirector.WaveNumber} / {mDirector.WaveCount}";
            SetText(mTitle, aText);
            Show(mSubtitle, false);
        }

        void RefreshBoss()
        {
            var aEnemy = mDirector.Boss;
            Show(mBoss, aEnemy != null);
            if (aEnemy == null) return;
            var aDamageable = aEnemy.Damageable;
            SetText(mBossName, aEnemy.Profile.DisplayName);
            Show(mBossPhase, aEnemy.IsPhase2);
            string aWeakness = WeaknessText(aEnemy);
            var aSet = aEnemy.CurrentSkillSet;
            if (aSet != null && aEnemy.Profile.SkillSets.Count > 1)
            {
                // 型を切り替えるボスは、今の型も出す
                aWeakness = $"<color=#{ColorUtility.ToHtmlStringRGB(aSet.Color)}>{aSet.Name}</color>   " + aWeakness;
            }
            SetText(mBossWeak, aWeakness.Length > 0 ? $"<color=#8A93A3>弱点</color> {aWeakness}" : "");
            SetFill(mBossFill, aDamageable.Health / aDamageable.MaxHealth);
            Show(mBossArmor, aDamageable.MaxArmor > 0f);
            if (aDamageable.MaxArmor > 0f) SetFill(mBossArmorFill, aDamageable.Armor / aDamageable.MaxArmor);
        }

        void RefreshStyle()
        {
            var aMeter = mStyle.Meter;
            var aColor = mRankColors[Mathf.Min(aMeter.RankIndex, mRankColors.Length - 1)];
            SetText(mRank, aMeter.RankName);
            mRank.style.color = aColor;
            mRankFill.style.backgroundColor = aColor;
            SetFill(mRankFill, aMeter.RankProgress);
            if (aMeter.RankIndex > mLastRank)
            {
                mRank.AddToClassList("pulse");
                mRank.schedule.Execute(() => mRank.RemoveFromClassList("pulse")).StartingIn(40);
            }
            mLastRank = aMeter.RankIndex;

            // ランクによる攻撃力の強化
            int aPower = Mathf.RoundToInt((mStyle.Config.DamageMultiplier(aMeter.RankIndex) - 1f) * 100f);
            Show(mRankPower, aPower > 0);
            SetText(mRankPower, $"攻撃力 +{aPower}%");

            // コンボ(2ヒットから出す)。途切れたら評価を少し出す
            var aCounter = mExecutor.Combo;
            Show(mCombo, aCounter.Count >= 2);
            if (aCounter.Count >= 2)
            {
                SetText(mComboCount, aCounter.Count.ToString());
                SetFill(mComboFill, aCounter.Remaining(Time.time));
                if (aCounter.Count > mLastComboCount)
                {
                    mComboCount.AddToClassList("pulse");
                    mComboCount.schedule.Execute(() => mComboCount.RemoveFromClassList("pulse")).StartingIn(40);
                }
            }
            mLastComboCount = aCounter.Count;
            bool aShowResult = aCounter.Count == 0 && aCounter.LastFinished >= 10 && Time.time - aCounter.LastFinishedTime < 1.6f;
            Show(mComboResult, aShowResult);
            if (aShowResult) SetText(mComboResult, $"{aCounter.LastFinished} HITS  {ComboCounter.Rating(aCounter.LastFinished)}");

            // お知らせ(新しいものを上に)
            int aCount = mStyle.Announcements.Count;
            while (mAnnouncementLabels.Count < aCount)
            {
                var aLabel = Classed(new Label(), "announcement");
                mAnnouncementLabels.Add(aLabel);
                mAnnouncements.Add(aLabel);
            }
            for (int aAnnouncementLabelIndex = 0; aAnnouncementLabelIndex < mAnnouncementLabels.Count; aAnnouncementLabelIndex++)
            {
                var aLabel = mAnnouncementLabels[aAnnouncementLabelIndex];
                if (aAnnouncementLabelIndex >= aCount)
                {
                    Show(aLabel, false);
                    continue;
                }
                var aAnnouncement = mStyle.Announcements[aCount - 1 - aAnnouncementLabelIndex];
                float aAge = Time.unscaledTime - aAnnouncement.Time;
                Show(aLabel, true);
                SetText(aLabel, aAnnouncement.Text);
                var aOtherColor = aAnnouncement.Color;
                aOtherColor.a = Mathf.Clamp01((1f - aAge / StyleRankSystem.AnnouncementLifetime) * 1.5f);
                aLabel.style.color = aOtherColor;
            }

            var aChainMeter = mExecutor.SwapChain;
            bool aChaining = aChainMeter.Chain > 0;
            Show(mChain, aChaining);
            Show(mChainHint, !aChaining);
            if (aChaining)
            {
                int aBonusPercent = Mathf.RoundToInt((aChainMeter.DamageMultiplier - 1f) * 100f);
                SetText(mChainCount, $"CHAIN ×{aChainMeter.Chain}{(aChainMeter.Chain >= aChainMeter.MaxChain ? "  MAX" : "")}");
                SetText(mChainBonus, $"ダメージ +{aBonusPercent}%");
                SetFill(mChainFill, aChainMeter.Remaining(Time.time));
            }
            SetText(mDebugLabel, mExecutor.DebugLabel);
        }

        void RefreshSlots()
        {
            bool aRackMode = mSlots.Mode == SwapMode.Rack;
            for (int aIndex = 0; aIndex < 3; aIndex++)
            {
                var aSlot = (SlotType)aIndex;
                var aView = mSlotViews[aIndex];
                var aCurrent = mSlots.GetCurrent(aSlot);
                mShownSkills[aIndex] = aCurrent;
                if (aView == null) continue;

                var aWeaponData = aCurrent != null ? mSlots.GetWeaponData(aCurrent.Weapon) : null;
                var aWeaponColor = aWeaponData != null ? aWeaponData.Color : new Color(1f, 1f, 1f, 0.25f);
                bool aActive = mExecutor.Current != null && mExecutor.Current == aCurrent && !mExecutor.IsFinisherActive;
                bool aCancelReady = aRackMode && mExecutor.CanSwapCancelNow(aSlot);

                aView.Card.style.borderTopColor = aWeaponColor;
                aView.Card.EnableInClassList("slot--active", aActive);
                aView.Card.EnableInClassList("slot--cancel", aCancelReady && Mathf.PingPong(Time.unscaledTime * 8f, 1f) > 0.4f);
                Show(aView.Cancel, aCancelReady);
                Show(aView.Key, aRackMode);
                SetText(aView.Key, SlotKey(aIndex));

                SetText(aView.Name, aCurrent != null ? aCurrent.DisplayName : "(なし)");
                aView.Name.style.color = aCurrent != null ? aWeaponColor : new Color(1f, 1f, 1f, 0.35f);
                SetText(aView.Weapon, aWeaponData != null ? aWeaponData.DisplayName : "");
                SetText(aView.Desc, aRackMode ? RackLine(mSlots.GetRack(aSlot)) : DescribeShort(aCurrent));
            }
        }

        string RackLine(SkillRack iRack)
        {
            mBuilder.Clear();
            for (int aRackIndex = 0; aRackIndex < iRack.Count; aRackIndex++)
            {
                var aSkill = iRack.Skills[aRackIndex];
                string aHex = ColorUtility.ToHtmlStringRGB(WeaponColor(aSkill));
                if (aRackIndex > 0) mBuilder.Append("  ");
                if (aRackIndex == iRack.CurrentIndex) mBuilder.Append($"<color=#{aHex}>■{aSkill.DisplayName}</color>");
                else if (aSkill == iRack.Next) mBuilder.Append($"<color=#{aHex}>▶{aSkill.DisplayName}</color>");
                else mBuilder.Append($"<color=#777777>{aSkill.DisplayName}</color>");
            }
            return mBuilder.ToString();
        }

        void RefreshBottom()
        {
            // スワップストライク
            bool aStrike = mExecutor.SwapStrikeReady;
            Show(mSwapStrike, aStrike);
            if (aStrike) SetFill(mSwapStrikeFill, mExecutor.SwapStrikeRemaining);

            // 武器種ボーナス
            var aWeaponBonus = mSlots.Bonus;
            var aData = mSlots.GetWeaponData(aWeaponBonus.Weapon);
            string aWeaponName = aData != null ? aData.DisplayName : aWeaponBonus.Weapon.ToString();
            string aText = "";
            Color aColor = Color.white;
            switch (aWeaponBonus.Kind)
            {
                case WeaponBonusKind.Mastery:
                    var aFinisher = mSlots.MasteryFinisher;
                    aText = $"マスタリー({aWeaponName})   <color=#FFFFFF>[{(UsingGamepad ? "R1" : "F")}]</color> フィニッシャー「{(aFinisher != null ? aFinisher.DisplayName : "-")}」";
                    aColor = aData != null ? aData.Color : Color.white;
                    break;
                case WeaponBonusKind.Synergy:
                    aText = $"シナジー({aWeaponName})   ひるみ +{Mathf.RoundToInt((aData != null ? aData.SynergyStaggerBonus : 0f) * 100f)}%";
                    aColor = aData != null ? Color.Lerp(aData.Color, Color.white, 0.3f) : Color.white;
                    break;
                case WeaponBonusKind.Arsenal:
                    aText = $"アーセナル   スタイル獲得 x{mStyle.Config.ArsenalMultiplier:0.##}";
                    aColor = new Color(0.9f, 0.9f, 1f);
                    break;
            }
            Show(mBonus, aText.Length > 0);
            SetText(mBonus, aText);
            mBonus.style.color = aColor;

            // プリセット
            bool aPresetMode = mSlots.Mode == SwapMode.Preset;
            Show(mPresetsRoot, aPresetMode);
            if (aPresetMode)
            {
                for (int aPresetViewIndex = 0; aPresetViewIndex < mPresetViews.Length; aPresetViewIndex++)
                {
                    var aView = mPresetViews[aPresetViewIndex];
                    bool aExists = aPresetViewIndex < mSlots.Presets.Count;
                    Show(aView.Chip, aExists);
                    if (!aExists) continue;
                    var aPreset = mSlots.Presets[aPresetViewIndex];
                    var aAttackA = aPreset.AttackA;
                    aView.Chip.style.borderLeftColor = aAttackA != null ? WeaponColor(aAttackA) : new Color(1f, 1f, 1f, 0.2f);
                    aView.Chip.EnableInClassList("preset--current", aPresetViewIndex == mSlots.PresetIndex);
                    aView.Chip.EnableInClassList("preset--empty", aPreset.IsEmpty);
                    aView.Chip.EnableInClassList("preset--cancel",
                        aPresetViewIndex != mSlots.PresetIndex && mExecutor.CanPresetCancel(aPresetViewIndex) && Mathf.PingPong(Time.unscaledTime * 8f, 1f) > 0.4f);
                    SetText(aView.Key, SlotKey(aPresetViewIndex));
                    SetText(aView.Name, aPreset.Name);
                }
            }

            // チェンジドライブ
            bool aFull = mExecutor.Drive.IsFull;
            mDrive.EnableInClassList("full", aFull && Mathf.PingPong(Time.unscaledTime * 3f, 1f) > 0.3f);
            SetFill(mDriveFill, mExecutor.Drive.Value);
            SetText(mDriveLabel, aFull ? $"[{(UsingGamepad ? "L2" : "V")}] CHANGE DRIVE" : mExecutor.IsDriving ? "CHANGE DRIVE!" : "DRIVE");

            // チェンジアタック
            bool aReadyNow = mExecutor.ChangeAttackReady;
            mChangeAttack.EnableInClassList("ready", aReadyNow);
            SetText(mChangeAttackLabel, aReadyNow ? "CHANGE ATTACK 準備OK" : "CHANGE ATTACK");
            SetFill(mChangeAttackFill, mExecutor.ChangeAttackCharge);
        }

        void RefreshCenter()
        {
            var aState = mDirector.State;
            Show(mReady, aState == ArenaDirector.GameState.Starting);

            // 見切り(敵だけ遅くなっている間)
            Show(mWitch, EnemyTime.IsSlowed);
            if (EnemyTime.IsSlowed) mWitch.style.opacity = Mathf.Clamp01(EnemyTime.Remaining * 2.5f);

            // ボスの第二形態の告知(2秒)
            float aSincePhase = Time.unscaledTime - mPhaseAnnouncedAt;
            bool aPhase = mPhaseEnemy != null && aSincePhase < 2f && aState != ArenaDirector.GameState.Cleared && aState != ArenaDirector.GameState.GameOver;
            Show(mPhaseBanner, aPhase);
            if (aPhase)
            {
                var aColor = mPhaseEnemy.Profile.TelegraphColor;
                aColor.a = Mathf.Clamp01((2f - aSincePhase) / 0.5f);
                SetText(mPhaseTitle, $"{mPhaseEnemy.Profile.DisplayName}  第二形態");
                SetText(mPhaseSub, mPhaseEnemy.Profile.Phase2Message);
                mPhaseTitle.style.color = aColor;
                mPhaseSub.style.color = aColor;
            }

            // 探索のお知らせ(4秒で消える)
            var aRegion = ExplorationRegion.Active;
            float aSinceToast = aRegion != null ? Time.unscaledTime - aRegion.MessageTime : 99f;
            bool aShowToast = aRegion != null && !string.IsNullOrEmpty(aRegion.Message) && aSinceToast < 4f && !aPhase;
            Show(mToastWrap, aShowToast);
            if (aShowToast)
            {
                SetText(mToast, aRegion.Message);
                mToast.style.color = aRegion.MessageColor;
                mToast.style.opacity = Mathf.Clamp01((4f - aSinceToast) / 0.6f);
            }

            RefreshResult(aState);
        }

        void RefreshResult(ArenaDirector.GameState iState)
        {
            bool aCleared = iState == ArenaDirector.GameState.Cleared;
            bool aOver = iState == ArenaDirector.GameState.GameOver;
            Show(mResult, aCleared || aOver);
            if (!aCleared && !aOver) return;

            if (aCleared && mDirector.IsExploring && ExplorationRegion.Active != null)
            {
                SetText(mResultTitle, "探索クリア");
                SetText(mResultBody, $"集めたスキル {ExplorationSave.UnlockedCount} / {ExplorationRegion.Active.TotalSkills}   タイム {mDirector.ElapsedTime:0.0}秒\n続きからは、マップを自由に回れます");
                mResultTitle.style.color = new Color(1f, 0.9f, 0.4f);
            }
            else if (aCleared)
            {
                string aBest = mStyle.Config.RankNames[Mathf.Min(mStyle.HighestRank, mStyle.Config.RankNames.Length - 1)];
                string aRecord = mDirector.NewRecord ? "   <color=#FFE066>NEW RECORD!</color>" : "";
                SetText(mResultTitle, "STAGE CLEAR");
                SetText(mResultBody, $"タイム {mDirector.ElapsedTime:0.0}秒   最高ランク {aBest}   刻片 {mDirector.Shards}{aRecord}");
                mResultTitle.style.color = new Color(1f, 0.9f, 0.4f);
            }
            else
            {
                bool aEndless = mDirector.CurrentStage != null && mDirector.CurrentStage.Kind == StageKind.Endless;
                SetText(mResultTitle, "GAME OVER");
                SetText(mResultBody, aEndless ? $"到達 WAVE {mDirector.WaveNumber}{(mDirector.NewRecord ? "   <color=#FFE066>NEW RECORD!</color>" : "")}" : "");
                mResultTitle.style.color = new Color(1f, 0.35f, 0.35f);
            }
            Show(mResultBody, mResultBody.text.Length > 0);

            Show(mResultNext, aCleared && mDirector.HasNextStage);
            SetText(mResultNext, "次のステージ [N]");
            SetText(mResultRetry, !mDirector.IsExploring ? "もう一度 [R]" : aOver ? "祭壇から再開 [R]" : "続きから [R]");
            SetText(mResultSelect, "ステージ選択 [T]");
        }

        // ---------- 3D の位置に出すもの ----------

        void RefreshWorld()
        {
            if (mView == null || mRoot.panel == null) return;

            // 敵の体力・弱点・予備動作の「!」(ボスは上の体力バーに出すので除く)
            int aUsed = 0;
            var aAttackA = mSlots.GetCurrent(SlotType.AttackA);
            foreach (var aEnemy in EnemyController.Active)
            {
                if (aEnemy.IsDead || aEnemy.Profile.IsBoss) continue;
                if (!ToPanel(aEnemy.transform.position + Vector3.up * (aEnemy.Height + 0.4f), out var aPoint)) continue;
                var aTag = EnemyTagAt(aUsed++);
                Show(aTag.Root, true);
                Place(aTag.Root, aPoint);
                var aDamageable = aEnemy.Damageable;
                SetFill(aTag.Fill, aDamageable.Health / aDamageable.MaxHealth);
                Show(aTag.Armor, aDamageable.MaxArmor > 0f);
                if (aDamageable.MaxArmor > 0f) SetFill(aTag.ArmorFill, aDamageable.Armor / aDamageable.MaxArmor);
                string aWeakness = WeaknessText(aEnemy);
                bool aWeakNow = aAttackA != null && aEnemy.IsWeakTo(aAttackA.Weapon);
                Show(aTag.Weak, aWeakness.Length > 0);
                SetText(aTag.Weak, (aWeakNow ? "弱点 " : "弱 ") + aWeakness);
                aTag.Weak.EnableInClassList("now", aWeakNow);
                Show(aTag.Elite, aEnemy.Profile.Elite != EliteAffix.None);
                if (aEnemy.Profile.Elite != EliteAffix.None) SetText(aTag.Elite, $"精鋭・{EliteMaker.Label(aEnemy.Profile.Elite)}");
                Show(aTag.Alert, aEnemy.IsTelegraphing);
                aTag.Alert.style.color = aEnemy.Profile.TelegraphColor;

                // プレイヤーと同じスキルを使うときは、予備動作中に技の名前を出す
                var aSkill = aEnemy.IsTelegraphing && aEnemy.CurrentAttack != null ? aEnemy.CurrentAttack.SourceSkill : null;
                var aSet = aEnemy.CurrentSkillSet;
                bool aSwapped = aSet != null && Time.time - aEnemy.SkillSetChangedAt < 1.5f;
                Show(aTag.Skill, aSkill != null || aSwapped);
                if (aSkill != null)
                {
                    SetText(aTag.Skill, aSkill.DisplayName);
                    aTag.Skill.style.color = WeaponColor(aSkill);
                }
                else if (aSwapped)
                {
                    SetText(aTag.Skill, $"⇄ {aSet.Name}");
                    aTag.Skill.style.color = aSet.Color;
                }
            }
            for (int aEnemyTagIndex = aUsed; aEnemyTagIndex < mEnemyTags.Count; aEnemyTagIndex++) Show(mEnemyTags[aEnemyTagIndex].Root, false);

            // 探索の目印(近いものだけ)
            aUsed = 0;
            var aPlayerPosition = mPlayer.transform.position;
            foreach (var aWorldLabel in WorldLabel.All)
            {
                if (aWorldLabel == null || string.IsNullOrEmpty(aWorldLabel.Text)) continue;
                var aPosition = aWorldLabel.Position;
                float aDistance = Vector3.Distance(aPosition, aPlayerPosition);
                if (aDistance > aWorldLabel.ShowDistance || !ToPanel(aPosition, out var aPoint)) continue;
                var aLabel = PooledLabel(mWorldLabels, aUsed++, "world-label");
                Place(aLabel, aPoint);
                SetText(aLabel, aWorldLabel.Text);
                var aColor = aWorldLabel.Color;
                aColor.a *= Mathf.Clamp01((aWorldLabel.ShowDistance - aDistance) / 3f);
                aLabel.style.color = aColor;
            }
            for (int aWorldLabelIndex = aUsed; aWorldLabelIndex < mWorldLabels.Count; aWorldLabelIndex++) Show(mWorldLabels[aWorldLabelIndex], false);

            // ワイヤーで飛ぶ先
            var aGrapple = mExecutor.GrapplePreview;
            Vector2 aGrapplePoint = default;
            bool aShowGrapple = aGrapple != null && ToPanel(aGrapple.transform.position, out aGrapplePoint);
            Show(mGrappleMark, aShowGrapple);
            if (aShowGrapple)
            {
                Place(mGrappleMark, aGrapplePoint);
                float aPulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f);
                mGrappleMark.style.scale = new Scale(new Vector3(aPulse, aPulse, 1f));
                SetText(mGrappleMark.Q<Label>("grapple-hint"), UsingGamepad ? "○" : "Shift / L");
            }

            // ロックオン
            var aTarget = mLockOn.Target;
            Vector2 aLockPoint = default;
            bool aShowLock = aTarget != null && ToPanel(aTarget.CenterPoint, out aLockPoint);
            Show(mLockMark, aShowLock);
            if (aShowLock) Place(mLockMark, aLockPoint);

            // ダメージの数字
            aUsed = 0;
            if (CombatFeedback.Instance != null)
            {
                foreach (var aNumber in CombatFeedback.Instance.DamageNumbers)
                {
                    float aAge = Time.unscaledTime - aNumber.Time;
                    if (!ToPanel(aNumber.Position + Vector3.up * aAge * 1.2f, out var aPoint)) continue;
                    var aLabel = PooledLabel(mDamageLabels, aUsed++, "damage");
                    Place(aLabel, aPoint);
                    SetText(aLabel, aNumber.Text);
                    var aColor = aNumber.Color;
                    aColor.a = 1f - aAge / CombatFeedback.DamageNumberLifetime;
                    aLabel.style.color = aColor;
                    // 大きい当たりほど大きく。出た瞬間に大きく弾んで、すぐ元の大きさへ
                    float aScale = aNumber.Scale > 0f ? aNumber.Scale : 1f;
                    float aPop = 1f + 0.5f * Mathf.Clamp01(1f - aAge / 0.12f) * aScale;
                    aLabel.style.fontSize = mDamageFontSize * aScale * aPop;
                }
            }
            for (int aDamageLabelIndex = aUsed; aDamageLabelIndex < mDamageLabels.Count; aDamageLabelIndex++) Show(mDamageLabels[aDamageLabelIndex], false);

            // インパクトフレームと、画面の縁の光
            var aFeedback = CombatFeedback.Instance;
            if (mImpactFlash != null) mImpactFlash.style.opacity = aFeedback != null ? aFeedback.FlashAmount * 0.85f : 0f;
            if (mImpactVignette != null)
            {
                float aVignette = aFeedback != null ? aFeedback.VignetteAmount : 0f;
                mImpactVignette.style.opacity = aVignette;
                if (aVignette > 0f) mImpactVignette.style.unityBackgroundImageTintColor = aFeedback.VignetteColor;
            }

            // HP が少ないとき: 画面の縁が、心臓の鼓動のように赤く脈打つ
            float aHealthRatio = mPlayer != null && mPlayer.MaxHealth > 0f ? mPlayer.Health / mPlayer.MaxHealth : 1f;
            bool aInBattle = mDirector != null && (mDirector.State == ArenaDirector.GameState.Fighting || mDirector.State == ArenaDirector.GameState.Intermission);
            bool aDanger = aInBattle && mPlayer != null && !mPlayer.IsDead && aHealthRatio < DangerHealthRatio;
            float aBeat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)), 6f);
            DangerAmount = aDanger ? (0.25f + 0.35f * (1f - aHealthRatio / DangerHealthRatio)) * (0.55f + 0.45f * aBeat) : 0f;
            if (mDangerVignette != null) mDangerVignette.style.opacity = DangerAmount * GameSettings.Flash;

            // 見切り(敵だけが遅い)の間: 画面の縁がうっすら紫に光り、終わりに向けて薄くなる
            SlowAmount = aInBattle && EnemyTime.IsSlowed ? 0.15f + 0.3f * EnemyTime.Remaining : 0f;
            if (mSlowVignette != null) mSlowVignette.style.opacity = SlowAmount * GameSettings.Flash;
        }

        EnemyTag EnemyTagAt(int iIndex)
        {
            while (mEnemyTags.Count <= iIndex)
            {
                var aTag = new EnemyTag { Root = Classed(new VisualElement(), "enemy-tag") };
                aTag.Alert = Classed(new Label("!"), "enemy-tag__alert");
                aTag.Skill = Classed(new Label(), "enemy-tag__skill");
                aTag.Elite = Classed(new Label(), "enemy-tag__elite");
                aTag.Weak = Classed(new Label(), "enemy-tag__weak");
                var aBar = Classed(Classed(new VisualElement(), "bar"), "enemy-tag__bar");
                aTag.Fill = Classed(new VisualElement(), "bar__fill");
                aBar.Add(aTag.Fill);
                aTag.Armor = Classed(Classed(new VisualElement(), "bar"), "enemy-tag__armor");
                aTag.ArmorFill = Classed(new VisualElement(), "bar__fill");
                aTag.Armor.Add(aTag.ArmorFill);
                aTag.Root.Add(aTag.Alert);
                aTag.Root.Add(aTag.Elite);
                aTag.Root.Add(aTag.Skill);
                aTag.Root.Add(aTag.Weak);
                aTag.Root.Add(aBar);
                aTag.Root.Add(aTag.Armor);
                mWorldLayer.Insert(0, aTag.Root);
                mEnemyTags.Add(aTag);
            }
            return mEnemyTags[iIndex];
        }

        /// <summary>ダメージの数字のふつうの大きさ(Hud.uss の .damage と同じ)</summary>
        const float mDamageFontSize = 26f;

        /// <summary>画面の縁ほど濃くなる、白い丸いぼかし(色は tint で付ける)</summary>
        static Texture2D CreateVignetteTexture()
        {
            const int aSize = 64;
            var aTexture = new Texture2D(aSize, aSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var aPixels = new Color32[aSize * aSize];
            for (int aY = 0; aY < aSize; aY++)
            {
                for (int aX = 0; aX < aSize; aX++)
                {
                    float aU = (aX + 0.5f) / aSize * 2f - 1f;
                    float aV = (aY + 0.5f) / aSize * 2f - 1f;
                    float aDistance = Mathf.Sqrt(aU * aU + aV * aV) / Mathf.Sqrt(2f);
                    float aAlpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, aDistance));
                    aPixels[aY * aSize + aX] = new Color32(255, 255, 255, (byte)(aAlpha * 255f));
                }
            }
            aTexture.SetPixels32(aPixels);
            aTexture.Apply();
            return aTexture;
        }

        Label PooledLabel(List<Label> iPool, int iIndex, string iClassName)
        {
            while (iPool.Count <= iIndex)
            {
                var aLabel = Classed(new Label(), iClassName);
                iPool.Add(aLabel);
                mWorldLayer.Add(aLabel);
            }
            Show(iPool[iIndex], true);
            return iPool[iIndex];
        }

        bool ToPanel(Vector3 iWorld, out Vector2 oPoint)
        {
            oPoint = default;
            if (mView.WorldToScreenPoint(iWorld).z <= 0f) return false;
            oPoint = RuntimePanelUtils.CameraTransformWorldToPanel(mRoot.panel, iWorld, mView);
            return true;
        }

        static void Place(VisualElement iElement, Vector2 iPoint)
        {
            iElement.style.left = iPoint.x;
            iElement.style.top = iPoint.y;
        }

        // ---------- 補助 ----------

        /// <summary>今の弱点(型を切り替える敵は、今の型の弱点)</summary>
        string WeaknessText(EnemyController iEnemy)
        {
            var aWeaknesses = iEnemy != null ? iEnemy.Weaknesses : null;
            if (aWeaknesses == null || aWeaknesses.Length == 0) return "";
            mBuilder.Clear();
            foreach (var aWeapon in aWeaknesses)
            {
                var aData = mSlots.GetWeaponData(aWeapon);
                if (aData == null) continue;
                if (mBuilder.Length > 0) mBuilder.Append(' ');
                mBuilder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(aData.Color)}>{aData.DisplayName}</color>");
            }
            return mBuilder.ToString();
        }

        Color WeaponColor(SkillData iSkill)
        {
            var aData = iSkill != null ? mSlots.GetWeaponData(iSkill.Weapon) : null;
            return aData != null ? aData.Color : Color.white;
        }

        static string DescribeShort(SkillData iSkill)
        {
            if (iSkill == null) return "";
            string aDescription = iSkill.Description ?? "";
            int aEnd = aDescription.IndexOf('。');
            if (aEnd > 0) aDescription = aDescription.Substring(0, aEnd);
            return aDescription.Length > 20 ? aDescription.Substring(0, 20) + "…" : aDescription;
        }

        static T Classed<T>(T iElement, string iClassName) where T : VisualElement
        {
            iElement.AddToClassList(iClassName);
            iElement.pickingMode = PickingMode.Ignore;
            return iElement;
        }

        /// <summary>文字が変わったときだけ入れ替える(毎フレーム同じ文字を入れて、レイアウトをやり直させない)</summary>
        static void SetText(TextElement iElement, string iText)
        {
            iText ??= "";
            if (iElement.text != iText) iElement.text = iText;
        }

        static void SetFill(VisualElement iFill, float iRatio) => iFill.style.width = Length.Percent(Mathf.Clamp01(iRatio) * 100f);

        static void Show(VisualElement iElement, bool iVisible)
        {
            var aDisplay = iVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (iElement.style.display != aDisplay) iElement.style.display = aDisplay;
        }
    }
}
