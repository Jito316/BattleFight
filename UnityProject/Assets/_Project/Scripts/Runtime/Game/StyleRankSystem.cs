using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>StyleRankMeter のシーン側の窓口。アナウンス(SWAP STRIKE! など)も持つ。</summary>
    public class StyleRankSystem : MonoBehaviour
    {
        public struct Announcement
        {
            public string Text;
            public Color Color;
            public float Time;
        }

        public const float AnnouncementLifetime = 1.4f;

        [SerializeField, FormerlySerializedAs("config")] StyleRankConfig mConfig;

        public StyleRankConfig Config => mConfig;
        public StyleRankMeter Meter { get; private set; }
        public int HighestRank { get; private set; }
        public List<Announcement> Announcements { get; } = new List<Announcement>();

        public float RewardMultiplier =>
            mConfig.RewardMultiplier[Mathf.Min(Meter.RankIndex, mConfig.RewardMultiplier.Length - 1)];

        void Awake()
        {
            if (mConfig == null) mConfig = ScriptableObject.CreateInstance<StyleRankConfig>();
            Meter = new StyleRankMeter(mConfig);
        }

        void Update()
        {
            Meter.Tick(Time.deltaTime);
            Announcements.RemoveAll(iAnnouncement => Time.unscaledTime - iAnnouncement.Time > AnnouncementLifetime);
        }

        public void RegisterHit(string iMoveKey, WeaponType iWeapon, float iBasePoints, bool iArsenal)
        {
            Meter.RegisterHit(iMoveKey, iWeapon, iBasePoints, iArsenal);
            TrackHighest();
        }

        public void AddBonus(float iPoints)
        {
            Meter.AddPoints(iPoints);
            TrackHighest();
        }

        public void AddBonus(float iPoints, string iLabel, Color iColor)
        {
            AddBonus(iPoints);
            Announce(iLabel, iColor);
        }

        public void Announce(string iText, Color iColor)
        {
            Announcements.Add(new Announcement { Text = iText, Color = iColor, Time = Time.unscaledTime });
            if (Announcements.Count > 5) Announcements.RemoveAt(0);
        }

        public void OnDamaged() => Meter.OnDamaged();

        void TrackHighest() => HighestRank = Mathf.Max(HighestRank, Meter.RankIndex);
    }
}
