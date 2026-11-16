using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>大鎌のデータ: 武器種・3 スロットとフィニッシャーの技・広く刈って引き寄せる・編成の一覧・プレイヤーの武器の一覧</summary>
    public class ScytheTests
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
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_Scythe.asset");
            Assert.AreEqual(WeaponType.Scythe, aWeapon.Weapon);
            Assert.AreEqual(Skill("Skill_Scythe_Finisher"), aWeapon.MasteryFinisher, "マスタリーのフィニッシャーがつながっていない");
            Assert.NotNull(aWeapon.Prefab, "武器のプレハブがつながっていない");
            Assert.NotNull(aWeapon.Prefab.GetComponent<WeaponVisual>(), "武器のプレハブに WeaponVisual がない");
        }

        [Test]
        public void Skills_FillEverySlot()
        {
            var aReap = Skill("Skill_Scythe_Reap");
            var aDoom = Skill("Skill_Scythe_Doom");
            var aHarvest = Skill("Skill_Scythe_Harvest");
            var aShade = Skill("Skill_Scythe_Shade");
            var aFinisher = Skill("Skill_Scythe_Finisher");
            Assert.AreEqual(SlotType.AttackA, aReap.Slot);
            Assert.AreEqual(SlotType.AttackB, aDoom.Slot);
            Assert.AreEqual(SlotType.AttackB, aHarvest.Slot);
            Assert.AreEqual(SlotType.Movement, aShade.Slot);
            foreach (var aSkill in new[] { aReap, aDoom, aHarvest, aShade, aFinisher }) Assert.AreEqual(WeaponType.Scythe, aSkill.Weapon, aSkill.name);
            Assert.AreEqual(SkillBehavior.TargetedStrike, aDoom.Behavior, "死神の鎌が遠くに落ちない");
            Assert.AreEqual(SkillBehavior.DashStrike, aShade.Behavior, "影走りが駆け抜けない");
            Assert.Greater(aShade.InvulnerableFrames, Skill("Skill_Sword_Step").InvulnerableFrames, "影走りの無敵がステップ斬りより短い");
            Assert.IsTrue(aFinisher.AutoChain);
        }

        [Test]
        public void Reap_IsWiderThanTheSword_AndPullsEnemiesIn()
        {
            // 大鎌らしさ: 剣の連斬より当たりが広い。押し返しが負の値(手前へ引き寄せる)
            var aReap = Skill("Skill_Scythe_Reap");
            Assert.Greater(aReap.GetStep(0).HitboxRadius, Skill("Skill_Sword_Combo").GetStep(0).HitboxRadius + 0.4f, "刈り取りが剣より広くない");
            Assert.IsTrue(aReap.Steps.All(iStep => iStep.Knockback < 0f), "刈り取りが敵を引き寄せない");
            Assert.Less(Skill("Skill_Scythe_Harvest").GetStep(0).Knockback, aReap.GetStep(0).Knockback, "円月刈りが刈り取りより強く引き寄せない");
            Assert.Greater(Skill("Skill_Scythe_Doom").GetStep(0).ArmorBreak, Skill("Skill_Staff_Thunder").GetStep(0).ArmorBreak, "死神の鎌のアーマー削りが落雷より弱い");
        }

        [Test]
        public void Finisher_PullsThenBlowsAway()
        {
            // 魂狩り: 最後の一振りの前までは引き寄せ、最後だけ吹き飛ばす
            var aSteps = Skill("Skill_Scythe_Finisher").Steps;
            for (int aIndex = 0; aIndex < aSteps.Count - 1; aIndex++) Assert.Less(aSteps[aIndex].Knockback, 0f, $"{aIndex} 段目が引き寄せない");
            Assert.Greater(aSteps[aSteps.Count - 1].Knockback, 0f, "最後の一振りが吹き飛ばさない");
        }

        [Test]
        public void LoadoutList_AndPlayer_IncludeTheScythe()
        {
            var aDatabase = Load<SkillDatabase>("Assets/_Project/Data/SkillDatabase.asset");
            foreach (var aName in new[] { "Skill_Scythe_Reap", "Skill_Scythe_Doom", "Skill_Scythe_Harvest", "Skill_Scythe_Shade" })
            {
                Assert.IsTrue(aDatabase.Skills.Contains(Skill(aName)), $"{aName} が編成の一覧にない");
            }
            Assert.IsFalse(aDatabase.Skills.Contains(Skill("Skill_Scythe_Finisher")), "フィニッシャーは編成の一覧に入れない");

            var aPlayer = Load<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_Scythe.asset");
            Assert.IsTrue(aPlayer.GetComponent<SkillSlotController>().Weapons.Contains(aWeapon), "プレイヤーの武器の一覧に大鎌がない");
        }
    }
}
