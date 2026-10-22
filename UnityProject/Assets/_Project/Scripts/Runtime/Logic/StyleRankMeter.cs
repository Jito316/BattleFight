using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>スタイルランクの計算。MonoBehaviour に依存しないのでテストできる。</summary>
    public class StyleRankMeter
    {
        readonly StyleRankConfig mConfig;
        readonly Queue<string> mHistory = new Queue<string>();
        WeaponType? mLastWeapon;
        float mIdleTime;

        public StyleRankMeter(StyleRankConfig iConfig)
        {
            mConfig = iConfig;
        }

        public float Points { get; private set; }
        public int RankCount => mConfig.RankThresholds.Length;

        public int RankIndex
        {
            get
            {
                for (int aI = mConfig.RankThresholds.Length - 1; aI >= 0; aI--)
                {
                    if (Points >= mConfig.RankThresholds[aI]) return aI;
                }
                return 0;
            }
        }

        public string RankName => mConfig.RankNames[Mathf.Min(RankIndex, mConfig.RankNames.Length - 1)];

        /// <summary>現在のランクの中での進み具合(0〜1)</summary>
        public float RankProgress
        {
            get
            {
                int aRank = RankIndex;
                float aFrom = mConfig.RankThresholds[aRank];
                float aTo = aRank + 1 < mConfig.RankThresholds.Length ? mConfig.RankThresholds[aRank + 1] : mConfig.MaxPoints;
                return aTo <= aFrom ? 1f : Mathf.Clamp01((Points - aFrom) / (aTo - aFrom));
            }
        }

        /// <summary>技のヒットを登録し、実際に加算したポイントを返す。</summary>
        /// <param name="iMoveKey">繰り返し判定に使う技の識別子(スキル名+段数など)</param>
        public float RegisterHit(string iMoveKey, WeaponType iWeapon, float iBasePoints, bool iArsenal)
        {
            int aUses = 0;
            foreach (var aKey in mHistory)
            {
                if (aKey == iMoveKey) aUses++;
            }

            float aMultiplier = Mathf.Max(mConfig.MinRepeatMultiplier, 1f - mConfig.RepeatPenaltyPerUse * aUses);
            if (mLastWeapon.HasValue && mLastWeapon.Value != iWeapon) aMultiplier *= mConfig.WeaponVarietyMultiplier;
            if (iArsenal) aMultiplier *= mConfig.ArsenalMultiplier;

            mHistory.Enqueue(iMoveKey);
            while (mHistory.Count > mConfig.RepeatHistory) mHistory.Dequeue();
            mLastWeapon = iWeapon;

            float aGained = iBasePoints * aMultiplier;
            AddPoints(aGained);
            return aGained;
        }

        public void AddPoints(float iPoints)
        {
            Points = Mathf.Min(mConfig.MaxPoints, Points + iPoints);
            mIdleTime = 0f;
        }

        public void OnDamaged()
        {
            int aTarget = Mathf.Max(0, RankIndex - mConfig.RanksLostOnDamage);
            Points = mConfig.RankThresholds[aTarget];
            mHistory.Clear();
            mLastWeapon = null;
        }

        public void Tick(float iDeltaTime)
        {
            mIdleTime += iDeltaTime;
            if (mIdleTime < mConfig.IdleDelay) return;
            int aRank = Mathf.Min(RankIndex, mConfig.DecayPerSecond.Length - 1);
            Points = Mathf.Max(0f, Points - mConfig.DecayPerSecond[aRank] * iDeltaTime);
        }
    }
}
