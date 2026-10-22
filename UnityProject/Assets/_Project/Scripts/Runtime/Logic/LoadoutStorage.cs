using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 切り替え方式とプリセットの保存。スキルはアセット名で保存する。
    /// 今のセーブスロット(SaveProfiles)に保存する。WebGL ではブラウザ(IndexedDB)に残る。
    /// </summary>
    public static class LoadoutStorage
    {
        public const string SaveName = "Loadout.v1";

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
            for (int aPresetIndex = 0; aPresetIndex < iPresets.Count; aPresetIndex++)
            {
                var aPreset = iPresets[aPresetIndex];
                aSaved.Presets[aPresetIndex] = new SavedPreset
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
            foreach (var aSavedPreset in aSaved.Presets)
            {
                var aPreset = new SkillPreset { Name = aSavedPreset.Name };
                aPreset.Set(SlotType.AttackA, iDatabase.Find(aSavedPreset.AttackA));
                aPreset.Set(SlotType.AttackB, iDatabase.Find(aSavedPreset.AttackB));
                aPreset.Set(SlotType.Movement, iDatabase.Find(aSavedPreset.Movement));
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
        public const string ExplorationSaveName = "Loadout.Explore.v1";

        public static void Save(SwapMode iMode, int iPresetIndex, IReadOnlyList<SkillPreset> iPresets) =>
            Save(SaveName, iMode, iPresetIndex, iPresets);

        public static void Save(string iName, SwapMode iMode, int iPresetIndex, IReadOnlyList<SkillPreset> iPresets)
        {
            SaveProfiles.SetString(iName, Serialize(iMode, iPresetIndex, iPresets));
            SaveProfiles.Save();
        }

        public static bool TryLoad(SkillDatabase iDatabase, out Loadout oLoadout) => TryLoad(SaveName, iDatabase, out oLoadout);

        public static bool TryLoad(string iName, SkillDatabase iDatabase, out Loadout oLoadout) =>
            TryDeserialize(SaveProfiles.GetString(iName), iDatabase, out oLoadout);

        /// <summary>アリーナと探索の両方の編成を消す</summary>
        public static void Clear()
        {
            SaveProfiles.DeleteKey(SaveName);
            SaveProfiles.DeleteKey(ExplorationSaveName);
            SaveProfiles.Save();
        }

        /// <summary>探索の編成だけを消す</summary>
        public static void ClearExploration()
        {
            SaveProfiles.DeleteKey(ExplorationSaveName);
            SaveProfiles.Save();
        }
    }
}
