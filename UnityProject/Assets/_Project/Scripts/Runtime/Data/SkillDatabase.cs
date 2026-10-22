using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>編成で選べるスキルの一覧。フィニッシャーは武器種データから参照するので含めない。</summary>
    [CreateAssetMenu(menuName = "BattleFight/Skill Database", fileName = "SkillDatabase")]
    public class SkillDatabase : ScriptableObject
    {
        [FormerlySerializedAs("skills")] public List<SkillData> Skills = new List<SkillData>();

        /// <summary>保存データから引くときの識別子はアセット名</summary>
        public SkillData Find(string iId)
        {
            if (string.IsNullOrEmpty(iId)) return null;
            foreach (var aSkill in Skills)
            {
                if (aSkill != null && aSkill.name == iId) return aSkill;
            }
            return null;
        }

        public IEnumerable<SkillData> ForSlot(SlotType iSlot)
        {
            foreach (var aSkill in Skills)
            {
                if (aSkill != null && aSkill.Slot == iSlot) yield return aSkill;
            }
        }
    }
}
