namespace BattleFight
{
    /// <summary>ステージごとのベスト記録。今のセーブスロット(SaveProfiles)に保存する。ステージはアセット名で区別する。</summary>
    public static class StageRecords
    {
        static string Name(string iStageId, string iField)
        {
            SaveProfiles.MigrateLegacyStage(iStageId);
            return $"Stage.{iStageId}.{iField}";
        }

        public static bool IsCleared(string iStageId) => SaveProfiles.GetInt(Name(iStageId, "cleared")) == 1;

        /// <summary>記録がなければ 0</summary>
        public static float BestTime(string iStageId) => SaveProfiles.GetFloat(Name(iStageId, "time"));

        /// <summary>記録がなければ -1</summary>
        public static int BestRank(string iStageId) => SaveProfiles.GetInt(Name(iStageId, "rank"), -1);

        public static int BestWave(string iStageId) => SaveProfiles.GetInt(Name(iStageId, "wave"));

        /// <summary>クリアを記録する。タイムは短いほう、ランクは高いほうを残す。記録を更新したら true。</summary>
        public static bool RecordClear(string iStageId, float iTime, int iRank)
        {
            bool aImproved = !IsCleared(iStageId);
            SaveProfiles.SetInt(Name(iStageId, "cleared"), 1);

            float aBest = BestTime(iStageId);
            if (aBest <= 0f || iTime < aBest)
            {
                SaveProfiles.SetFloat(Name(iStageId, "time"), iTime);
                aImproved = true;
            }
            if (iRank > BestRank(iStageId))
            {
                SaveProfiles.SetInt(Name(iStageId, "rank"), iRank);
                aImproved = true;
            }
            SaveProfiles.Save();
            return aImproved;
        }

        public static bool RecordWave(string iStageId, int iWave)
        {
            if (iWave <= BestWave(iStageId)) return false;
            SaveProfiles.SetInt(Name(iStageId, "wave"), iWave);
            SaveProfiles.Save();
            return true;
        }

        public static void Clear(string iStageId)
        {
            foreach (var aField in new[] { "cleared", "time", "rank", "wave" }) SaveProfiles.DeleteKey(Name(iStageId, aField));
            SaveProfiles.Save();
        }
    }
}
