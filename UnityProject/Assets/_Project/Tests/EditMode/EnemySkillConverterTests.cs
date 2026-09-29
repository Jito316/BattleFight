using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>プレイヤーのスキルを敵の攻撃に置き換えるときの形と強さ</summary>
    public class EnemySkillConverterTests
    {
        readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        SkillData Skill(SkillBehavior behavior, SlotType slot = SlotType.AttackA, int steps = 1, int startup = 8, float damage = 20f)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            created.Add(skill);
            skill.displayName = behavior.ToString();
            skill.behavior = behavior;
            skill.slot = slot;
            skill.steps = new List<SkillStep>();
            for (int i = 0; i < steps; i++) skill.steps.Add(new SkillStep { startupFrames = startup, damage = damage, hitboxRadius = 1.2f });
            return skill;
        }

        [Test]
        public void Melee_IsSlowerAndWeakerThanThePlayersVersion()
        {
            var attack = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, startup: 6, damage: 20f), 0.5f, 2.2f);
            Assert.GreaterOrEqual(attack.windup, EnemySkillConverter.MinWindup, "予備動作が短すぎて見えない");
            Assert.Greater(attack.windup, FrameTime.ToSeconds(6));
            Assert.AreEqual(10f, attack.damage, 1e-4f);
            Assert.IsFalse(attack.ranged);
            Assert.NotNull(attack.sourceSkill);
            Assert.AreEqual("Standard", attack.name);
        }

        [Test]
        public void MultiStepSkill_RepeatsUpToTheLimit()
        {
            var four = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, steps: 4), 0.5f, 2.2f);
            Assert.AreEqual(EnemySkillConverter.MaxRepeat, four.repeat);
            var two = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, steps: 2), 0.5f, 2.2f);
            Assert.AreEqual(1, two.repeat);
        }

        [Test]
        public void Projectile_BecomesARangedAttackWithSlowerShots()
        {
            var skill = Skill(SkillBehavior.Projectile);
            skill.projectileCount = 3;
            skill.projectileSpeed = 30f;
            var attack = EnemySkillConverter.ToAttack(skill, 0.5f, 2.2f);
            Assert.IsTrue(attack.ranged);
            Assert.AreEqual(3, attack.projectileCount);
            Assert.Less(attack.projectileSpeed, 30f, "弾がプレイヤーの弾と同じ速さのまま");
            Assert.GreaterOrEqual(attack.range, 8f);
        }

        [Test]
        public void SpecialBehaviors_KeepTheirShape()
        {
            Assert.IsTrue(EnemySkillConverter.ToAttack(Skill(SkillBehavior.TargetedStrike, SlotType.AttackB), 0.5f, 2.2f).targetedStrike);
            Assert.IsTrue(EnemySkillConverter.ToAttack(Skill(SkillBehavior.HammerJump, SlotType.Movement), 0.5f, 2.2f).jumpSlam);
            Assert.IsTrue(EnemySkillConverter.ToAttack(Skill(SkillBehavior.Blink, SlotType.Movement), 0.5f, 2.2f).teleportBehind);
            Assert.GreaterOrEqual(EnemySkillConverter.ToAttack(Skill(SkillBehavior.DashStrike, SlotType.Movement), 0.5f, 2.2f).lunge, 4f);
        }

        [Test]
        public void MovementSkills_AreUsedLessOftenThanAttacks()
        {
            var a = EnemySkillConverter.ToAttack(Skill(SkillBehavior.Standard, SlotType.AttackA), 0.5f, 2.2f);
            var m = EnemySkillConverter.ToAttack(Skill(SkillBehavior.DashStrike, SlotType.Movement), 0.5f, 2.2f);
            Assert.Greater(a.weight, m.weight);
        }
    }
}
