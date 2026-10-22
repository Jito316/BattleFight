using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 探索の進み具合の保存。集めたスキル、壊した壁・取ったアイテム・倒した敵の集団(フラグ)、最後に触れた祭壇。
    /// スキルとフラグはアセット名 / ID の文字列で持つ。今のセーブスロット(SaveProfiles)に保存する。
    /// </summary>
    public static class ExplorationSave
    {
        public const string SaveName = "Explore.v1";

        [Serializable]
        class SavedData
        {
            [FormerlySerializedAs("version")] public int Version = 1;
            [FormerlySerializedAs("skills")] public List<string> Skills = new List<string>();
            [FormerlySerializedAs("flags")] public List<string> Flags = new List<string>();
            [FormerlySerializedAs("checkpoint")] public string Checkpoint;
            [FormerlySerializedAs("totalSkills")] public int TotalSkills;
        }

        static SavedData mCache;
        /// <summary>mCache を読んだときの SaveProfiles.Revision。スロットが変わったり消されたりしたら読み直す</summary>
        static int mCacheRevision = -1;

        /// <summary>新しくスキルを手に入れた</summary>
        public static event Action<SkillData> SkillUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mCache = null;
            mCacheRevision = -1;
            SkillUnlocked = null;
        }

        static SavedData mData
        {
            get
            {
                if (mCache == null || mCacheRevision != SaveProfiles.Revision)
                {
                    mCache = Deserialize(SaveProfiles.GetString(SaveName)) ?? new SavedData();
                    mCacheRevision = SaveProfiles.Revision;
                }
                return mCache;
            }
        }

        public static int UnlockedCount => mData.Skills.Count;

        /// <summary>このマップで集められるスキルの総数(ステージ選択の表示用に、探索を始めたときに記録する)</summary>
        public static int TotalSkills
        {
            get => mData.TotalSkills;
            set
            {
                if (mData.TotalSkills == value) return;
                mData.TotalSkills = value;
                Save();
            }
        }

        /// <summary>最後に触れた祭壇の ID。まだなければ null</summary>
        public static string Checkpoint
        {
            get => string.IsNullOrEmpty(mData.Checkpoint) ? null : mData.Checkpoint;
            set
            {
                if (mData.Checkpoint == value) return;
                mData.Checkpoint = value;
                Save();
            }
        }

        public static bool HasStarted => mData.Skills.Count > 0;

        public static bool IsUnlocked(SkillData iSkill) => iSkill != null && mData.Skills.Contains(iSkill.name);

        /// <summary>スキルを手に入れる。新しく手に入れたときだけ true</summary>
        public static bool Unlock(SkillData iSkill)
        {
            if (iSkill == null || mData.Skills.Contains(iSkill.name)) return false;
            mData.Skills.Add(iSkill.name);
            Save();
            SkillUnlocked?.Invoke(iSkill);
            return true;
        }

        public static bool HasFlag(string iId) => !string.IsNullOrEmpty(iId) && mData.Flags.Contains(iId);

        public static void SetFlag(string iId)
        {
            if (string.IsNullOrEmpty(iId) || mData.Flags.Contains(iId)) return;
            mData.Flags.Add(iId);
            Save();
        }

        /// <summary>探索をはじめからにする(スキル・フラグ・祭壇をすべて消す)</summary>
        public static void Reset()
        {
            mCache = new SavedData();
            mCacheRevision = SaveProfiles.Revision;
            SaveProfiles.DeleteKey(SaveName);
            SaveProfiles.Save();
        }

        /// <summary>探索をはじめからにする: 進み具合・探索の編成・探索ステージの記録を消す</summary>
        public static void StartOver(string iStageId)
        {
            Reset();
            LoadoutStorage.ClearExploration();
            if (!string.IsNullOrEmpty(iStageId)) StageRecords.Clear(iStageId);
        }

        static void Save()
        {
            SaveProfiles.SetString(SaveName, Serialize());
            SaveProfiles.Save();
        }

        public static string Serialize() => JsonUtility.ToJson(mData);

        /// <summary>壊れたデータや空のときは null</summary>
        static SavedData Deserialize(string iJson)
        {
            if (string.IsNullOrEmpty(iJson)) return null;
            try
            {
                var aSaved = JsonUtility.FromJson<SavedData>(iJson);
                if (aSaved == null) return null;
                aSaved.Skills ??= new List<string>();
                aSaved.Flags ??= new List<string>();
                return aSaved;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>テスト用: 保存された文字列から読み直す</summary>
        public static void LoadFrom(string iJson)
        {
            mCache = Deserialize(iJson) ?? new SavedData();
            mCacheRevision = SaveProfiles.Revision;
        }
    }
}
