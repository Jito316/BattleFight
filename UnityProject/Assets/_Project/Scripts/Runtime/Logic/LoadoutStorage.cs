using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
            [FormerlySerializedAs("name")] public string Name;
            [FormerlySerializedAs("attackA")] public string AttackA;
            [FormerlySerializedAs("attackB")] public string AttackB;
            [FormerlySerializedAs("movement")] public string Movement;
        }

        [Serializable]
        class SavedLoadout
        {
            [FormerlySerializedAs("version")] public int Version = 1;
            [FormerlySerializedAs("mode")] public SwapMode Mode;
            [FormerlySerializedAs("presetIndex")] public int PresetIndex;
            [FormerlySerializedAs("presets")] public SavedPreset[] Presets;
        }

        public struct Loadout
        {
            public SwapMode Mode;
            public int PresetIndex;
            public List<SkillPreset> Presets;
        }

        public static string Serialize(SwapMode iMode, int iPresetIndex, IReadOnlyList<SkillPreset> iPresets)
        {
            var aSaved = new SavedLoadout
            {
                Mode = iMode,
                PresetIndex = iPresetIndex,
                Presets = new SavedPreset[iPresets.Count],
            };
            for (int aI = 0; aI < iPresets.Count; aI++)
            {
                var aPreset = iPresets[aI];
                aSaved.Presets[aI] = new SavedPreset
                {
                    Name = aPreset.Name,
                    AttackA = aPreset.AttackA != null ? aPreset.AttackA.name : null,
                    AttackB = aPreset.AttackB != null ? aPreset.AttackB.name : null,
                    Movement = aPreset.Movement != null ? aPreset.Movement.name : null,
                };
            }
            return JsonUtility.ToJson(aSaved);
        }

        /// <summary>見つからないスキルは null になる(データを消したときなど)</summary>
        public static bool TryDeserialize(string iJson, SkillDatabase iDatabase, out Loadout oLoadout)
        {
            oLoadout = default;
            if (string.IsNullOrEmpty(iJson) || iDatabase == null) return false;

            SavedLoadout aSaved;
            try
            {
                aSaved = JsonUtility.FromJson<SavedLoadout>(iJson);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (aSaved == null || aSaved.Presets == null) return false;

            var aPresets = new List<SkillPreset>(aSaved.Presets.Length);
            foreach (var aP in aSaved.Presets)
            {
                var aPreset = new SkillPreset { Name = aP.Name };
                aPreset.Set(SlotType.AttackA, iDatabase.Find(aP.AttackA));
                aPreset.Set(SlotType.AttackB, iDatabase.Find(aP.AttackB));
                aPreset.Set(SlotType.Movement, iDatabase.Find(aP.Movement));
                aPresets.Add(aPreset);
            }

            oLoadout = new Loadout
            {
                Mode = aSaved.Mode,
                PresetIndex = Mathf.Clamp(aSaved.PresetIndex, 0, Mathf.Max(0, aPresets.Count - 1)),
                Presets = aPresets,
            };
            return true;
        }

        /// <summary>探索モードの編成(集めたスキルだけで組む)。アリーナの編成とは別に保存する</summary>
        public const string ExplorationPrefsKey = "BattleFight.Loadout.Explore.v1";

        public static void Save(SwapMode iMode, int iPresetIndex, IReadOnlyList<SkillPreset> iPresets) =>
            Save(PrefsKey, iMode, iPresetIndex, iPresets);

        public static void Save(string iKey, SwapMode iMode, int iPresetIndex, IReadOnlyList<SkillPreset> iPresets)
        {
            PlayerPrefs.SetString(iKey, Serialize(iMode, iPresetIndex, iPresets));
            PlayerPrefs.Save();
        }

        public static bool TryLoad(SkillDatabase iDatabase, out Loadout oLoadout) => TryLoad(PrefsKey, iDatabase, out oLoadout);

        public static bool TryLoad(string iKey, SkillDatabase iDatabase, out Loadout oLoadout) =>
            TryDeserialize(PlayerPrefs.GetString(iKey, null), iDatabase, out oLoadout);

        /// <summary>アリーナと探索の両方の編成を消す</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.DeleteKey(ExplorationPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
