using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
        static readonly string[] SlotLabels = { "攻撃A", "攻撃B", "移動" };
        static readonly string[] SlotNames = { "slot-a", "slot-b", "slot-m" };
        static readonly string[] KeyboardSlotKeys = { "1", "2", "3", "4" };
        static readonly string[] GamepadSlotKeys = { "←", "→", "↓", "↑" };
        static readonly Color[] RankColors =
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
        static readonly (string action, string keys)[] KeyboardGuide =
        {
            ("移動", "W・A・S・D"), ("カメラ", "マウス"), ("攻撃A", "左クリック・J"), ("攻撃B", "右クリック・K"),
            ("移動スキル", "Shift・L"), ("ジャンプ", "Space"), ("ロックオン", "Tab・中クリック"), ("フィニッシャー", "F"),
            ("スキル編成", "P"), ("もう一度 / 選択へ", "R・T"), ("切り替え", "1・2・3・4"),
        };
        static readonly (string action, string keys)[] GamepadGuide =
        {
            ("移動", "左スティック"), ("カメラ", "右スティック"), ("攻撃A", "□"), ("攻撃B", "△"),
            ("移動スキル", "○"), ("ジャンプ", "×"), ("ロックオン", "L1"), ("フィニッシャー", "R1"),
            ("スキル編成", "Select"), ("もう一度", "Start"), ("切り替え", "十字キー"),
        };
        static readonly string[] Tips =
        {
            "技の硬直中にそのスロットを切り替えると、硬直をキャンセル",
            "切り替えた直後の一撃は強化(SWAP STRIKE)",
            "同じ技の連発はスタイルが伸びない",
            "敵の頭上の「弱」の武器で攻撃すると 1.5倍",
            "切り替え → ヒットで CHAIN(最大×5、ダメージ +50%)",
            "切り替えた瞬間、新しい武器で周りを攻撃(3秒ごと)",
            "居合は長押しで溜め。ワイヤーは視界内で一番近い ◎ へ飛ぶ",
        };

        [SerializeField] SkillSlotController slots;
        [SerializeField] SkillExecutor executor;
        [SerializeField] StyleRankSystem style;
        [SerializeField] Damageable player;
        [SerializeField] LockOnSystem lockOn;
        [SerializeField] ArenaDirector director;
        [SerializeField] Camera view;

        UIDocument document;
        VisualElement root;
        VisualElement hud;
        bool showHelp = true;
        bool guideForGamepad;
        int lastRank;
        readonly SkillData[] shownSkills = new SkillData[3];
        readonly StringBuilder builder = new StringBuilder();

        EnemyController phaseEnemy;
        float phaseAnnouncedAt = -99f;

        // 要素
        Label hpValue, shards, mode, guideHint, guideTitle, guideClose, title, subtitle;
        VisualElement hpBar, hpFill, guide, guideRows, guideTips, boss, bossFill, bossArmor, bossArmorFill;
        Label bossName, bossPhase, bossWeak, rank, chainCount, chainBonus, chainHint, debugLabel, bonus, changeAttackLabel;
        VisualElement rankFill, announcements, chain, chainFill, swapStrike, swapStrikeFill, presetsRoot, changeAttack, changeAttackFill;
        Label ready, phaseTitle, phaseSub, toast;
        VisualElement phaseBanner, toastWrap, result, worldLayer;
        Label resultTitle, resultBody;
        Button resultNext, resultRetry, resultSelect;
        readonly SlotView[] slotViews = new SlotView[3];
        readonly PresetView[] presetViews = new PresetView[SkillSlotController.PresetCount];

        // 3D の位置に出すもの(使い回す)
        readonly List<EnemyTag> enemyTags = new List<EnemyTag>();
        readonly List<Label> worldLabels = new List<Label>();
        readonly List<Label> damageLabels = new List<Label>();
        readonly List<Label> announcementLabels = new List<Label>();
        Label lockMark, grappleMark;

        class SlotView
        {
            public VisualElement card;
            public Label popup, label, key, cancel, name, weapon, desc;
        }

        class PresetView
        {
            public VisualElement chip;
            public Label key, name;
        }

        class EnemyTag
        {
            public VisualElement root, fill, armor, armorFill;
            public Label weak, alert, skill;
        }

        /// <summary>操作説明を表示しているか</summary>
        public bool IsHelpShown => showHelp;

        /// <summary>最後に触ったのがゲームパッドか(操作説明とキー表示をそのデバイスのものにする)</summary>
        public bool UsingGamepad { get; private set; }

        /// <summary>スロットのパネルに今出ているスキル(実際に使われるもの。プリセット方式ではプリセットの中身)</summary>
        public SkillData ShownSkill(SlotType slot) => shownSkills[(int)slot];

        void OnEnable()
        {
            document = GetComponent<UIDocument>();
            root = document.rootVisualElement;
            Bind();

            slots.SlotChanged += OnSlotChanged;
            EnemyController.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            slots.SlotChanged -= OnSlotChanged;
            EnemyController.PhaseChanged -= OnPhaseChanged;
        }

        void Bind()
        {
            hud = root.Q("hud");
            hpValue = root.Q<Label>("hp-value");
            hpFill = root.Q("hp-fill");
            hpBar = hpFill.parent;
            shards = root.Q<Label>("shards");
            mode = root.Q<Label>("mode");
            guideHint = root.Q<Label>("guide-hint");
            guide = root.Q("guide");
            guideTitle = root.Q<Label>("guide-title");
            guideRows = root.Q("guide-rows");
            guideTips = root.Q("guide-tips");
            guideClose = root.Q<Label>("guide-close");
            title = root.Q<Label>("title");
            subtitle = root.Q<Label>("subtitle");
            boss = root.Q("boss");
            bossName = root.Q<Label>("boss-name");
            bossPhase = root.Q<Label>("boss-phase");
            bossWeak = root.Q<Label>("boss-weak");
            bossFill = root.Q("boss-fill");
            bossArmor = root.Q("boss-armor");
            bossArmorFill = root.Q("boss-armor-fill");
            rank = root.Q<Label>("rank");
            rankFill = root.Q("rank-fill");
            announcements = root.Q("announcements");
            chain = root.Q("chain");
            chainCount = root.Q<Label>("chain-count");
            chainBonus = root.Q<Label>("chain-bonus");
            chainFill = root.Q("chain-fill");
            chainHint = root.Q<Label>("chain-hint");
            debugLabel = root.Q<Label>("debug");
            swapStrike = root.Q("swap-strike");
            swapStrikeFill = root.Q("swap-strike-fill");
            bonus = root.Q<Label>("bonus");
            presetsRoot = root.Q("presets");
            changeAttack = root.Q("change-attack");
            changeAttackLabel = root.Q<Label>("change-attack-label");
            changeAttackFill = root.Q("change-attack-fill");
            ready = root.Q<Label>("ready");
            phaseBanner = root.Q("phase-banner");
            phaseTitle = root.Q<Label>("phase-title");
            phaseSub = root.Q<Label>("phase-sub");
            toastWrap = root.Q("toast-wrap");
            toast = root.Q<Label>("toast");
            result = root.Q("result");
            resultTitle = root.Q<Label>("result-title");
            resultBody = root.Q<Label>("result-body");
            resultNext = root.Q<Button>("result-next");
            resultRetry = root.Q<Button>("result-retry");
            resultSelect = root.Q<Button>("result-select");
            worldLayer = root.Q("world-layer");

            for (int i = 0; i < 3; i++)
            {
                var instance = root.Q(SlotNames[i]);
                slotViews[i] = new SlotView
                {
                    card = instance.Q("card"),
                    popup = instance.Q<Label>("popup"),
                    label = instance.Q<Label>("label"),
                    key = instance.Q<Label>("key"),
                    cancel = instance.Q<Label>("cancel"),
                    name = instance.Q<Label>("name"),
                    weapon = instance.Q<Label>("weapon"),
                    desc = instance.Q<Label>("desc"),
                };
                slotViews[i].label.text = SlotLabels[i];
            }
            for (int i = 0; i < presetViews.Length; i++)
            {
                var instance = root.Q($"preset-{i + 1}");
                presetViews[i] = new PresetView { chip = instance.Q("chip"), key = instance.Q<Label>("key"), name = instance.Q<Label>("name") };
            }

            resultNext.clicked += () => director.NextStage();
            resultRetry.clicked += () => director.Retry();
            resultSelect.clicked += () => director.BackToStageSelect();

            lockMark = new Label("◇") { pickingMode = PickingMode.Ignore };
            lockMark.AddToClassList("world-mark");
            lockMark.AddToClassList("world-mark--lock");
            grappleMark = new Label("◎") { pickingMode = PickingMode.Ignore };
            grappleMark.AddToClassList("world-mark");
            grappleMark.AddToClassList("world-mark--grapple");
            var grappleHint = new Label { name = "grapple-hint", pickingMode = PickingMode.Ignore };
            grappleHint.AddToClassList("world-mark__hint");
            grappleMark.Add(grappleHint);
            worldLayer.Add(lockMark);
            worldLayer.Add(grappleMark);

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
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if ((keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 4f)))
            {
                UsingGamepad = false;
            }
        }

        static bool GamepadTouched(Gamepad gamepad)
        {
            if (gamepad == null) return false;
            if (gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f || gamepad.rightStick.ReadValue().sqrMagnitude > 0.25f) return true;
            foreach (var control in gamepad.allControls)
            {
                if (control is ButtonControl button && !button.synthetic && button.wasPressedThisFrame) return true;
            }
            return false;
        }

        static bool HelpTogglePressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame)) return true;
            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightStickButton.wasPressedThisFrame;
        }

        string SlotKey(int index) => (UsingGamepad ? GamepadSlotKeys : KeyboardSlotKeys)[index];

        void OnPhaseChanged(EnemyController enemy)
        {
            phaseEnemy = enemy;
            phaseAnnouncedAt = Time.unscaledTime;
        }

        void OnSlotChanged(SlotType slot)
        {
            int i = (int)slot;
            // 表示中のスキルはすぐ更新する(編成を変えた直後に ShownSkill を読んでも古くならないように)
            shownSkills[i] = slots.GetCurrent(slot);
            // 切り替えた瞬間だけ大きくし、新しいスキル名を上に出す(戻りは USS の transition)
            var view = slotViews[i];
            if (view == null) return;
            var skill = slots.GetCurrent(slot);
            view.card.AddToClassList("slot--swapped");
            view.card.schedule.Execute(() => view.card.RemoveFromClassList("slot--swapped")).StartingIn(60);
            if (skill != null)
            {
                view.popup.text = $"⇒ {skill.displayName}";
                view.popup.style.color = WeaponColor(skill);
                view.popup.AddToClassList("slot__popup--show");
                view.popup.schedule.Execute(() => view.popup.RemoveFromClassList("slot__popup--show")).StartingIn(380);
            }
        }

        void Update()
        {
            TrackLastDevice();
            if (HelpTogglePressed()) showHelp = !showHelp;
            Refresh();
        }

        // ---------- 反映 ----------

        void Refresh()
        {
            if (root == null) return;
            bool visible = director.State != ArenaDirector.GameState.StageSelect && !LoadoutEditorUI.IsOpen;
            Show(hud, visible);
            // スロットの表示はテストや他の画面からも読むので、HUD を隠していても更新する
            RefreshSlots();
            if (!visible) return;

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
            float ratio = player.MaxHealth > 0f ? player.Health / player.MaxHealth : 0f;
            SetText(hpValue, $"{Mathf.CeilToInt(player.Health)} <color=#8A93A3>/ {Mathf.CeilToInt(player.MaxHealth)}</color>");
            SetFill(hpFill, ratio);
            hpBar.EnableInClassList("low", ratio < 0.3f);
            SetText(shards, $"刻片 {director.Shards}");
            string modeName = slots.Mode == SwapMode.Preset ? "プリセット方式" : "ラック方式";
            SetText(mode, $"{modeName}  <color=#FFFFFF>[{(UsingGamepad ? "Select" : "P")}]</color> 編成");
        }

        void RefreshGuide()
        {
            if (guideForGamepad != UsingGamepad) BuildGuide(UsingGamepad);
            Show(guide, showHelp);
            Show(guideHint, !showHelp);
            SetText(guideHint, UsingGamepad ? "[R3] 操作説明" : "[0] 操作説明");
        }

        void BuildGuide(bool gamepad)
        {
            guideForGamepad = gamepad;
            guideTitle.text = gamepad ? "操作(ゲームパッド)" : "操作(キーボード・マウス)";
            guideRows.Clear();
            foreach (var (action, keys) in gamepad ? GamepadGuide : KeyboardGuide)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("guide__row");
                row.Add(Classed(new Label(action), "guide__action"));
                var keyRow = Classed(new VisualElement(), "guide__keys");
                foreach (var key in keys.Split('・')) keyRow.Add(Classed(new Label(key), "key"));
                row.Add(keyRow);
                if (action == "切り替え")
                {
                    // 補足があるので1行まるごと使う
                    row.AddToClassList("guide__row--wide");
                    row.Add(Classed(new Label(gamepad ? "ラック: ←/→/↓ で各スロット  プリセット: 十字キーで一括" : "ラック: 1/2/3 で各スロット  プリセット: 1〜4 で一括"), "guide__note"));
                }
                guideRows.Add(row);
            }
            guideTips.Clear();
            foreach (var tip in Tips) guideTips.Add(Classed(new Label("・" + tip), "guide__tip"));
            guideClose.text = gamepad ? "[R3] で閉じる" : "[0] で閉じる";
        }

        void RefreshHeader()
        {
            var stage = director.CurrentStage;
            if (stage == null)
            {
                SetText(title, "");
                Show(subtitle, false);
                return;
            }

            var region = ExplorationRegion.Active;
            if (stage.kind == StageKind.Exploration && region != null)
            {
                // 探索: 今いる部屋と、集めたスキルの数(ボス戦中は体力と重なるので出さない)
                string room = region.CurrentRoomName();
                SetText(title, room.Length > 0 ? $"{region.RegionName}  <color=#8A93A3>-</color>  {room}" : region.RegionName);
                SetText(subtitle, $"集めたスキル  {ExplorationSave.UnlockedCount} / {region.TotalSkills}");
                Show(subtitle, director.Boss == null);
                return;
            }

            string text;
            if (stage.kind == StageKind.Training || director.WaveNumber <= 0) text = stage.displayName;
            else if (stage.kind == StageKind.Endless) text = $"{stage.displayName}   <color=#8A93A3>{director.WaveLabel}</color> {director.WaveNumber}";
            else text = $"{stage.displayName}   <color=#8A93A3>{director.WaveLabel}</color>  {director.WaveNumber} / {director.WaveCount}";
            SetText(title, text);
            Show(subtitle, false);
        }

        void RefreshBoss()
        {
            var enemy = director.Boss;
            Show(boss, enemy != null);
            if (enemy == null) return;
            var damageable = enemy.Damageable;
            SetText(bossName, enemy.Profile.displayName);
            Show(bossPhase, enemy.IsPhase2);
            string weakness = WeaknessText(enemy);
            var set = enemy.CurrentSkillSet;
            if (set != null && enemy.Profile.skillSets.Count > 1)
            {
                // 型を切り替えるボスは、今の型も出す
                weakness = $"<color=#{ColorUtility.ToHtmlStringRGB(set.color)}>{set.name}</color>   " + weakness;
            }
            SetText(bossWeak, weakness.Length > 0 ? $"<color=#8A93A3>弱点</color> {weakness}" : "");
            SetFill(bossFill, damageable.Health / damageable.MaxHealth);
            Show(bossArmor, damageable.MaxArmor > 0f);
            if (damageable.MaxArmor > 0f) SetFill(bossArmorFill, damageable.Armor / damageable.MaxArmor);
        }

        void RefreshStyle()
        {
            var meter = style.Meter;
            var color = RankColors[Mathf.Min(meter.RankIndex, RankColors.Length - 1)];
            SetText(rank, meter.RankName);
            rank.style.color = color;
            rankFill.style.backgroundColor = color;
            SetFill(rankFill, meter.RankProgress);
            if (meter.RankIndex > lastRank)
            {
                rank.AddToClassList("pulse");
                rank.schedule.Execute(() => rank.RemoveFromClassList("pulse")).StartingIn(40);
            }
            lastRank = meter.RankIndex;

            // お知らせ(新しいものを上に)
            int count = style.Announcements.Count;
            while (announcementLabels.Count < count)
            {
                var label = Classed(new Label(), "announcement");
                announcementLabels.Add(label);
                announcements.Add(label);
            }
            for (int i = 0; i < announcementLabels.Count; i++)
            {
                var label = announcementLabels[i];
                if (i >= count)
                {
                    Show(label, false);
                    continue;
                }
                var announcement = style.Announcements[count - 1 - i];
                float age = Time.unscaledTime - announcement.time;
                Show(label, true);
                SetText(label, announcement.text);
                var c = announcement.color;
                c.a = Mathf.Clamp01((1f - age / StyleRankSystem.AnnouncementLifetime) * 1.5f);
                label.style.color = c;
            }

            var chainMeter = executor.SwapChain;
            bool chaining = chainMeter.Chain > 0;
            Show(chain, chaining);
            Show(chainHint, !chaining);
            if (chaining)
            {
                int bonusPercent = Mathf.RoundToInt((chainMeter.DamageMultiplier - 1f) * 100f);
                SetText(chainCount, $"CHAIN ×{chainMeter.Chain}{(chainMeter.Chain >= chainMeter.MaxChain ? "  MAX" : "")}");
                SetText(chainBonus, $"ダメージ +{bonusPercent}%");
                SetFill(chainFill, chainMeter.Remaining(Time.time));
            }
            SetText(debugLabel, executor.DebugLabel);
        }

        void RefreshSlots()
        {
            bool rackMode = slots.Mode == SwapMode.Rack;
            for (int i = 0; i < 3; i++)
            {
                var slot = (SlotType)i;
                var view = slotViews[i];
                var current = slots.GetCurrent(slot);
                shownSkills[i] = current;
                if (view == null) continue;

                var weaponData = current != null ? slots.GetWeaponData(current.weapon) : null;
                var weaponColor = weaponData != null ? weaponData.color : new Color(1f, 1f, 1f, 0.25f);
                bool active = executor.Current != null && executor.Current == current && !executor.IsFinisherActive;
                bool cancelReady = rackMode && executor.CanSwapCancelNow(slot);

                view.card.style.borderTopColor = weaponColor;
                view.card.EnableInClassList("slot--active", active);
                view.card.EnableInClassList("slot--cancel", cancelReady && Mathf.PingPong(Time.unscaledTime * 8f, 1f) > 0.4f);
                Show(view.cancel, cancelReady);
                Show(view.key, rackMode);
                SetText(view.key, SlotKey(i));

                SetText(view.name, current != null ? current.displayName : "(なし)");
                view.name.style.color = current != null ? weaponColor : new Color(1f, 1f, 1f, 0.35f);
                SetText(view.weapon, weaponData != null ? weaponData.displayName : "");
                SetText(view.desc, rackMode ? RackLine(slots.GetRack(slot)) : DescribeShort(current));
            }
        }

        string RackLine(SkillRack rack)
        {
            builder.Clear();
            for (int j = 0; j < rack.Count; j++)
            {
                var skill = rack.Skills[j];
                string hex = ColorUtility.ToHtmlStringRGB(WeaponColor(skill));
                if (j > 0) builder.Append("  ");
                if (j == rack.CurrentIndex) builder.Append($"<color=#{hex}>■{skill.displayName}</color>");
                else if (skill == rack.Next) builder.Append($"<color=#{hex}>▶{skill.displayName}</color>");
                else builder.Append($"<color=#777777>{skill.displayName}</color>");
            }
            return builder.ToString();
        }

        void RefreshBottom()
        {
            // スワップストライク
            bool strike = executor.SwapStrikeReady;
            Show(swapStrike, strike);
            if (strike) SetFill(swapStrikeFill, executor.SwapStrikeRemaining);

            // 武器種ボーナス
            var weaponBonus = slots.Bonus;
            var data = slots.GetWeaponData(weaponBonus.Weapon);
            string weaponName = data != null ? data.displayName : weaponBonus.Weapon.ToString();
            string text = "";
            Color color = Color.white;
            switch (weaponBonus.Kind)
            {
                case WeaponBonusKind.Mastery:
                    var finisher = slots.MasteryFinisher;
                    text = $"マスタリー({weaponName})   <color=#FFFFFF>[{(UsingGamepad ? "R1" : "F")}]</color> フィニッシャー「{(finisher != null ? finisher.displayName : "-")}」";
                    color = data != null ? data.color : Color.white;
                    break;
                case WeaponBonusKind.Synergy:
                    text = $"シナジー({weaponName})   ひるみ +{Mathf.RoundToInt((data != null ? data.synergyStaggerBonus : 0f) * 100f)}%";
                    color = data != null ? Color.Lerp(data.color, Color.white, 0.3f) : Color.white;
                    break;
                case WeaponBonusKind.Arsenal:
                    text = $"アーセナル   スタイル獲得 x{style.Config.arsenalMultiplier:0.##}";
                    color = new Color(0.9f, 0.9f, 1f);
                    break;
            }
            Show(bonus, text.Length > 0);
            SetText(bonus, text);
            bonus.style.color = color;

            // プリセット
            bool presetMode = slots.Mode == SwapMode.Preset;
            Show(presetsRoot, presetMode);
            if (presetMode)
            {
                for (int i = 0; i < presetViews.Length; i++)
                {
                    var view = presetViews[i];
                    bool exists = i < slots.Presets.Count;
                    Show(view.chip, exists);
                    if (!exists) continue;
                    var preset = slots.Presets[i];
                    var a = preset.attackA;
                    view.chip.style.borderLeftColor = a != null ? WeaponColor(a) : new Color(1f, 1f, 1f, 0.2f);
                    view.chip.EnableInClassList("preset--current", i == slots.PresetIndex);
                    view.chip.EnableInClassList("preset--empty", preset.IsEmpty);
                    view.chip.EnableInClassList("preset--cancel",
                        i != slots.PresetIndex && executor.CanPresetCancel(i) && Mathf.PingPong(Time.unscaledTime * 8f, 1f) > 0.4f);
                    SetText(view.key, SlotKey(i));
                    SetText(view.name, preset.name);
                }
            }

            // チェンジアタック
            bool readyNow = executor.ChangeAttackReady;
            changeAttack.EnableInClassList("ready", readyNow);
            SetText(changeAttackLabel, readyNow ? "CHANGE ATTACK 準備OK" : "CHANGE ATTACK");
            SetFill(changeAttackFill, executor.ChangeAttackCharge);
        }

        void RefreshCenter()
        {
            var state = director.State;
            Show(ready, state == ArenaDirector.GameState.Starting);

            // ボスの第二形態の告知(2秒)
            float sincePhase = Time.unscaledTime - phaseAnnouncedAt;
            bool phase = phaseEnemy != null && sincePhase < 2f && state != ArenaDirector.GameState.Cleared && state != ArenaDirector.GameState.GameOver;
            Show(phaseBanner, phase);
            if (phase)
            {
                var c = phaseEnemy.Profile.telegraphColor;
                c.a = Mathf.Clamp01((2f - sincePhase) / 0.5f);
                SetText(phaseTitle, $"{phaseEnemy.Profile.displayName}  第二形態");
                SetText(phaseSub, phaseEnemy.Profile.phase2Message);
                phaseTitle.style.color = c;
                phaseSub.style.color = c;
            }

            // 探索のお知らせ(4秒で消える)
            var region = ExplorationRegion.Active;
            float sinceToast = region != null ? Time.unscaledTime - region.MessageTime : 99f;
            bool showToast = region != null && !string.IsNullOrEmpty(region.Message) && sinceToast < 4f && !phase;
            Show(toastWrap, showToast);
            if (showToast)
            {
                SetText(toast, region.Message);
                toast.style.color = region.MessageColor;
                toast.style.opacity = Mathf.Clamp01((4f - sinceToast) / 0.6f);
            }

            RefreshResult(state);
        }

        void RefreshResult(ArenaDirector.GameState state)
        {
            bool cleared = state == ArenaDirector.GameState.Cleared;
            bool over = state == ArenaDirector.GameState.GameOver;
            Show(result, cleared || over);
            if (!cleared && !over) return;

            if (cleared && director.IsExploring && ExplorationRegion.Active != null)
            {
                SetText(resultTitle, "探索クリア");
                SetText(resultBody, $"集めたスキル {ExplorationSave.UnlockedCount} / {ExplorationRegion.Active.TotalSkills}   タイム {director.ElapsedTime:0.0}秒\n続きからは、マップを自由に回れます");
                resultTitle.style.color = new Color(1f, 0.9f, 0.4f);
            }
            else if (cleared)
            {
                string best = style.Config.rankNames[Mathf.Min(style.HighestRank, style.Config.rankNames.Length - 1)];
                string record = director.NewRecord ? "   <color=#FFE066>NEW RECORD!</color>" : "";
                SetText(resultTitle, "STAGE CLEAR");
                SetText(resultBody, $"タイム {director.ElapsedTime:0.0}秒   最高ランク {best}   刻片 {director.Shards}{record}");
                resultTitle.style.color = new Color(1f, 0.9f, 0.4f);
            }
            else
            {
                bool endless = director.CurrentStage != null && director.CurrentStage.kind == StageKind.Endless;
                SetText(resultTitle, "GAME OVER");
                SetText(resultBody, endless ? $"到達 WAVE {director.WaveNumber}{(director.NewRecord ? "   <color=#FFE066>NEW RECORD!</color>" : "")}" : "");
                resultTitle.style.color = new Color(1f, 0.35f, 0.35f);
            }
            Show(resultBody, resultBody.text.Length > 0);

            Show(resultNext, cleared && director.HasNextStage);
            SetText(resultNext, "次のステージ [N]");
            SetText(resultRetry, !director.IsExploring ? "もう一度 [R]" : over ? "祭壇から再開 [R]" : "続きから [R]");
            SetText(resultSelect, "ステージ選択 [T]");
        }

        // ---------- 3D の位置に出すもの ----------

        void RefreshWorld()
        {
            if (view == null || root.panel == null) return;

            // 敵の体力・弱点・予備動作の「!」(ボスは上の体力バーに出すので除く)
            int used = 0;
            var attackA = slots.GetCurrent(SlotType.AttackA);
            foreach (var enemy in EnemyController.Active)
            {
                if (enemy.IsDead || enemy.Profile.isBoss) continue;
                if (!ToPanel(enemy.transform.position + Vector3.up * (enemy.Height + 0.4f), out var point)) continue;
                var tag = EnemyTagAt(used++);
                Show(tag.root, true);
                Place(tag.root, point);
                var damageable = enemy.Damageable;
                SetFill(tag.fill, damageable.Health / damageable.MaxHealth);
                Show(tag.armor, damageable.MaxArmor > 0f);
                if (damageable.MaxArmor > 0f) SetFill(tag.armorFill, damageable.Armor / damageable.MaxArmor);
                string weakness = WeaknessText(enemy);
                bool weakNow = attackA != null && enemy.IsWeakTo(attackA.weapon);
                Show(tag.weak, weakness.Length > 0);
                SetText(tag.weak, (weakNow ? "弱点 " : "弱 ") + weakness);
                tag.weak.EnableInClassList("now", weakNow);
                Show(tag.alert, enemy.IsTelegraphing);
                tag.alert.style.color = enemy.Profile.telegraphColor;

                // プレイヤーと同じスキルを使うときは、予備動作中に技の名前を出す
                var skill = enemy.IsTelegraphing && enemy.CurrentAttack != null ? enemy.CurrentAttack.sourceSkill : null;
                var set = enemy.CurrentSkillSet;
                bool swapped = set != null && Time.time - enemy.SkillSetChangedAt < 1.5f;
                Show(tag.skill, skill != null || swapped);
                if (skill != null)
                {
                    SetText(tag.skill, skill.displayName);
                    tag.skill.style.color = WeaponColor(skill);
                }
                else if (swapped)
                {
                    SetText(tag.skill, $"⇄ {set.name}");
                    tag.skill.style.color = set.color;
                }
            }
            for (int i = used; i < enemyTags.Count; i++) Show(enemyTags[i].root, false);

            // 探索の目印(近いものだけ)
            used = 0;
            var playerPosition = player.transform.position;
            foreach (var worldLabel in WorldLabel.All)
            {
                if (worldLabel == null || string.IsNullOrEmpty(worldLabel.text)) continue;
                var position = worldLabel.Position;
                float distance = Vector3.Distance(position, playerPosition);
                if (distance > worldLabel.showDistance || !ToPanel(position, out var point)) continue;
                var label = PooledLabel(worldLabels, used++, "world-label");
                Place(label, point);
                SetText(label, worldLabel.text);
                var c = worldLabel.color;
                c.a *= Mathf.Clamp01((worldLabel.showDistance - distance) / 3f);
                label.style.color = c;
            }
            for (int i = used; i < worldLabels.Count; i++) Show(worldLabels[i], false);

            // ワイヤーで飛ぶ先
            var grapple = executor.GrapplePreview;
            Vector2 grapplePoint = default;
            bool showGrapple = grapple != null && ToPanel(grapple.transform.position, out grapplePoint);
            Show(grappleMark, showGrapple);
            if (showGrapple)
            {
                Place(grappleMark, grapplePoint);
                float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f);
                grappleMark.style.scale = new Scale(new Vector3(pulse, pulse, 1f));
                SetText(grappleMark.Q<Label>("grapple-hint"), UsingGamepad ? "○" : "Shift / L");
            }

            // ロックオン
            var target = lockOn.Target;
            Vector2 lockPoint = default;
            bool showLock = target != null && ToPanel(target.CenterPoint, out lockPoint);
            Show(lockMark, showLock);
            if (showLock) Place(lockMark, lockPoint);

            // ダメージの数字
            used = 0;
            if (CombatFeedback.Instance != null)
            {
                foreach (var number in CombatFeedback.Instance.DamageNumbers)
                {
                    float age = Time.unscaledTime - number.time;
                    if (!ToPanel(number.position + Vector3.up * age * 1.2f, out var point)) continue;
                    var label = PooledLabel(damageLabels, used++, "damage");
                    Place(label, point);
                    SetText(label, number.text);
                    var c = number.color;
                    c.a = 1f - age / CombatFeedback.DamageNumberLifetime;
                    label.style.color = c;
                }
            }
            for (int i = used; i < damageLabels.Count; i++) Show(damageLabels[i], false);
        }

        EnemyTag EnemyTagAt(int index)
        {
            while (enemyTags.Count <= index)
            {
                var tag = new EnemyTag { root = Classed(new VisualElement(), "enemy-tag") };
                tag.alert = Classed(new Label("!"), "enemy-tag__alert");
                tag.skill = Classed(new Label(), "enemy-tag__skill");
                tag.weak = Classed(new Label(), "enemy-tag__weak");
                var bar = Classed(Classed(new VisualElement(), "bar"), "enemy-tag__bar");
                tag.fill = Classed(new VisualElement(), "bar__fill");
                bar.Add(tag.fill);
                tag.armor = Classed(Classed(new VisualElement(), "bar"), "enemy-tag__armor");
                tag.armorFill = Classed(new VisualElement(), "bar__fill");
                tag.armor.Add(tag.armorFill);
                tag.root.Add(tag.alert);
                tag.root.Add(tag.skill);
                tag.root.Add(tag.weak);
                tag.root.Add(bar);
                tag.root.Add(tag.armor);
                worldLayer.Insert(0, tag.root);
                enemyTags.Add(tag);
            }
            return enemyTags[index];
        }

        Label PooledLabel(List<Label> pool, int index, string className)
        {
            while (pool.Count <= index)
            {
                var label = Classed(new Label(), className);
                pool.Add(label);
                worldLayer.Add(label);
            }
            Show(pool[index], true);
            return pool[index];
        }

        bool ToPanel(Vector3 world, out Vector2 point)
        {
            point = default;
            if (view.WorldToScreenPoint(world).z <= 0f) return false;
            point = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, world, view);
            return true;
        }

        static void Place(VisualElement element, Vector2 point)
        {
            element.style.left = point.x;
            element.style.top = point.y;
        }

        // ---------- 補助 ----------

        /// <summary>今の弱点(型を切り替える敵は、今の型の弱点)</summary>
        string WeaknessText(EnemyController enemy)
        {
            var weaknesses = enemy != null ? enemy.Weaknesses : null;
            if (weaknesses == null || weaknesses.Length == 0) return "";
            builder.Clear();
            foreach (var weapon in weaknesses)
            {
                var data = slots.GetWeaponData(weapon);
                if (data == null) continue;
                if (builder.Length > 0) builder.Append(' ');
                builder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(data.color)}>{data.displayName}</color>");
            }
            return builder.ToString();
        }

        Color WeaponColor(SkillData skill)
        {
            var data = skill != null ? slots.GetWeaponData(skill.weapon) : null;
            return data != null ? data.color : Color.white;
        }

        static string DescribeShort(SkillData skill)
        {
            if (skill == null) return "";
            string description = skill.description ?? "";
            int end = description.IndexOf('。');
            if (end > 0) description = description.Substring(0, end);
            return description.Length > 20 ? description.Substring(0, 20) + "…" : description;
        }

        static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            element.pickingMode = PickingMode.Ignore;
            return element;
        }

        /// <summary>文字が変わったときだけ入れ替える(毎フレーム同じ文字を入れて、レイアウトをやり直させない)</summary>
        static void SetText(TextElement element, string text)
        {
            text ??= "";
            if (element.text != text) element.text = text;
        }

        static void SetFill(VisualElement fill, float ratio) => fill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);

        static void Show(VisualElement element, bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display != display) element.style.display = display;
        }
    }
}
