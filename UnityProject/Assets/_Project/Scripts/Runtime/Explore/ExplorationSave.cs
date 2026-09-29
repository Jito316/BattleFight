using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 探索の進み具合の保存。集めたスキル、壊した壁・取ったアイテム・倒した敵の集団(フラグ)、最後に触れた祭壇。
    /// スキルとフラグはアセット名 / ID の文字列で持つ。WebGL では PlayerPrefs がブラウザに保存される。
    /// </summary>
    public static class ExplorationSave
    {
        public const string PrefsKey = "BattleFight.Explore.v1";

        [Serializable]
        class SavedData
        {
            public int version = 1;
            public List<string> skills = new List<string>();
            public List<string> flags = new List<string>();
            public string checkpoint;
            public int totalSkills;
        }

        static SavedData data;

        /// <summary>新しくスキルを手に入れた</summary>
        public static event Action<SkillData> SkillUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            data = null;
            SkillUnlocked = null;
        }

        static SavedData Data
        {
            get
            {
                if (data == null) data = Deserialize(PlayerPrefs.GetString(PrefsKey, null)) ?? new SavedData();
                return data;
            }
        }

        public static int UnlockedCount => Data.skills.Count;

        /// <summary>このマップで集められるスキルの総数(ステージ選択の表示用に、探索を始めたときに記録する)</summary>
        public static int TotalSkills
        {
            get => Data.totalSkills;
            set
            {
                if (Data.totalSkills == value) return;
                Data.totalSkills = value;
                Save();
            }
        }

        /// <summary>最後に触れた祭壇の ID。まだなければ null</summary>
        public static string Checkpoint
        {
            get => string.IsNullOrEmpty(Data.checkpoint) ? null : Data.checkpoint;
            set
            {
                if (Data.checkpoint == value) return;
                Data.checkpoint = value;
                Save();
            }
        }

        public static bool HasStarted => Data.skills.Count > 0;

        public static bool IsUnlocked(SkillData skill) => skill != null && Data.skills.Contains(skill.name);

        /// <summary>スキルを手に入れる。新しく手に入れたときだけ true</summary>
        public static bool Unlock(SkillData skill)
        {
            if (skill == null || Data.skills.Contains(skill.name)) return false;
            Data.skills.Add(skill.name);
            Save();
            SkillUnlocked?.Invoke(skill);
            return true;
        }

        public static bool HasFlag(string id) => !string.IsNullOrEmpty(id) && Data.flags.Contains(id);

        public static void SetFlag(string id)
        {
            if (string.IsNullOrEmpty(id) || Data.flags.Contains(id)) return;
            Data.flags.Add(id);
            Save();
        }

        /// <summary>探索をはじめからにする(スキル・フラグ・祭壇をすべて消す)</summary>
        public static void Reset()
        {
            data = new SavedData();
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, Serialize());
            PlayerPrefs.Save();
        }

        public static string Serialize() => JsonUtility.ToJson(Data);

        /// <summary>壊れたデータや空のときは null</summary>
        static SavedData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var saved = JsonUtility.FromJson<SavedData>(json);
                if (saved == null) return null;
                saved.skills ??= new List<string>();
                saved.flags ??= new List<string>();
                return saved;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>テスト用: 保存された文字列から読み直す</summary>
        public static void LoadFrom(string json) => data = Deserialize(json) ?? new SavedData();
    }
}
