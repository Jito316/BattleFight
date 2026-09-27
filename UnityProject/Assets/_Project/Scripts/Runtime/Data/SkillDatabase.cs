using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>編成で選べるスキルの一覧。フィニッシャーは武器種データから参照するので含めない。</summary>
    [CreateAssetMenu(menuName = "BattleFight/Skill Database", fileName = "SkillDatabase")]
    public class SkillDatabase : ScriptableObject
    {
        public List<SkillData> skills = new List<SkillData>();

        /// <summary>保存データから引くときの識別子はアセット名</summary>
        public SkillData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var skill in skills)
            {
                if (skill != null && skill.name == id) return skill;
            }
            return null;
        }

        public IEnumerable<SkillData> ForSlot(SlotType slot)
        {
            foreach (var skill in skills)
            {
                if (skill != null && skill.slot == slot) yield return skill;
            }
        }
    }
}
