using System;
using UnityEngine;

namespace BattleFight
{
    /// <summary>3スロット × 最大3候補のラックを持ち、戦闘中のリアルタイム切り替えを受け付ける。</summary>
    public class SkillSlotController : MonoBehaviour
    {
        [SerializeField] SkillData[] attackARack = new SkillData[SkillRack.Capacity];
        [SerializeField] SkillData[] attackBRack = new SkillData[SkillRack.Capacity];
        [SerializeField] SkillData[] movementRack = new SkillData[SkillRack.Capacity];
        [SerializeField] WeaponTypeData[] weaponDatabase;

        SkillRack[] racks;

        public event Action<SlotType> SlotChanged;

        public WeaponBonus Bonus { get; private set; }

        /// <summary>マスタリー中ならその武器種のフィニッシャー、それ以外は null</summary>
        public SkillData MasteryFinisher
        {
            get
            {
                if (Bonus.Kind != WeaponBonusKind.Mastery) return null;
                var data = GetWeaponData(Bonus.Weapon);
                return data != null ? data.masteryFinisher : null;
            }
        }

        void Awake()
        {
            racks = new[]
            {
                new SkillRack(attackARack),
                new SkillRack(attackBRack),
                new SkillRack(movementRack),
            };
            RecomputeBonus();
        }

        public SkillRack GetRack(SlotType slot) => racks[(int)slot];

        public SkillData GetCurrent(SlotType slot) => racks[(int)slot].Current;

        public bool Cycle(SlotType slot)
        {
            if (!racks[(int)slot].Cycle()) return false;
            RecomputeBonus();
            SlotChanged?.Invoke(slot);
            return true;
        }

        public WeaponTypeData GetWeaponData(WeaponType weapon)
        {
            if (weaponDatabase == null) return null;
            foreach (var data in weaponDatabase)
            {
                if (data != null && data.weapon == weapon) return data;
            }
            return null;
        }

        void RecomputeBonus()
        {
            var a = GetCurrent(SlotType.AttackA);
            var b = GetCurrent(SlotType.AttackB);
            var m = GetCurrent(SlotType.Movement);
            Bonus = a != null && b != null && m != null
                ? WeaponBonusResolver.Resolve(a.weapon, b.weapon, m.weapon)
                : WeaponBonus.None;
        }
    }
}
