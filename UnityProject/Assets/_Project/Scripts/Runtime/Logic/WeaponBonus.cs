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

        public WeaponBonus(WeaponBonusKind kind, WeaponType weapon)
        {
            Kind = kind;
            Weapon = weapon;
        }

        /// <summary>この武器種の技に Mastery / Synergy のボーナスが乗るか</summary>
        public bool Boosts(WeaponType weapon) =>
            (Kind == WeaponBonusKind.Mastery || Kind == WeaponBonusKind.Synergy) && Weapon == weapon;
    }

    public static class WeaponBonusResolver
    {
        public static WeaponBonus Resolve(WeaponType attackA, WeaponType attackB, WeaponType movement)
        {
            if (attackA == attackB && attackB == movement) return new WeaponBonus(WeaponBonusKind.Mastery, attackA);
            if (attackA == attackB || attackA == movement) return new WeaponBonus(WeaponBonusKind.Synergy, attackA);
            if (attackB == movement) return new WeaponBonus(WeaponBonusKind.Synergy, attackB);
            return new WeaponBonus(WeaponBonusKind.Arsenal, attackA);
        }
    }
}
