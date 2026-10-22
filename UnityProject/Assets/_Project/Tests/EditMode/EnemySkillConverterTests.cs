using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>プレイヤーのスキルを敵の攻撃に置き換えるときの形と強さ</summary>
    public class EnemySkillConverterTests
    {
        readonly List<Object> mCreated = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var aO in mCreated) Object.DestroyImmediate(aO);
            mCreated.Clear();
        }

        SkillData Skill(SkillBehavior iBehavior, SlotType iSlot = SlotType.AttackA, int iSteps = 1, int iStartup = 8, float iDamage = 20f)
        {
            var aSkill = ScriptableObject.CreateInstance<SkillData>();
            mCreated.Add(aSkill);
            aSkill.DisplayName = iBehavior.ToString();
            aSkill.Behavior = iBehavior;
            aSkill.Slot = iSlot;
            aSkill.Steps = new List<SkillStep>();
            for (int aI = 0; aI < iSteps; aI++) aSkill.Steps.Add(new SkillStep { StartupFrames = iStartup, Damage = iDamage, HitboxRadius = 1.2f });
            return aSkill;
        }

        [Test]
        public void Melee_IsSlowerAndWeakerThanThePlayersVersion()
        {
            var aAttack = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, iStartup: 6, iDamage: 20f), 0.5f, 2.2f);
            Assert.GreaterOrEqual(aAttack.Windup, EnemySkillConverter.MinWindup, "予備動作が短すぎて見えない");
            Assert.Greater(aAttack.Windup, FrameTime.ToSeconds(6));
            Assert.AreEqual(10f, aAttack.Damage, 1e-4f);
            Assert.IsFalse(aAttack.Ranged);
            Assert.NotNull(aAttack.SourceSkill);
            Assert.AreEqual("Standard", aAttack.Name);
        }

        [Test]
        public void MultiStepSkill_RepeatsUpToTheLimit()
        {
            var aFour = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, iSteps: 4), 0.5f, 2.2f);
            Assert.AreEqual(EnemySkillConverter.MaxRepeat, aFour.Repeat);
            var aTwo = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, iSteps: 2), 0.5f, 2.2f);
            Assert.AreEqual(1, aTwo.Repeat);
        }

        [Test]
        public void Projectile_BecomesARangedAttackWithSlowerShots()
        {
            var aSkill = Skill(SkillBehavior.Projectile);
            aSkill.ProjectileCount = 3;
            aSkill.ProjectileSpeed = 30f;
            var aAttack = EnemySkillConverter.ToAttack(aSkill, 0.5f, 2.2f);
            Assert.IsTrue(aAttack.Ranged);
            Assert.AreEqual(3, aAttack.ProjectileCount);
            Assert.Less(aAttack.ProjectileSpeed, 30f, "弾がプレイヤーの弾と同じ速さのまま");
            Assert.GreaterOrEqual(aAttack.Range, 8f);
        }

        [Test]
        public void SpecialBehaviors_KeepTheirShape()
        {
            Assert.IsTrue(EnemySkillConverter.ToAttack(Skill(SkillBehavior.TargetedStrike, SlotType.AttackB), 0.5f, 2.2f).TargetedStrike);
            Assert.IsTrue(EnemySkillConverter.ToAttack(Skill(SkillBehavior.HammerJump, SlotType.Movement), 0.5f, 2.2f).JumpSlam);
            Assert.IsTrue(EnemySkillConverter.ToAttack(Skill(SkillBehavior.Blink, SlotType.Movement), 0.5f, 2.2f).TeleportBehind);
            Assert.GreaterOrEqual(EnemySkillConverter.ToAttack(Skill(SkillBehavior.DashStrike, SlotType.Movement), 0.5f, 2.2f).Lunge, 4f);
        }

        [Test]
        public void MovementSkills_AreUsedLessOftenThanAttacks()
        {
            var aA = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, SlotType.AttackA), 0.5f, 2.2f);
            var aM = EnemySkillConverter.ToAttack(Skill(SkillBehavior.DashStrike, SlotType.Movement), 0.5f, 2.2f);
            Assert.Greater(aA.Weight, aM.Weight);
        }
    }
}
