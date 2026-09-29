using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BattleFight
{
    /// <summary>
    /// スキル編成画面(UI Toolkit)。P / Select で開閉し、開いている間はゲームを止める。
    /// ・切り替え方式(ラック / プリセット)の選択
    /// ・4つのプリセットの中身(攻撃A / 攻撃B / 移動)をマウスで選ぶ
    /// 閉じると編成を保存する(WebGL ではブラウザに保存)。レイアウトは LoadoutEditor.uxml。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LoadoutEditorUI : MonoBehaviour
    {
        static readonly string[] SlotLabels = { "攻撃A", "攻撃B", "移動" };
        static readonly string[] ListNames = { "list-a", "list-b", "list-m" };
        static readonly Dictionary<SkillBehavior, string> BehaviorLabels = new Dictionary<SkillBehavior, string>
        {
            { SkillBehavior.Standard, "近接" },
            { SkillBehavior.DashStrike, "突進" },
            { SkillBehavior.ChargeCounter, "溜め・カウンター" },
            { SkillBehavior.HammerJump, "跳躍" },
            { SkillBehavior.Grapple, "ワイヤー" },
            { SkillBehavior.Pull, "引き寄せ" },
            { SkillBehavior.Projectile, "射撃" },
            { SkillBehavior.Beam, "ビーム" },
            { SkillBehavior.TargetedStrike, "遠隔の範囲攻撃" },
            { SkillBehavior.Blink, "瞬間移動" },
            { SkillBehavior.Boost, "ブースト" },
        };

        [SerializeField] SkillSlotController slots;
        [SerializeField] PlayerInputReader input;

        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        VisualElement root, editor, mode, detailStats;
        Label collectInfo, notice, nameCaption, detailEmpty, detailName, detailMeta, detailDesc, detailBonus, footer;
        Button modeRack, modePreset, close, reset;
        TextField presetName;
        readonly ScrollView[] lists = new ScrollView[3];
        readonly List<(VisualElement card, Label number, Label title, Label inUse, Label[] skills, Label bonus)> presetCards =
            new List<(VisualElement, Label, Label, Label, Label[], Label)>();
        readonly List<(VisualElement row, SkillData skill, SlotType slot)> skillRows = new List<(VisualElement, SkillData, SlotType)>();

        int editIndex;
        SkillData detailSkill;
        bool listsDirty = true;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            editor = root.Q("editor");
            mode = root.Q("mode");
            collectInfo = root.Q<Label>("collect-info");
            notice = root.Q<Label>("notice");
            nameCaption = root.Q<Label>("name-caption");
            presetName = root.Q<TextField>("preset-name");
            detailEmpty = root.Q<Label>("detail-empty");
            detailName = root.Q<Label>("detail-name");
            detailMeta = root.Q<Label>("detail-meta");
            detailDesc = root.Q<Label>("detail-desc");
            detailStats = root.Q("detail-stats");
            detailBonus = root.Q<Label>("detail-bonus");
            footer = root.Q<Label>("footer");
            modeRack = root.Q<Button>("mode-rack");
            modePreset = root.Q<Button>("mode-preset");
            close = root.Q<Button>("close");
            reset = root.Q<Button>("reset");
            for (int i = 0; i < 3; i++) lists[i] = root.Q<ScrollView>(ListNames[i]);

            presetCards.Clear();
            for (int i = 0; i < SkillSlotController.PresetCount; i++)
            {
                var instance = root.Q($"preset-card-{i + 1}");
                var card = instance.Q("card");
                int index = i;
                card.RegisterCallback<ClickEvent>(_ => SelectPreset(index));
                presetCards.Add((card, instance.Q<Label>("number"), instance.Q<Label>("title"), instance.Q<Label>("in-use"),
                    new[] { instance.Q<Label>("a"), instance.Q<Label>("b"), instance.Q<Label>("m") }, instance.Q<Label>("bonus")));
            }

            modeRack.clicked += () => { slots.SetMode(SwapMode.Rack); Refresh(); };
            modePreset.clicked += () => { slots.SetMode(SwapMode.Preset); Refresh(); };
            close.clicked += Close;
            reset.clicked += () => { slots.RestoreDefaultPresets(); Refresh(); };
            presetName.RegisterValueChangedCallback(e =>
            {
                if (IsOpen && e.newValue != slots.Presets[editIndex].name)
                {
                    slots.RenamePreset(editIndex, e.newValue);
                    RefreshPresets();
                }
            });

            editor.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnDisable()
        {
            if (IsOpen) Close();
        }

        void Update()
        {
            // 名前を入力している間は、P や Esc を文字として扱う(閉じない)
            bool typing = IsOpen && root.focusController?.focusedElement is TextField;
            bool toggle = input.Consume(PlayerAction.OpenLoadout) && !typing;
            bool escape = IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (escape && typing)
            {
                presetName.Blur();
                return;
            }
            if (toggle || escape)
            {
                if (IsOpen) Close();
                else Open();
            }
        }

        public void Open()
        {
            IsOpen = true;
            editIndex = slots.PresetIndex;
            detailSkill = null;
            listsDirty = true;
            editor.style.display = DisplayStyle.Flex;
            Refresh();
            GamePause.Set(true);
            ThirdPersonCamera.SetCursorLocked(false);
        }

        public void Close()
        {
            IsOpen = false;
            if (editor != null) editor.style.display = DisplayStyle.None;
            presetName?.Blur();
            slots.SaveLoadout();
            GamePause.Set(false);
            input.ClearBuffer();
#if !UNITY_WEBGL || UNITY_EDITOR
            ThirdPersonCamera.SetCursorLocked(true);
#endif
        }

        void SelectPreset(int index)
        {
            editIndex = index;
            presetName.Blur();
            Refresh();
        }

        // ---------- 反映 ----------

        void Refresh()
        {
            RefreshHeader();
            RefreshPresets();
            if (listsDirty) BuildLists();
            RefreshLists();
            RefreshDetail();
        }

        void RefreshHeader()
        {
            bool collecting = slots.IsCollecting;
            mode.style.display = collecting ? DisplayStyle.None : DisplayStyle.Flex;
            collectInfo.style.display = collecting ? DisplayStyle.Flex : DisplayStyle.None;
            if (collecting)
            {
                // 探索: 切り替え方式はプリセットだけ。代わりに集めたスキルの数を出す
                int total = ExplorationRegion.Active != null ? ExplorationRegion.Active.TotalSkills : ExplorationSave.TotalSkills;
                collectInfo.text = $"探索中: 集めたスキルだけで組めます   <color=#FFE08A>{ExplorationSave.UnlockedCount} / {total}</color>";
            }
            bool preset = slots.Mode == SwapMode.Preset;
            modePreset.EnableInClassList("bf-button--selected", preset);
            modeRack.EnableInClassList("bf-button--selected", !preset);
            notice.style.display = !preset && !collecting ? DisplayStyle.Flex : DisplayStyle.None;
            close.text = "閉じる [P]";
            footer.text = "クリックでスキルを入れる  /  戦闘中は 1〜4キー(十字キー)でプリセットを一括切り替え  /  閉じると自動で保存";
        }

        void RefreshPresets()
        {
            for (int i = 0; i < presetCards.Count && i < slots.Presets.Count; i++)
            {
                var (card, number, title, inUse, skills, bonus) = presetCards[i];
                var preset = slots.Presets[i];
                card.EnableInClassList("preset-card--editing", i == editIndex);
                inUse.style.display = slots.Mode == SwapMode.Preset && i == slots.PresetIndex ? DisplayStyle.Flex : DisplayStyle.None;
                number.text = (i + 1).ToString();
                title.text = preset.name;
                var a = preset.attackA;
                card.style.borderLeftColor = a != null ? WeaponColor(a) : new Color(1f, 1f, 1f, 0.2f);
                for (int s = 0; s < 3; s++)
                {
                    var skill = preset.Get((SlotType)s);
                    string name = skill != null ? $"<color=#{Hex(WeaponColor(skill))}>{skill.displayName}</color>" : "<color=#666666>(なし)</color>";
                    skills[s].text = $"<color=#8A93A3>{SlotLabels[s]}</color>  {name}";
                }
                bonus.text = BonusText(preset.Bonus);
            }
            nameCaption.text = $"プリセット{editIndex + 1} の名前";
            presetName.SetValueWithoutNotify(slots.Presets[editIndex].name);
        }

        /// <summary>スキルの一覧を作る(武器種ごとにまとめる)。手に入れていないスキルは「？？？」</summary>
        void BuildLists()
        {
            listsDirty = false;
            skillRows.Clear();
            for (int s = 0; s < 3; s++)
            {
                var slot = (SlotType)s;
                var list = lists[s];
                list.Clear();
                foreach (var weapon in slots.Weapons)
                {
                    if (weapon == null) continue;
                    var skills = new List<SkillData>();
                    foreach (var skill in slots.Database.ForSlot(slot))
                    {
                        if (skill.weapon == weapon.weapon) skills.Add(skill);
                    }
                    if (skills.Count == 0) continue;

                    var group = Classed(new VisualElement(), "skill-group");
                    var dot = Classed(new VisualElement(), "skill-group__dot");
                    dot.style.backgroundColor = weapon.color;
                    group.Add(dot);
                    var groupName = Classed(new Label(weapon.displayName), "skill-group__name");
                    groupName.style.color = weapon.color;
                    group.Add(groupName);
                    list.Add(group);

                    foreach (var skill in skills)
                    {
                        var row = Classed(new VisualElement(), "skill-row");
                        bool available = slots.IsAvailable(skill);
                        row.EnableInClassList("skill-row--locked", !available);
                        row.Add(Classed(new Label(available ? skill.displayName : "？？？"), "skill-row__name"));
                        row.Add(Classed(new Label("使用中"), "skill-row__check"));
                        if (available)
                        {
                            var captured = skill;
                            row.RegisterCallback<PointerEnterEvent>(_ => { detailSkill = captured; RefreshDetail(); });
                            row.RegisterCallback<ClickEvent>(_ =>
                            {
                                slots.AssignPresetSkill(editIndex, slot, captured);
                                detailSkill = captured;
                                Refresh();
                            });
                        }
                        list.Add(row);
                        skillRows.Add((row, skill, slot));
                    }
                }
            }
        }

        void RefreshLists()
        {
            var preset = slots.Presets[editIndex];
            foreach (var (row, skill, slot) in skillRows)
            {
                bool selected = preset.Get(slot) == skill;
                row.EnableInClassList("skill-row--selected", selected);
                row.Q<Label>(className: "skill-row__check").style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
                if (selected) row.style.borderLeftColor = WeaponColor(skill);
            }
        }

        void RefreshDetail()
        {
            var preset = slots.Presets[editIndex];
            detailBonus.text = BonusText(preset.Bonus);

            var skill = detailSkill;
            bool has = skill != null;
            detailEmpty.style.display = has ? DisplayStyle.None : DisplayStyle.Flex;
            detailName.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            detailMeta.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            detailDesc.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            detailStats.Clear();
            if (!has) return;

            var weaponData = slots.GetWeaponData(skill.weapon);
            detailName.text = skill.displayName;
            detailName.style.color = weaponData != null ? weaponData.color : Color.white;
            string behavior = BehaviorLabels.TryGetValue(skill.behavior, out var b) ? b : skill.behavior.ToString();
            detailMeta.text = $"{(weaponData != null ? weaponData.displayName : "")}  /  {SlotLabels[(int)skill.slot]}  /  {behavior}";
            detailDesc.text = skill.description;
            foreach (var line in StatLines(skill)) detailStats.Add(Classed(new Label(line), "detail__stat"));
        }

        static IEnumerable<string> StatLines(SkillData skill)
        {
            var first = skill.GetStep(0);
            int totalFrames = 0;
            float totalDamage = 0f;
            foreach (var step in skill.steps)
            {
                totalFrames += step.startupFrames + step.activeFrames + step.recoveryFrames;
                totalDamage += step.damage * (skill.behavior == SkillBehavior.Projectile ? Mathf.Max(1, skill.projectileCount) : 1);
            }
            yield return $"段数 {skill.StepCount}   発生 {first.startupFrames}F   全体 {totalFrames}F";
            yield return $"威力(合計) {totalDamage:0}   ひるみ {first.stagger:0}   アーマー削り {first.armorBreak:0}";
            if (skill.invulnerableFrames > 0) yield return $"無敵 {skill.invulnerableFrames}F";
            if (!skill.usableInAir) yield return "地上でのみ使える";
            if (skill.behavior == SkillBehavior.ChargeCounter) yield return "長押しで溜め。溜め中に攻撃を受けるとカウンター";
            if (skill.explosionRadius > 0f) yield return $"爆発半径 {skill.explosionRadius:0.#}m";
            if (skill.projectileCount > 1 && skill.behavior == SkillBehavior.Projectile) yield return $"弾数 {skill.projectileCount}";
        }

        string BonusText(WeaponBonus bonus)
        {
            var data = slots.GetWeaponData(bonus.Weapon);
            string weaponName = data != null ? data.displayName : bonus.Weapon.ToString();
            switch (bonus.Kind)
            {
                case WeaponBonusKind.Mastery:
                    return $"マスタリー({weaponName})  フィニッシャー「{(data != null && data.masteryFinisher != null ? data.masteryFinisher.displayName : "-")}」";
                case WeaponBonusKind.Synergy:
                    return $"シナジー({weaponName})  ひるみ +{Mathf.RoundToInt((data != null ? data.synergyStaggerBonus : 0f) * 100f)}%";
                case WeaponBonusKind.Arsenal:
                    return "アーセナル  スタイル獲得アップ";
                default:
                    return "(スロットが空いています)";
            }
        }

        Color WeaponColor(SkillData skill)
        {
            var data = slots.GetWeaponData(skill.weapon);
            return data != null ? data.color : Color.white;
        }

        static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        static T Classed<T>(T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
