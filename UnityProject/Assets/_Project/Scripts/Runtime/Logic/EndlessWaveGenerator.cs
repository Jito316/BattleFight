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

        public static float BudgetFor(int iWave) => 2f + iWave * 1.5f;

        public static bool IsBossWave(int iWave, int iBossEvery) => iBossEvery > 0 && iWave > 0 && iWave % iBossEvery == 0;

        public static List<EnemyProfile> Generate(int iWave, IReadOnlyList<EndlessEntry> iPool, IReadOnlyList<EnemyProfile> iBosses,
            int iBossEvery, Random iRandom)
        {
            var aResult = new List<EnemyProfile>();
            float aBudget = BudgetFor(iWave);

            if (IsBossWave(iWave, iBossEvery) && iBosses != null && iBosses.Count > 0)
            {
                aResult.Add(iBosses[(iWave / iBossEvery - 1) % iBosses.Count]);
                // ボスのウェーブは護衛を少なめにする
                aBudget = 1f + iWave * 0.3f;
            }

            var aCandidates = new List<EndlessEntry>();
            if (iPool != null)
            {
                foreach (var aEntry in iPool)
                {
                    if (aEntry != null && aEntry.Profile != null && aEntry.MinWave <= iWave && aEntry.Cost > 0f) aCandidates.Add(aEntry);
                }
            }
            if (aCandidates.Count == 0) return aResult;

            var aAffordable = new List<EndlessEntry>();
            while (aResult.Count < MaxEnemiesPerWave)
            {
                aAffordable.Clear();
                foreach (var aEntry in aCandidates)
                {
                    if (aEntry.Cost <= aBudget) aAffordable.Add(aEntry);
                }
                if (aAffordable.Count == 0) break;

                var aPick = aAffordable[iRandom.Next(aAffordable.Count)];
                aResult.Add(aPick.Profile);
                aBudget -= aPick.Cost;
            }

            // 予算が足りなくても最低1体は出す
            if (aResult.Count == 0)
            {
                var aCheapest = aCandidates[0];
                foreach (var aEntry in aCandidates)
                {
                    if (aEntry.Cost < aCheapest.Cost) aCheapest = aEntry;
                }
                aResult.Add(aCheapest.Profile);
            }
            return aResult;
        }
    }
}
