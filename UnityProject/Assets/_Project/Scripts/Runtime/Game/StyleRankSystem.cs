using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>StyleRankMeter のシーン側の窓口。アナウンス(SWAP STRIKE! など)も持つ。</summary>
    public class StyleRankSystem : MonoBehaviour
    {
        public struct Announcement
        {
            public string text;
            public Color color;
            public float time;
        }

        public const float AnnouncementLifetime = 1.4f;

        [SerializeField] StyleRankConfig config;

        public StyleRankConfig Config => config;
        public StyleRankMeter Meter { get; private set; }
        public int HighestRank { get; private set; }
        public List<Announcement> Announcements { get; } = new List<Announcement>();

        public float RewardMultiplier =>
            config.rewardMultiplier[Mathf.Min(Meter.RankIndex, config.rewardMultiplier.Length - 1)];

        void Awake()
        {
            if (config == null) config = ScriptableObject.CreateInstance<StyleRankConfig>();
            Meter = new StyleRankMeter(config);
        }

        void Update()
        {
            Meter.Tick(Time.deltaTime);
            Announcements.RemoveAll(a => Time.unscaledTime - a.time > AnnouncementLifetime);
        }

        public void RegisterHit(string moveKey, WeaponType weapon, float basePoints, bool arsenal)
        {
            Meter.RegisterHit(moveKey, weapon, basePoints, arsenal);
            TrackHighest();
        }

        public void AddBonus(float points)
        {
            Meter.AddPoints(points);
            TrackHighest();
        }

        public void AddBonus(float points, string label, Color color)
        {
            AddBonus(points);
            Announce(label, color);
        }

        public void Announce(string text, Color color)
        {
            Announcements.Add(new Announcement { text = text, color = color, time = Time.unscaledTime });
            if (Announcements.Count > 5) Announcements.RemoveAt(0);
        }

        public void OnDamaged() => Meter.OnDamaged();

        void TrackHighest() => HighestRank = Mathf.Max(HighestRank, Meter.RankIndex);
    }
}
