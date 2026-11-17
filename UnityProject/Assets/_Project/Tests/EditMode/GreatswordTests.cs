using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>大剣のデータ: 武器種・3 スロットとフィニッシャーの技・剣より重い・剛断の溜め・地裂きの長さ・編成の一覧・プレイヤーの武器の一覧</summary>
    public class GreatswordTests
    {
        static T Load<T>(string iPath) where T : Object
        {
            var aAsset = AssetDatabase.LoadAssetAtPath<T>(iPath);
            Assert.NotNull(aAsset, $"{iPath} がない");
            return aAsset;
        }

        static SkillData Skill(string iName) => Load<SkillData>($"Assets/_Project/Data/Skills/{iName}.asset");

        [Test]
        public void Weapon_HasAFinisher_AndAPrefab()
        {
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_Greatsword.asset");
            Assert.AreEqual(WeaponType.Greatsword, aWeapon.Weapon);
            Assert.AreEqual(Skill("Skill_Greatsword_Finisher"), aWeapon.MasteryFinisher, "マスタリーのフィニッシャーがつながっていない");
            Assert.NotNull(aWeapon.Prefab, "武器のプレハブがつながっていない");
            Assert.NotNull(aWeapon.Prefab.GetComponent<WeaponVisual>(), "武器のプレハブに WeaponVisual がない");
        }

        [Test]
        public void Skills_FillEverySlot()
        {
            var aCombo = Skill("Skill_Greatsword_Combo");
            var aCharge = Skill("Skill_Greatsword_Charge");
            var aRift = Skill("Skill_Greatsword_Rift");
            var aTackle = Skill("Skill_Greatsword_Tackle");
            var aFinisher = Skill("Skill_Greatsword_Finisher");
            Assert.AreEqual(SlotType.AttackA, aCombo.Slot);
            Assert.AreEqual(SlotType.AttackB, aCharge.Slot);
            Assert.AreEqual(SlotType.AttackB, aRift.Slot);
            Assert.AreEqual(SlotType.Movement, aTackle.Slot);
            foreach (var aSkill in new[] { aCombo, aCharge, aRift, aTackle, aFinisher }) Assert.AreEqual(WeaponType.Greatsword, aSkill.Weapon, aSkill.name);
            Assert.AreEqual(SkillBehavior.ChargeCounter, aCharge.Behavior, "剛断が溜めない");
            Assert.AreEqual(SkillBehavior.Beam, aRift.Behavior, "地裂きが前へ走らない");
            Assert.AreEqual(SkillBehavior.DashStrike, aTackle.Behavior, "肩当てが踏み込まない");
            Assert.IsTrue(aFinisher.AutoChain);
        }

        [Test]
        public void Swings_AreHeavierThanTheSword()
        {
            // 大剣らしさ: 剣の連斬よりずっと遅いが、1 回ごとのひるみ・アーマー削りがずっと大きい
            var aGreat = Skill("Skill_Greatsword_Combo").GetStep(0);
            var aSword = Skill("Skill_Sword_Combo").GetStep(0);
            Assert.GreaterOrEqual(aGreat.StartupFrames, aSword.StartupFrames * 2, "大剣の振りが速すぎる");
            Assert.Greater(aGreat.Stagger, aSword.Stagger * 2f, "大剣のひるみが剣の 2 倍に届かない");
            Assert.Greater(aGreat.ArmorBreak, aSword.ArmorBreak * 2f, "大剣のアーマー削りが剣の 2 倍に届かない");
        }

        [Test]
        public void Charge_IsLongerAndBreaksMoreArmorThanTheIai_WithoutDashing()
        {
            var aCharge = Skill("Skill_Greatsword_Charge");
            var aIai = Skill("Skill_Sword_Iai");
            Assert.Greater(aCharge.ChargeFrames, aIai.ChargeFrames, "剛断の溜めが居合より短い");
            Assert.Greater(aCharge.GetStep(0).ArmorBreak, aIai.GetStep(0).ArmorBreak * 3f, "剛断のアーマー削りが弱い");
            Assert.Less(aCharge.GetStep(0).ForwardMove, aIai.GetStep(0).ForwardMove, "剛断は突進しない(その場で振り下ろす)");
        }

        [Test]
        public void Rift_IsAShortThickLine()
        {
            var aRift = Skill("Skill_Greatsword_Rift");
            var aLaser = Skill("Skill_Gun_Laser");
            Assert.Less(aRift.BeamLength, aLaser.BeamLength, "地裂きがレーザーより長い");
            Assert.Greater(aRift.BeamRadius, aLaser.BeamRadius, "地裂きがレーザーより細い");
            Assert.AreEqual(1, aRift.StepCount, "地裂きは 1 回だけ");
        }

        [Test]
        public void LoadoutList_AndPlayer_IncludeTheGreatsword()
        {
            var aDatabase = Load<SkillDatabase>("Assets/_Project/Data/SkillDatabase.asset");
            foreach (var aName in new[] { "Skill_Greatsword_Combo", "Skill_Greatsword_Charge", "Skill_Greatsword_Rift", "Skill_Greatsword_Tackle" })
            {
                Assert.IsTrue(aDatabase.Skills.Contains(Skill(aName)), $"{aName} が編成の一覧にない");
            }
            Assert.IsFalse(aDatabase.Skills.Contains(Skill("Skill_Greatsword_Finisher")), "フィニッシャーは編成の一覧に入れない");

            var aPlayer = Load<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_Greatsword.asset");
            Assert.IsTrue(aPlayer.GetComponent<SkillSlotController>().Weapons.Contains(aWeapon), "プレイヤーの武器の一覧に大剣がない");
        }
    }
}
