using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>槍のデータ: 武器種・3 スロットとフィニッシャーの技・剣より遠く届く・編成の一覧・プレイヤーの武器の一覧</summary>
    public class SpearTests
    {
        static T Load<T>(string iPath) where T : Object
        {
            var aAsset = AssetDatabase.LoadAssetAtPath<T>(iPath);
            Assert.NotNull(aAsset, $"{iPath} がない");
            return aAsset;
        }

        static SkillData Skill(string iName) => Load<SkillData>($"Assets/_Project/Data/Skills/{iName}.asset");

        /// <summary>体の前、どこまで届くか(当たりの中心の前への距離 + 半径)</summary>
        static float Reach(SkillStep iStep) => iStep.HitboxOffset.z + iStep.HitboxRadius;

        [Test]
        public void Weapon_HasAFinisher_AndAPrefab()
        {
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_Spear.asset");
            Assert.AreEqual(WeaponType.Spear, aWeapon.Weapon);
            Assert.AreEqual(Skill("Skill_Spear_Finisher"), aWeapon.MasteryFinisher, "マスタリーのフィニッシャーがつながっていない");
            Assert.NotNull(aWeapon.Prefab, "武器のプレハブがつながっていない");
            Assert.NotNull(aWeapon.Prefab.GetComponent<WeaponVisual>(), "武器のプレハブに WeaponVisual がない");
        }

        [Test]
        public void Skills_FillEverySlot()
        {
            var aCombo = Skill("Skill_Spear_Combo");
            var aSweep = Skill("Skill_Spear_Sweep");
            var aVault = Skill("Skill_Spear_Vault");
            var aFinisher = Skill("Skill_Spear_Finisher");
            Assert.AreEqual(SlotType.AttackA, aCombo.Slot);
            Assert.AreEqual(SlotType.AttackB, aSweep.Slot);
            Assert.AreEqual(SlotType.Movement, aVault.Slot);
            foreach (var aSkill in new[] { aCombo, aSweep, aVault, aFinisher }) Assert.AreEqual(WeaponType.Spear, aSkill.Weapon, aSkill.name);
            Assert.AreEqual(SkillBehavior.HammerJump, aVault.Behavior, "棒高跳びが跳ばない");
            Assert.IsTrue(aVault.Steps.All(iStep => iStep.Damage <= 0f), "棒高跳びは攻撃しない(跳ぶだけ)");
            Assert.Greater(aVault.InvulnerableFrames, 0, "棒高跳びに無敵がない");
            Assert.IsTrue(aFinisher.AutoChain);
        }

        [Test]
        public void Thrusts_ReachFartherThanTheSword_AndSweepPushesHarder()
        {
            // 槍らしさ: 剣の連斬より遠くへ届く。薙ぎ払いは剣より強く押し返す
            var aSpear = Skill("Skill_Spear_Combo").GetStep(0);
            var aSword = Skill("Skill_Sword_Combo").GetStep(0);
            Assert.Greater(Reach(aSpear), Reach(aSword) + 0.5f, "槍の突きが剣より遠くへ届かない");
            Assert.Greater(Skill("Skill_Spear_Sweep").GetStep(0).Knockback, Skill("Skill_Sword_Combo").GetStep(3).Knockback, "薙ぎ払いの押し返しが弱い");
        }

        [Test]
        public void Javelin_FliesFarAndPierces()
        {
            // 投げ槍: 攻撃B の弾。飛燕斬より遠くまで飛び、重く、敵を貫く
            var aJavelin = Skill("Skill_Spear_Javelin");
            var aWave = Skill("Skill_Sword_Wave");
            Assert.AreEqual(SlotType.AttackB, aJavelin.Slot);
            Assert.AreEqual(WeaponType.Spear, aJavelin.Weapon);
            Assert.AreEqual(SkillBehavior.Projectile, aJavelin.Behavior, "投げ槍が弾を飛ばさない");
            Assert.IsTrue(aJavelin.ProjectilePierce, "投げ槍が敵を貫かない");
            Assert.Greater(aJavelin.ProjectileSpeed * aJavelin.ProjectileLifetime, aWave.ProjectileSpeed * aWave.ProjectileLifetime, "飛燕斬より遠くへ飛ばない");
            Assert.Greater(aJavelin.GetStep(0).Damage, aWave.GetStep(0).Damage, "飛燕斬より軽い");
            Assert.Greater(aJavelin.GetStep(0).Knockback, aWave.GetStep(0).Knockback, "飛燕斬より押し返しが弱い");
        }

        [Test]
        public void LoadoutList_AndPlayer_IncludeTheSpear()
        {
            var aDatabase = Load<SkillDatabase>("Assets/_Project/Data/SkillDatabase.asset");
            foreach (var aName in new[] { "Skill_Spear_Combo", "Skill_Spear_Sweep", "Skill_Spear_Javelin", "Skill_Spear_Vault" })
            {
                Assert.IsTrue(aDatabase.Skills.Contains(Skill(aName)), $"{aName} が編成の一覧にない");
            }
            Assert.IsFalse(aDatabase.Skills.Contains(Skill("Skill_Spear_Finisher")), "フィニッシャーは編成の一覧に入れない");

            var aPlayer = Load<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var aWeapon = Load<WeaponTypeData>("Assets/_Project/Data/Weapons/Weapon_Spear.asset");
            Assert.IsTrue(aPlayer.GetComponent<SkillSlotController>().Weapons.Contains(aWeapon), "プレイヤーの武器の一覧に槍がない");
        }
    }
}
