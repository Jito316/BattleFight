using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class SkillRackTests
    {
        static SkillData Skill(string name, WeaponType weapon = WeaponType.Sword)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.name = name;
            skill.weapon = weapon;
            return skill;
        }

        [Test]
        public void Cycle_WrapsAroundThroughAllCandidates()
        {
            var a = Skill("a");
            var b = Skill("b");
            var c = Skill("c");
            var rack = new SkillRack(new[] { a, b, c });

            Assert.AreSame(a, rack.Current);
            Assert.AreSame(b, rack.Next);
            Assert.IsTrue(rack.Cycle());
            Assert.AreSame(b, rack.Current);
            rack.Cycle();
            Assert.AreSame(c, rack.Current);
            Assert.AreSame(a, rack.Next);
            rack.Cycle();
            Assert.AreSame(a, rack.Current);
        }

        [Test]
        public void Constructor_SkipsNullAndLimitsToCapacity()
        {
            var rack = new SkillRack(new[] { Skill("a"), null, Skill("b"), Skill("c"), Skill("d") });

            Assert.AreEqual(SkillRack.Capacity, rack.Count);
            Assert.AreEqual("c", rack.Skills[2].name);
        }

        [Test]
        public void Cycle_WithSingleCandidate_DoesNothing()
        {
            var rack = new SkillRack(new[] { Skill("a") });

            Assert.IsFalse(rack.Cycle());
            Assert.AreEqual("a", rack.Current.name);
        }

        [Test]
        public void EmptyRack_HasNoCurrent()
        {
            var rack = new SkillRack(null);

            Assert.IsNull(rack.Current);
            Assert.IsFalse(rack.Cycle());
        }
    }
}
