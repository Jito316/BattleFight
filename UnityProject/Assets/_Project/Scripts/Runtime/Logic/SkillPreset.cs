using System;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>3スロット分のスキルの組み合わせ。プリセット方式ではこれを一括で切り替える。</summary>
    [Serializable]
    public class SkillPreset
    {
        [FormerlySerializedAs("name")] public string Name = "プリセット";
        [FormerlySerializedAs("attackA")] public SkillData AttackA;
        [FormerlySerializedAs("attackB")] public SkillData AttackB;
        [FormerlySerializedAs("movement")] public SkillData Movement;

        public SkillPreset() { }

        public SkillPreset(string iName, SkillData iAttackA, SkillData iAttackB, SkillData iMovement)
        {
            Name = iName;
            AttackA = iAttackA;
            AttackB = iAttackB;
            Movement = iMovement;
        }

        public SkillData Get(SlotType iSlot) => iSlot switch
        {
            SlotType.AttackA => AttackA,
            SlotType.AttackB => AttackB,
            _ => Movement,
        };

        /// <summary>スロットの種類が合わないスキルは入れない</summary>
        public bool Set(SlotType iSlot, SkillData iSkill)
        {
            if (iSkill != null && iSkill.Slot != iSlot) return false;
            switch (iSlot)
            {
                case SlotType.AttackA: AttackA = iSkill; break;
                case SlotType.AttackB: AttackB = iSkill; break;
                default: Movement = iSkill; break;
            }
            return true;
        }

        public WeaponBonus Bonus =>
            AttackA != null && AttackB != null && Movement != null
                ? WeaponBonusResolver.Resolve(AttackA.Weapon, AttackB.Weapon, Movement.Weapon)
                : WeaponBonus.None;

        /// <summary>3スロットとも空(探索でまだスキルが足りないプリセット)</summary>
        public bool IsEmpty => AttackA == null && AttackB == null && Movement == null;

        public SkillPreset Clone() => new SkillPreset(Name, AttackA, AttackB, Movement);
    }
}
