using NUnit.Framework;

namespace BattleFight.Tests
{
    public class WeaponBonusResolverTests
    {
        [Test]
        public void AllSame_IsMastery()
        {
            var aBonus = WeaponBonusResolver.Resolve(WeaponType.Hammer, WeaponType.Hammer, WeaponType.Hammer);

            Assert.AreEqual(WeaponBonusKind.Mastery, aBonus.Kind);
            Assert.AreEqual(WeaponType.Hammer, aBonus.Weapon);
        }

        [TestCase(WeaponType.Sword, WeaponType.Sword, WeaponType.Chain, WeaponType.Sword)]
        [TestCase(WeaponType.Sword, WeaponType.Chain, WeaponType.Sword, WeaponType.Sword)]
        [TestCase(WeaponType.Sword, WeaponType.Chain, WeaponType.Chain, WeaponType.Chain)]
        public void TwoSame_IsSynergyForThePair(WeaponType iA, WeaponType iB, WeaponType iM, WeaponType iExpected)
        {
            var aBonus = WeaponBonusResolver.Resolve(iA, iB, iM);

            Assert.AreEqual(WeaponBonusKind.Synergy, aBonus.Kind);
            Assert.AreEqual(iExpected, aBonus.Weapon);
            Assert.IsTrue(aBonus.Boosts(iExpected));
        }

        [Test]
        public void AllDifferent_IsArsenal_AndBoostsNothing()
        {
            var aBonus = WeaponBonusResolver.Resolve(WeaponType.Sword, WeaponType.Hammer, WeaponType.Chain);

            Assert.AreEqual(WeaponBonusKind.Arsenal, aBonus.Kind);
            Assert.IsFalse(aBonus.Boosts(WeaponType.Sword));
            Assert.IsFalse(aBonus.Boosts(WeaponType.Hammer));
        }
    }
}
