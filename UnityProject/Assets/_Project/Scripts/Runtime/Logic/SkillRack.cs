using System.Collections.Generic;

namespace BattleFight
{
    /// <summary>1スロット分のスキル候補(最大3つ)。戦闘中は順送りで切り替える。</summary>
    public class SkillRack
    {
        public const int Capacity = 3;

        readonly List<SkillData> mSkills = new List<SkillData>(Capacity);
        int mIndex;

        public SkillRack(IEnumerable<SkillData> iSource)
        {
            if (iSource == null) return;
            foreach (var aSkill in iSource)
            {
                if (aSkill == null) continue;
                if (mSkills.Count >= Capacity) break;
                mSkills.Add(aSkill);
            }
        }

        public int Count => mSkills.Count;
        public int CurrentIndex => mIndex;
        public IReadOnlyList<SkillData> Skills => mSkills;
        public SkillData Current => mSkills.Count == 0 ? null : mSkills[mIndex];
        public SkillData Next => mSkills.Count == 0 ? null : mSkills[(mIndex + 1) % mSkills.Count];

        /// <summary>次の候補へ切り替える。候補が1つ以下なら何もしない。</summary>
        public bool Cycle()
        {
            if (mSkills.Count <= 1) return false;
            mIndex = (mIndex + 1) % mSkills.Count;
            return true;
        }
    }
}
