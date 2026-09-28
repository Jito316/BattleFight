using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>スタイルランクの計算。MonoBehaviour に依存しないのでテストできる。</summary>
    public class StyleRankMeter
    {
        readonly StyleRankConfig config;
        readonly Queue<string> history = new Queue<string>();
        WeaponType? lastWeapon;
        float idleTime;

        public StyleRankMeter(StyleRankConfig config)
        {
            this.config = config;
        }

        public float Points { get; private set; }
        public int RankCount => config.rankThresholds.Length;

        public int RankIndex
        {
            get
            {
                for (int i = config.rankThresholds.Length - 1; i >= 0; i--)
                {
                    if (Points >= config.rankThresholds[i]) return i;
                }
                return 0;
            }
        }

        public string RankName => config.rankNames[Mathf.Min(RankIndex, config.rankNames.Length - 1)];

        /// <summary>現在のランクの中での進み具合(0〜1)</summary>
        public float RankProgress
        {
            get
            {
                int rank = RankIndex;
                float from = config.rankThresholds[rank];
                float to = rank + 1 < config.rankThresholds.Length ? config.rankThresholds[rank + 1] : config.maxPoints;
                return to <= from ? 1f : Mathf.Clamp01((Points - from) / (to - from));
            }
        }

        /// <summary>技のヒットを登録し、実際に加算したポイントを返す。</summary>
        /// <param name="moveKey">繰り返し判定に使う技の識別子(スキル名+段数など)</param>
        public float RegisterHit(string moveKey, WeaponType weapon, float basePoints, bool arsenal)
        {
            int uses = 0;
            foreach (var key in history)
            {
                if (key == moveKey) uses++;
            }

            float multiplier = Mathf.Max(config.minRepeatMultiplier, 1f - config.repeatPenaltyPerUse * uses);
            if (lastWeapon.HasValue && lastWeapon.Value != weapon) multiplier *= config.weaponVarietyMultiplier;
            if (arsenal) multiplier *= config.arsenalMultiplier;

            history.Enqueue(moveKey);
            while (history.Count > config.repeatHistory) history.Dequeue();
            lastWeapon = weapon;

            float gained = basePoints * multiplier;
            AddPoints(gained);
            return gained;
        }

        public void AddPoints(float points)
        {
            Points = Mathf.Min(config.maxPoints, Points + points);
            idleTime = 0f;
        }

        public void OnDamaged()
        {
            int target = Mathf.Max(0, RankIndex - config.ranksLostOnDamage);
            Points = config.rankThresholds[target];
            history.Clear();
            lastWeapon = null;
        }

        public void Tick(float deltaTime)
        {
            idleTime += deltaTime;
            if (idleTime < config.idleDelay) return;
            int rank = Mathf.Min(RankIndex, config.decayPerSecond.Length - 1);
            Points = Mathf.Max(0f, Points - config.decayPerSecond[rank] * deltaTime);
        }
    }
}
