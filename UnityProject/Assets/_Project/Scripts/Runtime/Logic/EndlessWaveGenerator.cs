using System;
using System.Collections.Generic;

namespace BattleFight
{
    /// <summary>
    /// エンドレスモードのウェーブ生成。ウェーブが進むほど予算が増え、強い敵が混ざる。
    /// bossEvery ウェーブごとにボスと少しの護衛が出る。
    /// </summary>
    public static class EndlessWaveGenerator
    {
        public const int MaxEnemiesPerWave = 8;

        public static float BudgetFor(int wave) => 2f + wave * 1.5f;

        public static bool IsBossWave(int wave, int bossEvery) => bossEvery > 0 && wave > 0 && wave % bossEvery == 0;

        public static List<EnemyProfile> Generate(int wave, IReadOnlyList<EndlessEntry> pool, IReadOnlyList<EnemyProfile> bosses,
            int bossEvery, Random random)
        {
            var result = new List<EnemyProfile>();
            float budget = BudgetFor(wave);

            if (IsBossWave(wave, bossEvery) && bosses != null && bosses.Count > 0)
            {
                result.Add(bosses[(wave / bossEvery - 1) % bosses.Count]);
                // ボスのウェーブは護衛を少なめにする
                budget = 1f + wave * 0.3f;
            }

            var candidates = new List<EndlessEntry>();
            if (pool != null)
            {
                foreach (var entry in pool)
                {
                    if (entry != null && entry.profile != null && entry.minWave <= wave && entry.cost > 0f) candidates.Add(entry);
                }
            }
            if (candidates.Count == 0) return result;

            var affordable = new List<EndlessEntry>();
            while (result.Count < MaxEnemiesPerWave)
            {
                affordable.Clear();
                foreach (var entry in candidates)
                {
                    if (entry.cost <= budget) affordable.Add(entry);
                }
                if (affordable.Count == 0) break;

                var pick = affordable[random.Next(affordable.Count)];
                result.Add(pick.profile);
                budget -= pick.cost;
            }

            // 予算が足りなくても最低1体は出す
            if (result.Count == 0)
            {
                var cheapest = candidates[0];
                foreach (var entry in candidates)
                {
                    if (entry.cost < cheapest.cost) cheapest = entry;
                }
                result.Add(cheapest.profile);
            }
            return result;
        }
    }
}
