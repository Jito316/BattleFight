using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    public class LoadoutTests
    {
        readonly List<Object> created = new List<Object>();
        SkillDatabase database;

        SkillData Skill(string id, SlotType slot, WeaponType weapon)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.name = id;
            skill.slot = slot;
            skill.weapon = weapon;
            created.Add(skill);
            database.skills.Add(skill);
            return skill;
        }

        [SetUp]
        public void SetUp()
        {
            database = ScriptableObject.CreateInstance<SkillDatabase>();
            created.Add(database);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Preset_RejectsSkillForAnotherSlot()
        {
            var attack = Skill("a", SlotType.AttackA, WeaponType.Sword);
            var preset = new SkillPreset();

            Assert.IsFalse(preset.Set(SlotType.Movement, attack));
            Assert.IsNull(preset.movement);
            Assert.IsTrue(preset.Set(SlotType.AttackA, attack));
            Assert.AreSame(attack, preset.Get(SlotType.AttackA));
        }

        [Test]
        public void Preset_Bonus_UsesWeaponsOfAllSlots()
        {
            var preset = new SkillPreset("p",
                Skill("a", SlotType.AttackA, WeaponType.Staff),
                Skill("b", SlotType.AttackB, WeaponType.Staff),
                Skill("m", SlotType.Movement, WeaponType.Staff));
            Assert.AreEqual(WeaponBonusKind.Mastery, preset.Bonus.Kind);
            Assert.AreEqual(WeaponType.Staff, preset.Bonus.Weapon);

            preset.Set(SlotType.Movement, null);
            Assert.AreEqual(WeaponBonusKind.None, preset.Bonus.Kind);
        }

        [Test]
        public void Storage_RoundTripsModeIndexAndSkills()
        {
            var a = Skill("gun_a", SlotType.AttackA, WeaponType.Gun);
            var b = Skill("staff_b", SlotType.AttackB, WeaponType.Staff);
            var m = Skill("chain_m", SlotType.Movement, WeaponType.Chain);
            var presets = new List<SkillPreset> { new SkillPreset("one", a, b, m), new SkillPreset("two", a, null, m) };

            string json = LoadoutStorage.Serialize(SwapMode.Preset, 1, presets);
            Assert.IsTrue(LoadoutStorage.TryDeserialize(json, database, out var loaded));

            Assert.AreEqual(SwapMode.Preset, loaded.mode);
            Assert.AreEqual(1, loaded.presetIndex);
            Assert.AreEqual(2, loaded.presets.Count);
            Assert.AreEqual("one", loaded.presets[0].name);
            Assert.AreSame(a, loaded.presets[0].attackA);
            Assert.AreSame(b, loaded.presets[0].attackB);
            Assert.AreSame(m, loaded.presets[0].movement);
            Assert.IsNull(loaded.presets[1].attackB);
        }

        [Test]
        public void Storage_MissingSkillsBecomeNull()
        {
            var a = Skill("gone", SlotType.AttackA, WeaponType.Gun);
            string json = LoadoutStorage.Serialize(SwapMode.Rack, 0, new List<SkillPreset> { new SkillPreset("p", a, null, null) });
            database.skills.Clear();

            Assert.IsTrue(LoadoutStorage.TryDeserialize(json, database, out var loaded));
            Assert.IsNull(loaded.presets[0].attackA);
        }

        [Test]
        public void Storage_RejectsBrokenData()
        {
            Assert.IsFalse(LoadoutStorage.TryDeserialize("", database, out _));
            Assert.IsFalse(LoadoutStorage.TryDeserialize("{not json", database, out _));
        }

        [Test]
        public void Database_FindsByAssetName_AndFiltersBySlot()
        {
            var a = Skill("x", SlotType.AttackA, WeaponType.Sword);
            Skill("y", SlotType.Movement, WeaponType.Sword);

            Assert.AreSame(a, database.Find("x"));
            Assert.IsNull(database.Find("nothing"));
            CollectionAssert.AreEqual(new[] { a }, new List<SkillData>(database.ForSlot(SlotType.AttackA)));
        }
    }
}
