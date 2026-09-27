using UnityEngine;

namespace BattleFight
{
    /// <summary>ステージごとのベスト記録(PlayerPrefs)。ステージはアセット名で区別する。</summary>
    public static class StageRecords
    {
        static string Key(string stageId, string field) => $"BattleFight.Stage.{stageId}.{field}";

        public static bool IsCleared(string stageId) => PlayerPrefs.GetInt(Key(stageId, "cleared"), 0) == 1;

        /// <summary>記録がなければ 0</summary>
        public static float BestTime(string stageId) => PlayerPrefs.GetFloat(Key(stageId, "time"), 0f);

        /// <summary>記録がなければ -1</summary>
        public static int BestRank(string stageId) => PlayerPrefs.GetInt(Key(stageId, "rank"), -1);

        public static int BestWave(string stageId) => PlayerPrefs.GetInt(Key(stageId, "wave"), 0);

        /// <summary>クリアを記録する。タイムは短いほう、ランクは高いほうを残す。記録を更新したら true。</summary>
        public static bool RecordClear(string stageId, float time, int rank)
        {
            bool improved = !IsCleared(stageId);
            PlayerPrefs.SetInt(Key(stageId, "cleared"), 1);

            float best = BestTime(stageId);
            if (best <= 0f || time < best)
            {
                PlayerPrefs.SetFloat(Key(stageId, "time"), time);
                improved = true;
            }
            if (rank > BestRank(stageId))
            {
                PlayerPrefs.SetInt(Key(stageId, "rank"), rank);
                improved = true;
            }
            PlayerPrefs.Save();
            return improved;
        }

        public static bool RecordWave(string stageId, int wave)
        {
            if (wave <= BestWave(stageId)) return false;
            PlayerPrefs.SetInt(Key(stageId, "wave"), wave);
            PlayerPrefs.Save();
            return true;
        }

        public static void Clear(string stageId)
        {
            foreach (var field in new[] { "cleared", "time", "rank", "wave" }) PlayerPrefs.DeleteKey(Key(stageId, field));
            PlayerPrefs.Save();
        }
    }
}
