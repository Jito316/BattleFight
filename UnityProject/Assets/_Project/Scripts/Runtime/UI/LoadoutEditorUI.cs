using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight
{
    /// <summary>
    /// スキル編成画面(IMGUI)。P / Select で開閉し、開いている間はゲームを止める。
    /// ・切り替え方式(ラック / プリセット)の選択
    /// ・4つのプリセットの中身(攻撃A / 攻撃B / 移動)をマウスで選ぶ
    /// 閉じると編成を保存する(WebGL ではブラウザに保存)。
    /// </summary>
    public class LoadoutEditorUI : MonoBehaviour
    {
        const float RefHeight = 1080f;
        const float RowHeight = 40f;
        const float HeaderHeight = 30f;

        static readonly string[] SlotLabels = { "攻撃A", "攻撃B", "移動" };
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
        [SerializeField] Font font;

        public static bool IsOpen { get; private set; }

        readonly Vector2[] scroll = new Vector2[3];
        int editIndex;
        SkillData detailSkill;
        GUIStyle label;
        GUIStyle wrapLabel;
        GUIStyle textField;
        GUIStyle buttonText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        void OnDisable()
        {
            if (IsOpen) Close();
        }

        void Update()
        {
            bool toggle = input.Consume(PlayerAction.OpenLoadout);
            bool escape = IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
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
            GamePause.Set(true);
            ThirdPersonCamera.SetCursorLocked(false);
        }

        public void Close()
        {
            IsOpen = false;
            slots.SaveLoadout();
            GamePause.Set(false);
            input.ClearBuffer();
#if !UNITY_WEBGL || UNITY_EDITOR
            ThirdPersonCamera.SetCursorLocked(true);
#endif
        }

        // ---------- 描画 ----------

        void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = false, fontSize = 20 };
            wrapLabel = new GUIStyle(label) { wordWrap = true, fontSize = 18 };
            textField = new GUIStyle(GUI.skin.textField) { fontSize = 22 };
            buttonText = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            if (font != null)
            {
                label.font = font;
                wrapLabel.font = font;
                textField.font = font;
                buttonText.font = font;
            }
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            EnsureStyles();

            float scale = Screen.height / RefHeight;
            float width = Screen.width / scale;
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Box(new Rect(0, 0, width, RefHeight), new Color(0f, 0f, 0f, 0.55f));
            var panel = new Rect(50, 40, width - 100, RefHeight - 80);
            Box(panel, new Color(0.08f, 0.09f, 0.12f, 0.96f));

            DrawHeader(panel);

            const float presetWidth = 300f;
            const float detailWidth = 360f;
            float top = panel.y + 90f;
            float bottom = panel.yMax - 50f;
            var presetArea = new Rect(panel.x + 20, top, presetWidth, bottom - top);
            var detailArea = new Rect(panel.xMax - 20 - detailWidth, top, detailWidth, bottom - top);
            var listArea = new Rect(presetArea.xMax + 20, top, detailArea.x - presetArea.xMax - 40, bottom - top);

            DrawPresets(presetArea);
            DrawSkillLists(listArea);
            DrawDetail(detailArea);

            Text(new Rect(panel.x + 20, panel.yMax - 40, panel.width - 40, 30),
                "クリックでスキルを入れる  /  戦闘中は 1〜4キー(十字キー)でプリセットを一括切り替え  /  閉じると自動で保存",
                18, new Color(1f, 1f, 1f, 0.6f));

            GUI.matrix = previousMatrix;
        }

        void DrawHeader(Rect panel)
        {
            Text(new Rect(panel.x + 24, panel.y + 18, 400, 50), "<b>スキル編成</b>", 36, Color.white);

            float x = panel.xMax - 24;
            if (Button(new Rect(x -= 200, panel.y + 20, 200, 48), "閉じる [P]", new Color(0.3f, 0.3f, 0.35f))) Close();

            x -= 30;
            if (slots.IsCollecting)
            {
                // 探索: 切り替え方式はプリセットだけ。代わりに集めたスキルの数を出す
                int total = ExplorationRegion.Active != null ? ExplorationRegion.Active.TotalSkills : ExplorationSave.TotalSkills;
                Text(new Rect(x - 700, panel.y + 30, 700, 30),
                    $"探索中: 集めたスキルだけで組めます   <color=#FFE08A>{ExplorationSave.UnlockedCount} / {total}</color>", 20,
                    new Color(1f, 1f, 1f, 0.8f), TextAnchor.UpperRight);
                return;
            }
            bool preset = slots.Mode == SwapMode.Preset;
            if (Button(new Rect(x -= 220, panel.y + 20, 220, 48), "プリセット方式",
                    preset ? new Color(0.2f, 0.55f, 0.9f) : new Color(0.22f, 0.22f, 0.26f)))
            {
                slots.SetMode(SwapMode.Preset);
            }
            if (Button(new Rect(x -= 230, panel.y + 20, 220, 48), "ラック方式",
                    !preset ? new Color(0.2f, 0.55f, 0.9f) : new Color(0.22f, 0.22f, 0.26f)))
            {
                slots.SetMode(SwapMode.Rack);
            }
            Text(new Rect(x - 220, panel.y + 30, 210, 30), "戦闘中の切り替え方式", 18, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperRight);

            if (!preset)
            {
                Text(new Rect(panel.x + 24, panel.y + 62, panel.width - 48, 26),
                    "※ 今はラック方式です。ここで編集したプリセットは「プリセット方式」にすると使えます", 18, new Color(1f, 0.85f, 0.4f));
            }
        }

        void DrawPresets(Rect area)
        {
            Text(new Rect(area.x, area.y - 4, area.width, 30), "プリセット", 22, new Color(1f, 1f, 1f, 0.8f));
            float y = area.y + 30f;
            const float cardHeight = 152f;

            for (int i = 0; i < slots.Presets.Count; i++)
            {
                var preset = slots.Presets[i];
                var card = new Rect(area.x, y, area.width, cardHeight);
                bool editing = i == editIndex;
                bool inUse = slots.Mode == SwapMode.Preset && i == slots.PresetIndex;

                Box(card, editing ? new Color(0.18f, 0.24f, 0.34f) : new Color(0.13f, 0.14f, 0.18f));
                if (editing) Outline(card, new Color(0.4f, 0.75f, 1f), 3f);
                if (GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    editIndex = i;
                    GUI.FocusControl(null);
                }

                Text(new Rect(card.x + 14, card.y + 8, card.width - 28, 30),
                    $"<b>{i + 1}</b>  {preset.name}{(inUse ? "   <color=#7FD4FF>使用中</color>" : "")}", 22, Color.white);
                for (int s = 0; s < 3; s++)
                {
                    var skill = preset.Get((SlotType)s);
                    string name = skill != null ? $"<color=#{Hex(WeaponColor(skill))}>{skill.displayName}</color>" : "<color=#777777>(なし)</color>";
                    Text(new Rect(card.x + 14, card.y + 42 + s * 26, card.width - 28, 26),
                        $"<color=#999999>{SlotLabels[s]}</color>  {name}", 18, Color.white);
                }
                Text(new Rect(card.x + 14, card.y + 122, card.width - 28, 26), BonusText(preset.Bonus), 16, new Color(1f, 0.9f, 0.5f));
                y += cardHeight + 12f;
            }

            if (Button(new Rect(area.x, y + 4, area.width, 44), "初期のプリセットに戻す", new Color(0.35f, 0.2f, 0.2f)))
            {
                slots.RestoreDefaultPresets();
            }
        }

        void DrawSkillLists(Rect area)
        {
            float columnWidth = (area.width - 20f) / 3f;
            var preset = slots.Presets[editIndex];

            for (int s = 0; s < 3; s++)
            {
                var slot = (SlotType)s;
                var column = new Rect(area.x + (columnWidth + 10f) * s, area.y, columnWidth, area.height);
                Text(new Rect(column.x, column.y - 4, column.width, 30), SlotLabels[s], 22, new Color(1f, 1f, 1f, 0.8f));

                var listRect = new Rect(column.x, column.y + 30, column.width, column.height - 30);
                Box(listRect, new Color(0.11f, 0.12f, 0.15f));

                // 武器種ごとにまとめて並べる
                var groups = new List<(WeaponTypeData weapon, List<SkillData> skills)>();
                foreach (var weapon in slots.Weapons)
                {
                    if (weapon == null) continue;
                    var list = new List<SkillData>();
                    foreach (var skill in slots.Database.ForSlot(slot))
                    {
                        if (skill.weapon == weapon.weapon) list.Add(skill);
                    }
                    if (list.Count > 0) groups.Add((weapon, list));
                }

                float contentHeight = 8f;
                foreach (var group in groups) contentHeight += HeaderHeight + group.skills.Count * RowHeight + 6f;

                var view = new Rect(0, 0, listRect.width - 20, contentHeight);
                scroll[s] = GUI.BeginScrollView(listRect, scroll[s], view);
                float y = 6f;
                var assigned = preset.Get(slot);
                foreach (var (weapon, skills) in groups)
                {
                    Box(new Rect(6, y + 8, 6, HeaderHeight - 14), weapon.color);
                    Text(new Rect(18, y + 2, view.width - 24, HeaderHeight), weapon.displayName, 18, weapon.color);
                    y += HeaderHeight;

                    foreach (var skill in skills)
                    {
                        var row = new Rect(6, y, view.width - 12, RowHeight - 4);
                        bool selected = skill == assigned;
                        bool hover = row.Contains(Event.current.mousePosition);

                        // 探索: まだ手に入れていないスキルは名前を隠し、選べないようにする
                        if (!slots.IsAvailable(skill))
                        {
                            Box(row, new Color(0.12f, 0.12f, 0.14f));
                            Text(new Rect(row.x + 10, row.y + 6, row.width - 14, row.height), "？？？", 17, new Color(1f, 1f, 1f, 0.3f));
                            if (hover) detailSkill = null;
                            y += RowHeight;
                            continue;
                        }
                        var background = selected ? Color.Lerp(weapon.color, Color.black, 0.45f)
                            : hover ? new Color(0.22f, 0.24f, 0.3f)
                            : new Color(0.15f, 0.16f, 0.2f);
                        Box(row, background);
                        if (selected) Outline(row, weapon.color, 2f);
                        Text(new Rect(row.x + 10, row.y + 6, row.width - 14, row.height), skill.displayName, 17, Color.white);

                        if (hover) detailSkill = skill;
                        if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                        {
                            slots.AssignPresetSkill(editIndex, slot, skill);
                            detailSkill = skill;
                        }
                        y += RowHeight;
                    }
                    y += 6f;
                }
                GUI.EndScrollView();
            }
        }

        void DrawDetail(Rect area)
        {
            var preset = slots.Presets[editIndex];
            Text(new Rect(area.x, area.y - 4, area.width, 30), $"プリセット{editIndex + 1} の名前", 22, new Color(1f, 1f, 1f, 0.8f));
            string newName = GUI.TextField(new Rect(area.x, area.y + 30, area.width, 42), preset.name, 16, textField);
            if (newName != preset.name) slots.RenamePreset(editIndex, newName);

            var box = new Rect(area.x, area.y + 90, area.width, area.height - 90);
            Box(box, new Color(0.11f, 0.12f, 0.15f));

            var skill = detailSkill;
            if (skill == null)
            {
                Text(new Rect(box.x + 16, box.y + 16, box.width - 32, 60), "スキルにマウスを乗せると\n詳しい説明が出ます", 20,
                    new Color(1f, 1f, 1f, 0.5f));
            }
            else
            {
                var weaponData = slots.GetWeaponData(skill.weapon);
                var color = weaponData != null ? weaponData.color : Color.white;
                float y = box.y + 14;
                Text(new Rect(box.x + 16, y, box.width - 32, 44), $"<b>{skill.displayName}</b>", 32, color);
                y += 46;
                string behavior = BehaviorLabels.TryGetValue(skill.behavior, out var b) ? b : skill.behavior.ToString();
                Text(new Rect(box.x + 16, y, box.width - 32, 28),
                    $"{(weaponData != null ? weaponData.displayName : "")}  /  {SlotLabels[(int)skill.slot]}  /  {behavior}", 18,
                    new Color(1f, 1f, 1f, 0.75f));
                y += 36;

                float descHeight = wrapLabel.CalcHeight(new GUIContent(skill.description), box.width - 32);
                GUI.Label(new Rect(box.x + 16, y, box.width - 32, descHeight), skill.description, wrapLabel);
                y += descHeight + 16;

                foreach (var line in StatLines(skill))
                {
                    Text(new Rect(box.x + 16, y, box.width - 32, 26), line, 18, new Color(0.8f, 0.9f, 1f));
                    y += 26;
                }
            }

            Text(new Rect(box.x + 16, box.yMax - 70, box.width - 32, 30), "このプリセットの武器種ボーナス", 18, new Color(1f, 1f, 1f, 0.6f));
            Text(new Rect(box.x + 16, box.yMax - 42, box.width - 32, 30), BonusText(preset.Bonus), 20, new Color(1f, 0.9f, 0.5f));
        }

        IEnumerable<string> StatLines(SkillData skill)
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

        // ---------- 描画の補助 ----------

        Color WeaponColor(SkillData skill)
        {
            var data = slots.GetWeaponData(skill.weapon);
            return data != null ? data.color : Color.white;
        }

        static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        void Text(Rect rect, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            label.fontSize = size;
            label.alignment = anchor;
            var previous = GUI.color;
            GUI.color = color;
            GUI.Label(rect, text, label);
            GUI.color = previous;
        }

        bool Button(Rect rect, string text, Color color)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Box(rect, hover ? Color.Lerp(color, Color.white, 0.15f) : color);
            buttonText.fontSize = 20;
            GUI.Label(rect, text, buttonText);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
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
