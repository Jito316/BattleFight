using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 3スロットのスキルを管理し、戦闘中のリアルタイム切り替えを受け付ける。
    /// ・ラック方式: スロットごとに最大3候補を順送り
    /// ・プリセット方式: 3スロットをまとめたプリセット(最大4つ)を一括で切り替え
    /// </summary>
    public class SkillSlotController : MonoBehaviour
    {
        public const int PresetCount = 4;

        [Header("切り替え方式")]
        [SerializeField] SwapMode mode = SwapMode.Rack;

        [Header("ラック方式")]
        [SerializeField] SkillData[] attackARack = new SkillData[SkillRack.Capacity];
        [SerializeField] SkillData[] attackBRack = new SkillData[SkillRack.Capacity];
        [SerializeField] SkillData[] movementRack = new SkillData[SkillRack.Capacity];

        [Header("プリセット方式")]
        [SerializeField] SkillPreset[] defaultPresets = new SkillPreset[PresetCount];
        [SerializeField, Tooltip("編成画面で選べるスキルと、保存データの読み込みに使う")] SkillDatabase database;
        [SerializeField, Tooltip("編成をブラウザ/PCに保存して、次回も使う")] bool persistLoadout = true;

        [Header("武器種")]
        [SerializeField] WeaponTypeData[] weaponDatabase;

        SkillRack[] racks;
        readonly List<SkillPreset> presets = new List<SkillPreset>();
        int presetIndex;

        /// <summary>スロットのスキルが変わった(切り替え・編成の変更)</summary>
        public event Action<SlotType> SlotChanged;
        public event Action PresetChanged;
        public event Action ModeChanged;

        public SwapMode Mode => mode;
        public int PresetIndex => presetIndex;
        public IReadOnlyList<SkillPreset> Presets => presets;
        public SkillPreset CurrentPreset => presets.Count > 0 ? presets[presetIndex] : null;
        public SkillDatabase Database => database;
        public IReadOnlyList<WeaponTypeData> Weapons => weaponDatabase;
        public WeaponBonus Bonus { get; private set; }

        /// <summary>マスタリー中ならその武器種のフィニッシャー、それ以外は null</summary>
        public SkillData MasteryFinisher
        {
            get
            {
                if (Bonus.Kind != WeaponBonusKind.Mastery) return null;
                var data = GetWeaponData(Bonus.Weapon);
                return data != null ? data.masteryFinisher : null;
            }
        }

        void Awake()
        {
            racks = new[]
            {
                new SkillRack(attackARack),
                new SkillRack(attackBRack),
                new SkillRack(movementRack),
            };

            ResetPresetsToDefault();
            if (persistLoadout && LoadoutStorage.TryLoad(database, out var saved))
            {
                mode = saved.mode;
                for (int i = 0; i < presets.Count && i < saved.presets.Count; i++) presets[i] = saved.presets[i];
                presetIndex = Mathf.Clamp(saved.presetIndex, 0, presets.Count - 1);
            }
            RecomputeBonus();
        }

        void ResetPresetsToDefault()
        {
            presets.Clear();
            for (int i = 0; i < PresetCount; i++)
            {
                var source = defaultPresets != null && i < defaultPresets.Length ? defaultPresets[i] : null;
                presets.Add(source != null ? source.Clone() : new SkillPreset { name = $"プリセット{i + 1}" });
            }
            presetIndex = 0;
        }

        public SkillRack GetRack(SlotType slot) => racks[(int)slot];

        public SkillData GetCurrent(SlotType slot)
        {
            if (mode == SwapMode.Rack) return racks[(int)slot].Current;
            var preset = CurrentPreset;
            return preset != null ? preset.Get(slot) : null;
        }

        /// <summary>ラック方式: スロットを次の候補へ切り替える</summary>
        public bool Cycle(SlotType slot)
        {
            if (mode != SwapMode.Rack || !racks[(int)slot].Cycle()) return false;
            RecomputeBonus();
            SlotChanged?.Invoke(slot);
            return true;
        }

        /// <summary>プリセット方式: プリセットを一括で切り替える。中身が変わったスロットだけ SlotChanged を出す。</summary>
        public bool SelectPreset(int index)
        {
            if (mode != SwapMode.Preset || index < 0 || index >= presets.Count || index == presetIndex) return false;

            var before = new[] { GetCurrent(SlotType.AttackA), GetCurrent(SlotType.AttackB), GetCurrent(SlotType.Movement) };
            presetIndex = index;
            RecomputeBonus();
            for (int i = 0; i < 3; i++)
            {
                if (GetCurrent((SlotType)i) != before[i]) SlotChanged?.Invoke((SlotType)i);
            }
            PresetChanged?.Invoke();
            return true;
        }

        public void SetMode(SwapMode newMode)
        {
            if (mode == newMode) return;
            mode = newMode;
            RecomputeBonus();
            for (int i = 0; i < 3; i++) SlotChanged?.Invoke((SlotType)i);
            ModeChanged?.Invoke();
        }

        /// <summary>編成画面から: プリセットのスロットにスキルを入れる</summary>
        public bool AssignPresetSkill(int index, SlotType slot, SkillData skill)
        {
            if (index < 0 || index >= presets.Count || !presets[index].Set(slot, skill)) return false;
            if (index == presetIndex)
            {
                RecomputeBonus();
                if (mode == SwapMode.Preset) SlotChanged?.Invoke(slot);
            }
            return true;
        }

        public void RenamePreset(int index, string newName)
        {
            if (index >= 0 && index < presets.Count) presets[index].name = newName;
        }

        public void RestoreDefaultPresets()
        {
            ResetPresetsToDefault();
            RecomputeBonus();
            for (int i = 0; i < 3; i++) SlotChanged?.Invoke((SlotType)i);
            PresetChanged?.Invoke();
        }

        public void SaveLoadout()
        {
            if (persistLoadout) LoadoutStorage.Save(mode, presetIndex, presets);
        }

        public WeaponTypeData GetWeaponData(WeaponType weapon)
        {
            if (weaponDatabase == null) return null;
            foreach (var data in weaponDatabase)
            {
                if (data != null && data.weapon == weapon) return data;
            }
            return null;
        }

        void RecomputeBonus()
        {
            var a = GetCurrent(SlotType.AttackA);
            var b = GetCurrent(SlotType.AttackB);
            var m = GetCurrent(SlotType.Movement);
            Bonus = a != null && b != null && m != null
                ? WeaponBonusResolver.Resolve(a.weapon, b.weapon, m.weapon)
                : WeaponBonus.None;
        }
    }
}
