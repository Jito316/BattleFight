namespace BattleFight
{
    public enum WeaponBonusKind
    {
        None,
        /// <summary>3スロットすべて同じ武器種</summary>
        Mastery,
        /// <summary>2スロットが同じ武器種</summary>
        Synergy,
        /// <summary>3スロットすべて違う武器種</summary>
        Arsenal,
    }

    public readonly struct WeaponBonus
    {
        public static readonly WeaponBonus None = new WeaponBonus(WeaponBonusKind.None, default);

        public readonly WeaponBonusKind Kind;
        /// <summary>Mastery / Synergy の対象の武器種</summary>
        public readonly WeaponType Weapon;

        public WeaponBonus(WeaponBonusKind iKind, WeaponType iWeapon)
        {
            Kind = iKind;
            Weapon = iWeapon;
        }

        /// <summary>この武器種の技に Mastery / Synergy のボーナスが乗るか</summary>
        public bool Boosts(WeaponType iWeapon) =>
            (Kind == WeaponBonusKind.Mastery || Kind == WeaponBonusKind.Synergy) && Weapon == iWeapon;
    }

    public static class WeaponBonusResolver
    {
        public static WeaponBonus Resolve(WeaponType iAttackA, WeaponType iAttackB, WeaponType iMovement)
        {
            if (iAttackA == iAttackB && iAttackB == iMovement) return new WeaponBonus(WeaponBonusKind.Mastery, iAttackA);
            if (iAttackA == iAttackB || iAttackA == iMovement) return new WeaponBonus(WeaponBonusKind.Synergy, iAttackA);
            if (iAttackB == iMovement) return new WeaponBonus(WeaponBonusKind.Synergy, iAttackB);
            return new WeaponBonus(WeaponBonusKind.Arsenal, iAttackA);
        }
    }
}
