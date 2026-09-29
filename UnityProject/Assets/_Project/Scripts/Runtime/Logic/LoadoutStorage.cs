using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 切り替え方式とプリセットの保存。スキルはアセット名で保存する。
    /// WebGL では PlayerPrefs がブラウザ(IndexedDB)に保存される。
    /// </summary>
    public static class LoadoutStorage
    {
        public const string PrefsKey = "BattleFight.Loadout.v1";

        [Serializable]
        class SavedPreset
        {
            public string name;
            public string attackA;
            public string attackB;
            public string movement;
        }

        [Serializable]
        class SavedLoadout
        {
            public int version = 1;
            public SwapMode mode;
            public int presetIndex;
            public SavedPreset[] presets;
        }

        public struct Loadout
        {
            public SwapMode mode;
            public int presetIndex;
            public List<SkillPreset> presets;
        }

        public static string Serialize(SwapMode mode, int presetIndex, IReadOnlyList<SkillPreset> presets)
        {
            var saved = new SavedLoadout
            {
                mode = mode,
                presetIndex = presetIndex,
                presets = new SavedPreset[presets.Count],
            };
            for (int i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                saved.presets[i] = new SavedPreset
                {
                    name = preset.name,
                    attackA = preset.attackA != null ? preset.attackA.name : null,
                    attackB = preset.attackB != null ? preset.attackB.name : null,
                    movement = preset.movement != null ? preset.movement.name : null,
                };
            }
            return JsonUtility.ToJson(saved);
        }

        /// <summary>見つからないスキルは null になる(データを消したときなど)</summary>
        public static bool TryDeserialize(string json, SkillDatabase database, out Loadout loadout)
        {
            loadout = default;
            if (string.IsNullOrEmpty(json) || database == null) return false;

            SavedLoadout saved;
            try
            {
                saved = JsonUtility.FromJson<SavedLoadout>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (saved == null || saved.presets == null) return false;

            var presets = new List<SkillPreset>(saved.presets.Length);
            foreach (var p in saved.presets)
            {
                var preset = new SkillPreset { name = p.name };
                preset.Set(SlotType.AttackA, database.Find(p.attackA));
                preset.Set(SlotType.AttackB, database.Find(p.attackB));
                preset.Set(SlotType.Movement, database.Find(p.movement));
                presets.Add(preset);
            }

            loadout = new Loadout
            {
                mode = saved.mode,
                presetIndex = Mathf.Clamp(saved.presetIndex, 0, Mathf.Max(0, presets.Count - 1)),
                presets = presets,
            };
            return true;
        }

        /// <summary>探索モードの編成(集めたスキルだけで組む)。アリーナの編成とは別に保存する</summary>
        public const string ExplorationPrefsKey = "BattleFight.Loadout.Explore.v1";

        public static void Save(SwapMode mode, int presetIndex, IReadOnlyList<SkillPreset> presets) =>
            Save(PrefsKey, mode, presetIndex, presets);

        public static void Save(string key, SwapMode mode, int presetIndex, IReadOnlyList<SkillPreset> presets)
        {
            PlayerPrefs.SetString(key, Serialize(mode, presetIndex, presets));
            PlayerPrefs.Save();
        }

        public static bool TryLoad(SkillDatabase database, out Loadout loadout) => TryLoad(PrefsKey, database, out loadout);

        public static bool TryLoad(string key, SkillDatabase database, out Loadout loadout) =>
            TryDeserialize(PlayerPrefs.GetString(key, null), database, out loadout);

        /// <summary>アリーナと探索の両方の編成を消す</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.DeleteKey(ExplorationPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
