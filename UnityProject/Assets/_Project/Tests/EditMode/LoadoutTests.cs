using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class LoadoutTests
    {
        readonly List<Object> mCreated = new List<Object>();
        SkillDatabase mDatabase;

        SkillData Skill(string iId, SlotType iSlot, WeaponType iWeapon)
        {
            var aSkill = ScriptableObject.CreateInstance<SkillData>();
            aSkill.name = iId;
            aSkill.Slot = iSlot;
            aSkill.Weapon = iWeapon;
            mCreated.Add(aSkill);
            mDatabase.Skills.Add(aSkill);
            return aSkill;
        }

        [SetUp]
        public void SetUp()
        {
            mDatabase = ScriptableObject.CreateInstance<SkillDatabase>();
            mCreated.Add(mDatabase);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var aCreated in mCreated) Object.DestroyImmediate(aCreated);
            mCreated.Clear();
        }

        [Test]
        public void Preset_RejectsSkillForAnotherSlot()
        {
            var aAttack = Skill("a", SlotType.AttackA, WeaponType.Sword);
            var aPreset = new SkillPreset();

            Assert.IsFalse(aPreset.Set(SlotType.Movement, aAttack));
            Assert.IsNull(aPreset.Movement);
            Assert.IsTrue(aPreset.Set(SlotType.AttackA, aAttack));
            Assert.AreSame(aAttack, aPreset.Get(SlotType.AttackA));
        }

        [Test]
        public void Preset_Bonus_UsesWeaponsOfAllSlots()
        {
            var aPreset = new SkillPreset("p",
                Skill("a", SlotType.AttackA, WeaponType.Staff),
                Skill("b", SlotType.AttackB, WeaponType.Staff),
                Skill("m", SlotType.Movement, WeaponType.Staff));
            Assert.AreEqual(WeaponBonusKind.Mastery, aPreset.Bonus.Kind);
            Assert.AreEqual(WeaponType.Staff, aPreset.Bonus.Weapon);

            aPreset.Set(SlotType.Movement, null);
            Assert.AreEqual(WeaponBonusKind.None, aPreset.Bonus.Kind);
        }

        [Test]
        public void Storage_RoundTripsModeIndexAndSkills()
        {
            var aAttackA = Skill("gun_a", SlotType.AttackA, WeaponType.Gun);
            var aAttackB = Skill("staff_b", SlotType.AttackB, WeaponType.Staff);
            var aMovement = Skill("chain_m", SlotType.Movement, WeaponType.Chain);
            var aPresets = new List<SkillPreset> { new SkillPreset("one", aAttackA, aAttackB, aMovement), new SkillPreset("two", aAttackA, null, aMovement) };

            string aJson = LoadoutStorage.Serialize(SwapMode.Preset, 1, aPresets);
            Assert.IsTrue(LoadoutStorage.TryDeserialize(aJson, mDatabase, out var aLoaded));

            Assert.AreEqual(SwapMode.Preset, aLoaded.Mode);
            Assert.AreEqual(1, aLoaded.PresetIndex);
            Assert.AreEqual(2, aLoaded.Presets.Count);
            Assert.AreEqual("one", aLoaded.Presets[0].Name);
            Assert.AreSame(aAttackA, aLoaded.Presets[0].AttackA);
            Assert.AreSame(aAttackB, aLoaded.Presets[0].AttackB);
            Assert.AreSame(aMovement, aLoaded.Presets[0].Movement);
            Assert.IsNull(aLoaded.Presets[1].AttackB);
        }

        [Test]
        public void Storage_MissingSkillsBecomeNull()
        {
            var aAttackA = Skill("gone", SlotType.AttackA, WeaponType.Gun);
            string aJson = LoadoutStorage.Serialize(SwapMode.Rack, 0, new List<SkillPreset> { new SkillPreset("p", aAttackA, null, null) });
            mDatabase.Skills.Clear();

            Assert.IsTrue(LoadoutStorage.TryDeserialize(aJson, mDatabase, out var aLoaded));
            Assert.IsNull(aLoaded.Presets[0].AttackA);
        }

        [Test]
        public void Storage_RejectsBrokenData()
        {
            Assert.IsFalse(LoadoutStorage.TryDeserialize("", mDatabase, out _));
            Assert.IsFalse(LoadoutStorage.TryDeserialize("{not json", mDatabase, out _));
        }

        [Test]
        public void Database_FindsByAssetName_AndFiltersBySlot()
        {
            var aAttackA = Skill("x", SlotType.AttackA, WeaponType.Sword);
            Skill("y", SlotType.Movement, WeaponType.Sword);

            Assert.AreSame(aAttackA, mDatabase.Find("x"));
            Assert.IsNull(mDatabase.Find("nothing"));
            CollectionAssert.AreEqual(new[] { aAttackA }, new List<SkillData>(mDatabase.ForSlot(SlotType.AttackA)));
        }
    }
}
