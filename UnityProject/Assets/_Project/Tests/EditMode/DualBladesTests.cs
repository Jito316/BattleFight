using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>双剣のデータ: 武器種・3 スロットとフィニッシャーの技・編成の一覧・プレイヤーの武器の一覧</summary>
    public class DualBladesTests
    {
        static T Load<T>(string iPath) where T : Object
        {
            var aAsset = AssetDatabase.LoadAssetAtPath<T>(iPath);
            Assert.NotNull(aAsset, $"{iPath} がない");
            return aAsset;
        }

        static SkillData Skill(string iName) => Load<SkillData>($"Assets/_Project/Data/Skills/{iName}.asset");

        [Test]
        public void Weapon_HasAFinisher_AndTwoBlades()
        {
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_DualBlades.asset");
            Assert.AreEqual(WeaponType.DualBlades, aWeapon.Weapon);
            Assert.AreEqual(Skill("Skill_Dual_Finisher"), aWeapon.MasteryFinisher, "マスタリーのフィニッシャーがつながっていない");
            Assert.NotNull(aWeapon.Prefab, "武器のプレハブがつながっていない");
            Assert.NotNull(aWeapon.Prefab.transform.Find("Head2"), "プレハブに 2 本目の刃がない");
        }

        [Test]
        public void Skills_FillEverySlot()
        {
            var aCombo = Skill("Skill_Dual_Combo");
            var aSpin = Skill("Skill_Dual_Spin");
            var aCross = Skill("Skill_Dual_Cross");
            var aFinisher = Skill("Skill_Dual_Finisher");
            Assert.AreEqual(SlotType.AttackA, aCombo.Slot);
            Assert.AreEqual(SlotType.AttackB, aSpin.Slot);
            Assert.AreEqual(SlotType.Movement, aCross.Slot);
            foreach (var aSkill in new[] { aCombo, aSpin, aCross, aFinisher }) Assert.AreEqual(WeaponType.DualBlades, aSkill.Weapon, aSkill.name);
            Assert.IsTrue(aSpin.AutoChain, "回転斬りは一度押すと最後まで回る");
            Assert.IsTrue(aCross.BlinkBehindTarget, "交差ステップが背後へ回り込まない");
            Assert.Greater(aCross.InvulnerableFrames, 0, "交差ステップに無敵がない");
        }

        [Test]
        public void Skills_AreFastButLight_ComparedWithTheSword()
        {
            // 双剣らしさ: 剣の連斬より出が速く、1 発のひるみは小さい
            var aDual = Skill("Skill_Dual_Combo").GetStep(0);
            var aSword = Skill("Skill_Sword_Combo").GetStep(0);
            Assert.Less(aDual.StartupFrames, aSword.StartupFrames, "双剣の出が剣より遅い");
            Assert.Less(aDual.Stagger, aSword.Stagger, "双剣の 1 発のひるみが剣より大きい");
        }

        [Test]
        public void LoadoutList_AndPlayer_IncludeDualBlades()
        {
            var aDatabase = Load<SkillDatabase>("Assets/_Project/Data/SkillDatabase.asset");
            foreach (var aName in new[] { "Skill_Dual_Combo", "Skill_Dual_Spin", "Skill_Dual_Cross" })
            {
                Assert.IsTrue(aDatabase.Skills.Contains(Skill(aName)), $"{aName} が編成の一覧にない");
            }
            Assert.IsFalse(aDatabase.Skills.Contains(Skill("Skill_Dual_Finisher")), "フィニッシャーは編成の一覧に入れない");

            var aPlayer = Load<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_DualBlades.asset");
            Assert.IsTrue(aPlayer.GetComponent<SkillSlotController>().Weapons.Contains(aWeapon), "プレイヤーの武器の一覧に双剣がない");
        }
    }
}
