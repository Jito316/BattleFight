using System;

namespace BattleFight
{
    /// <summary>3スロット分のスキルの組み合わせ。プリセット方式ではこれを一括で切り替える。</summary>
    [Serializable]
    public class SkillPreset
    {
        public string name = "プリセット";
        public SkillData attackA;
        public SkillData attackB;
        public SkillData movement;

        public SkillPreset() { }

        public SkillPreset(string name, SkillData attackA, SkillData attackB, SkillData movement)
        {
            this.name = name;
            this.attackA = attackA;
            this.attackB = attackB;
            this.movement = movement;
        }

        public SkillData Get(SlotType slot) => slot switch
        {
            SlotType.AttackA => attackA,
            SlotType.AttackB => attackB,
            _ => movement,
        };

        /// <summary>スロットの種類が合わないスキルは入れない</summary>
        public bool Set(SlotType slot, SkillData skill)
        {
            if (skill != null && skill.slot != slot) return false;
            switch (slot)
            {
                case SlotType.AttackA: attackA = skill; break;
                case SlotType.AttackB: attackB = skill; break;
                default: movement = skill; break;
            }
            return true;
        }

        public WeaponBonus Bonus =>
            attackA != null && attackB != null && movement != null
                ? WeaponBonusResolver.Resolve(attackA.weapon, attackB.weapon, movement.weapon)
                : WeaponBonus.None;

        /// <summary>3スロットとも空(探索でまだスキルが足りないプリセット)</summary>
        public bool IsEmpty => attackA == null && attackB == null && movement == null;

        public SkillPreset Clone() => new SkillPreset(name, attackA, attackB, movement);
    }
}
