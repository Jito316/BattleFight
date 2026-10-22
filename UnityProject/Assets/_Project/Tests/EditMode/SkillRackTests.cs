using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class SkillRackTests
    {
        static SkillData Skill(string iName, WeaponType iWeapon = WeaponType.Sword)
        {
            var aSkill = ScriptableObject.CreateInstance<SkillData>();
            aSkill.name = iName;
            aSkill.Weapon = iWeapon;
            return aSkill;
        }

        [Test]
        public void Cycle_WrapsAroundThroughAllCandidates()
        {
            var aFirst = Skill("a");
            var aSecond = Skill("b");
            var aThird = Skill("c");
            var aRack = new SkillRack(new[] { aFirst, aSecond, aThird });

            Assert.AreSame(aFirst, aRack.Current);
            Assert.AreSame(aSecond, aRack.Next);
            Assert.IsTrue(aRack.Cycle());
            Assert.AreSame(aSecond, aRack.Current);
            aRack.Cycle();
            Assert.AreSame(aThird, aRack.Current);
            Assert.AreSame(aFirst, aRack.Next);
            aRack.Cycle();
            Assert.AreSame(aFirst, aRack.Current);
        }

        [Test]
        public void Constructor_SkipsNullAndLimitsToCapacity()
        {
            var aRack = new SkillRack(new[] { Skill("a"), null, Skill("b"), Skill("c"), Skill("d") });

            Assert.AreEqual(SkillRack.Capacity, aRack.Count);
            Assert.AreEqual("c", aRack.Skills[2].name);
        }

        [Test]
        public void Cycle_WithSingleCandidate_DoesNothing()
        {
            var aRack = new SkillRack(new[] { Skill("a") });

            Assert.IsFalse(aRack.Cycle());
            Assert.AreEqual("a", aRack.Current.name);
        }

        [Test]
        public void EmptyRack_HasNoCurrent()
        {
            var aRack = new SkillRack(null);

            Assert.IsNull(aRack.Current);
            Assert.IsFalse(aRack.Cycle());
        }
    }
}
