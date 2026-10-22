using UnityEngine;

namespace BattleFight
{
    /// <summary>ステージごとのベスト記録(PlayerPrefs)。ステージはアセット名で区別する。</summary>
    public static class StageRecords
    {
        static string Key(string iStageId, string iField) => $"BattleFight.Stage.{iStageId}.{iField}";

        public static bool IsCleared(string iStageId) => PlayerPrefs.GetInt(Key(iStageId, "cleared"), 0) == 1;

        /// <summary>記録がなければ 0</summary>
        public static float BestTime(string iStageId) => PlayerPrefs.GetFloat(Key(iStageId, "time"), 0f);

        /// <summary>記録がなければ -1</summary>
        public static int BestRank(string iStageId) => PlayerPrefs.GetInt(Key(iStageId, "rank"), -1);

        public static int BestWave(string iStageId) => PlayerPrefs.GetInt(Key(iStageId, "wave"), 0);

        /// <summary>クリアを記録する。タイムは短いほう、ランクは高いほうを残す。記録を更新したら true。</summary>
        public static bool RecordClear(string iStageId, float iTime, int iRank)
        {
            bool aImproved = !IsCleared(iStageId);
            PlayerPrefs.SetInt(Key(iStageId, "cleared"), 1);

            float aBest = BestTime(iStageId);
            if (aBest <= 0f || iTime < aBest)
            {
                PlayerPrefs.SetFloat(Key(iStageId, "time"), iTime);
                aImproved = true;
            }
            if (iRank > BestRank(iStageId))
            {
                PlayerPrefs.SetInt(Key(iStageId, "rank"), iRank);
                aImproved = true;
            }
            PlayerPrefs.Save();
            return aImproved;
        }

        public static bool RecordWave(string iStageId, int iWave)
        {
            if (iWave <= BestWave(iStageId)) return false;
            PlayerPrefs.SetInt(Key(iStageId, "wave"), iWave);
            PlayerPrefs.Save();
            return true;
        }

        public static void Clear(string iStageId)
        {
            foreach (var aField in new[] { "cleared", "time", "rank", "wave" }) PlayerPrefs.DeleteKey(Key(iStageId, aField));
            PlayerPrefs.Save();
        }
    }
}
