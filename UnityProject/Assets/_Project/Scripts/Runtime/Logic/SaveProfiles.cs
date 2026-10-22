using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>セーブデータの読み書き先。ふだんは PlayerPrefs(WebGL ではブラウザ)。テストではメモリに差し替える</summary>
    public interface ISaveStore
    {
        bool HasKey(string iKey);
        string GetString(string iKey, string iDefault);
        void SetString(string iKey, string iValue);
        int GetInt(string iKey, int iDefault);
        void SetInt(string iKey, int iValue);
        float GetFloat(string iKey, float iDefault);
        void SetFloat(string iKey, float iValue);
        void DeleteKey(string iKey);
        void Save();
    }

    public class PlayerPrefsStore : ISaveStore
    {
        public bool HasKey(string iKey) => PlayerPrefs.HasKey(iKey);
        public string GetString(string iKey, string iDefault) => PlayerPrefs.GetString(iKey, iDefault);
        public void SetString(string iKey, string iValue) => PlayerPrefs.SetString(iKey, iValue);
        public int GetInt(string iKey, int iDefault) => PlayerPrefs.GetInt(iKey, iDefault);
        public void SetInt(string iKey, int iValue) => PlayerPrefs.SetInt(iKey, iValue);
        public float GetFloat(string iKey, float iDefault) => PlayerPrefs.GetFloat(iKey, iDefault);
        public void SetFloat(string iKey, float iValue) => PlayerPrefs.SetFloat(iKey, iValue);
        public void DeleteKey(string iKey) => PlayerPrefs.DeleteKey(iKey);
        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// セーブスロット(プロフィール)。探索の進み具合・編成・ステージの記録を、スロットごとに分けて持つ。
    /// 中身は "BattleFight.Slot{番号}.{名前}" というキーで保存し、スロットごとに使ったキーの一覧も残す(スロットを消すため)。
    /// スロットを分ける前のデータ(番号なしのキー)は、最初に使ったときにスロット 1 へ移す。
    /// </summary>
    public static class SaveProfiles
    {
        public const int SlotCount = 3;
        public const string CurrentKey = "BattleFight.Profile.Current";
        const string mPrefix = "BattleFight.";
        const string mIndexName = "Profile.Keys";
        const string mLastPlayedName = "Profile.LastPlayed";

        /// <summary>スロットを分ける前に使っていたキー(移す対象)</summary>
        static readonly string[] mLegacyStringKeys = { "Explore.v1", "Loadout.v1", "Loadout.Explore.v1" };
        static readonly string[] mLegacyStageFields = { "cleared", "time", "rank", "wave" };

        static ISaveStore mStore;
        static int mCurrent;
        static bool mMigrated;
        static readonly HashSet<string> mMigratedStages = new HashSet<string>();

        /// <summary>選んでいるスロットが変わった(または今のスロットを消した)</summary>
        public static event Action Changed;

        /// <summary>スロットを切り替える・消す・読み書き先を変えるたびに増える(読み込んだ中身を持っているところが、読み直す目安)</summary>
        public static int Revision { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mStore = null;
            mCurrent = 0;
            mMigrated = false;
            mMigratedStages.Clear();
            Changed = null;
        }

        /// <summary>読み書き先(テスト用に差し替えられる)。差し替えると状態を読み直す</summary>
        public static ISaveStore Store
        {
            get => mStore ??= new PlayerPrefsStore();
            set
            {
                mStore = value;
                Revision++;
                mCurrent = 0;
                mMigrated = false;
                mMigratedStages.Clear();
            }
        }

        /// <summary>今のスロット(1〜SlotCount)</summary>
        public static int Current
        {
            get
            {
                if (mCurrent == 0)
                {
                    mCurrent = Mathf.Clamp(Store.GetInt(CurrentKey, 1), 1, SlotCount);
                    MigrateLegacy();
                }
                return mCurrent;
            }
        }

        public static void Select(int iSlot)
        {
            iSlot = Mathf.Clamp(iSlot, 1, SlotCount);
            if (iSlot == Current) return;
            mCurrent = iSlot;
            Revision++;
            Store.SetInt(CurrentKey, iSlot);
            Store.Save();
            Changed?.Invoke();
        }

        /// <summary>スロットにデータがあるか</summary>
        public static bool IsUsed(int iSlot) => !string.IsNullOrEmpty(Store.GetString(SlotKey(iSlot, mIndexName), null));

        /// <summary>最後に遊んだ日時。なければ null</summary>
        public static DateTime? LastPlayed(int iSlot)
        {
            string aText = Store.GetString(SlotKey(iSlot, mLastPlayedName), null);
            return long.TryParse(aText, out long aTicks) ? new DateTime(aTicks, DateTimeKind.Utc).ToLocalTime() : (DateTime?)null;
        }

        /// <summary>スロットの中身をすべて消す(今のスロットなら、読み直しを知らせる)</summary>
        public static void Delete(int iSlot)
        {
            foreach (var aKey in Index(iSlot)) Store.DeleteKey(aKey);
            Store.DeleteKey(SlotKey(iSlot, mIndexName));
            Store.Save();
            Revision++;
            if (iSlot == Current) Changed?.Invoke();
        }

        // ---------- 今のスロットへの読み書き ----------

        public static bool HasKey(string iName) => Store.HasKey(Key(iName));
        public static string GetString(string iName, string iDefault = null) => Store.GetString(Key(iName), iDefault);
        public static int GetInt(string iName, int iDefault = 0) => Store.GetInt(Key(iName), iDefault);
        public static float GetFloat(string iName, float iDefault = 0f) => Store.GetFloat(Key(iName), iDefault);

        public static void SetString(string iName, string iValue)
        {
            Store.SetString(Key(iName), iValue);
            Touch(Current, Key(iName));
        }

        public static void SetInt(string iName, int iValue)
        {
            Store.SetInt(Key(iName), iValue);
            Touch(Current, Key(iName));
        }

        public static void SetFloat(string iName, float iValue)
        {
            Store.SetFloat(Key(iName), iValue);
            Touch(Current, Key(iName));
        }

        public static void DeleteKey(string iName) => Store.DeleteKey(Key(iName));

        public static void Save() => Store.Save();

        /// <summary>今のスロットのキー</summary>
        public static string Key(string iName) => SlotKey(Current, iName);

        public static string SlotKey(int iSlot, string iName) => $"{mPrefix}Slot{iSlot}.{iName}";

        // ---------- 前のデータを移す ----------

        /// <summary>スロットを分ける前の編成・探索のデータを、スロット 1 へ移す(1度だけ)</summary>
        static void MigrateLegacy()
        {
            if (mMigrated) return;
            mMigrated = true;
            bool aMoved = false;
            foreach (var aName in mLegacyStringKeys)
            {
                string aLegacy = mPrefix + aName;
                if (!Store.HasKey(aLegacy)) continue;
                string aTarget = SlotKey(1, aName);
                if (!Store.HasKey(aTarget))
                {
                    Store.SetString(aTarget, Store.GetString(aLegacy, null));
                    Touch(1, aTarget);
                }
                Store.DeleteKey(aLegacy);
                aMoved = true;
            }
            if (aMoved) Store.Save();
        }

        /// <summary>スロットを分ける前のステージの記録を、スロット 1 へ移す(ステージごとに 1度だけ)</summary>
        public static void MigrateLegacyStage(string iStageId)
        {
            if (string.IsNullOrEmpty(iStageId) || !mMigratedStages.Add(iStageId)) return;
            bool aMoved = false;
            foreach (var aField in mLegacyStageFields)
            {
                string aName = $"Stage.{iStageId}.{aField}";
                string aLegacy = mPrefix + aName;
                if (!Store.HasKey(aLegacy)) continue;
                string aTarget = SlotKey(1, aName);
                if (!Store.HasKey(aTarget))
                {
                    if (aField == "time") Store.SetFloat(aTarget, Store.GetFloat(aLegacy, 0f));
                    else Store.SetInt(aTarget, Store.GetInt(aLegacy, 0));
                    Touch(1, aTarget);
                }
                Store.DeleteKey(aLegacy);
                aMoved = true;
            }
            if (aMoved) Store.Save();
        }

        // ---------- スロットごとのキーの一覧 ----------

        static List<string> Index(int iSlot)
        {
            string aText = Store.GetString(SlotKey(iSlot, mIndexName), null);
            var aKeys = new List<string>();
            if (string.IsNullOrEmpty(aText)) return aKeys;
            foreach (var aKey in aText.Split('\n'))
            {
                if (aKey.Length > 0) aKeys.Add(aKey);
            }
            return aKeys;
        }

        /// <summary>書き込んだキーを一覧に入れ、最後に遊んだ日時を更新する</summary>
        static void Touch(int iSlot, string iKey)
        {
            var aKeys = Index(iSlot);
            string aLastPlayed = SlotKey(iSlot, mLastPlayedName);
            bool aDirty = false;
            foreach (var aKey in new[] { iKey, aLastPlayed })
            {
                if (aKeys.Contains(aKey)) continue;
                aKeys.Add(aKey);
                aDirty = true;
            }
            if (aDirty) Store.SetString(SlotKey(iSlot, mIndexName), string.Join("\n", aKeys));
            Store.SetString(aLastPlayed, DateTime.UtcNow.Ticks.ToString());
        }
    }
}
