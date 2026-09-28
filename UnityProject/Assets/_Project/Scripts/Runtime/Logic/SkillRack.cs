using System.Collections.Generic;

namespace BattleFight
{
    /// <summary>1スロット分のスキル候補(最大3つ)。戦闘中は順送りで切り替える。</summary>
    public class SkillRack
    {
        public const int Capacity = 3;

        readonly List<SkillData> skills = new List<SkillData>(Capacity);
        int index;

        public SkillRack(IEnumerable<SkillData> source)
        {
            if (source == null) return;
            foreach (var skill in source)
            {
                if (skill == null) continue;
                if (skills.Count >= Capacity) break;
                skills.Add(skill);
            }
        }

        public int Count => skills.Count;
        public int CurrentIndex => index;
        public IReadOnlyList<SkillData> Skills => skills;
        public SkillData Current => skills.Count == 0 ? null : skills[index];
        public SkillData Next => skills.Count == 0 ? null : skills[(index + 1) % skills.Count];

        /// <summary>次の候補へ切り替える。候補が1つ以下なら何もしない。</summary>
        public bool Cycle()
        {
            if (skills.Count <= 1) return false;
            index = (index + 1) % skills.Count;
            return true;
        }
    }
}
