using NUnit.Framework;

namespace BattleFight.Tests
{
    public class WeaponBonusResolverTests
    {
        [Test]
        public void AllSame_IsMastery()
        {
            var bonus = WeaponBonusResolver.Resolve(WeaponType.Hammer, WeaponType.Hammer, WeaponType.Hammer);

            Assert.AreEqual(WeaponBonusKind.Mastery, bonus.Kind);
            Assert.AreEqual(WeaponType.Hammer, bonus.Weapon);
        }

        [TestCase(WeaponType.Sword, WeaponType.Sword, WeaponType.Chain, WeaponType.Sword)]
        [TestCase(WeaponType.Sword, WeaponType.Chain, WeaponType.Sword, WeaponType.Sword)]
        [TestCase(WeaponType.Sword, WeaponType.Chain, WeaponType.Chain, WeaponType.Chain)]
        public void TwoSame_IsSynergyForThePair(WeaponType a, WeaponType b, WeaponType m, WeaponType expected)
        {
            var bonus = WeaponBonusResolver.Resolve(a, b, m);

            Assert.AreEqual(WeaponBonusKind.Synergy, bonus.Kind);
            Assert.AreEqual(expected, bonus.Weapon);
            Assert.IsTrue(bonus.Boosts(expected));
        }

        [Test]
        public void AllDifferent_IsArsenal_AndBoostsNothing()
        {
            var bonus = WeaponBonusResolver.Resolve(WeaponType.Sword, WeaponType.Hammer, WeaponType.Chain);

            Assert.AreEqual(WeaponBonusKind.Arsenal, bonus.Kind);
            Assert.IsFalse(bonus.Boosts(WeaponType.Sword));
            Assert.IsFalse(bonus.Boosts(WeaponType.Hammer));
        }
    }
}
