using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>プレイヤーのスキルを使う敵(写し身・ボス)が、スキルで攻撃し、型を切り替え、弱点が変わるか</summary>
    public class MirrorEnemyPlayTests : InputTestFixture
    {
        const string SceneName = "Prototype_Arena";

        ArenaDirector director;
        Transform player;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            base.TearDown();
        }

        IEnumerator Load()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            director = Object.FindFirstObjectByType<ArenaDirector>();
            player = Object.FindFirstObjectByType<SkillExecutor>().transform;
            player.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            director.StartStage(0);
            yield return new WaitForSeconds(2f);
            foreach (var enemy in EnemyController.Active.ToArray()) Object.Destroy(enemy.gameObject);
            director.StopAllCoroutines();
            yield return null;
        }

        EnemyProfile Enemy(string displayName)
        {
            foreach (var stage in director.Stages)
            {
                if (stage.waves == null) continue;
                foreach (var wave in stage.waves)
                {
                    var found = wave.enemies.FirstOrDefault(e => e != null && e.displayName == displayName);
                    if (found != null) return found;
                }
            }
            Assert.Fail($"{displayName} がどのステージにもいない");
            return null;
        }

        EnemyController Spawn(EnemyProfile profile) => director.SpawnExtra(profile, player.position + player.forward * 5f);

        [UnityTest]
        public IEnumerator Mirror_AttacksWithPlayerSkills_AndSwapsItsTypeAndWeakness()
        {
            yield return Load();
            var mirror = Spawn(Enemy("写し身・剣槌"));
            yield return null;

            Assert.AreEqual("剣の型", mirror.CurrentSkillSet.name);
            Assert.IsTrue(mirror.IsWeakTo(WeaponType.Hammer));
            Assert.IsFalse(mirror.IsWeakTo(WeaponType.Sword));

            // 攻撃はすべてプレイヤーのスキルから作ったもの
            Assert.IsTrue(mirror.ForceAttack(0));
            Assert.NotNull(mirror.CurrentAttack.sourceSkill);
            Assert.AreEqual("連斬", mirror.CurrentAttack.sourceSkill.displayName);

            // 型を切り替えると、使う技と弱点が変わる
            yield return new WaitForSeconds(3f);
            Assert.IsTrue(mirror.SwapSkillSet());
            Assert.AreEqual("大槌の型", mirror.CurrentSkillSet.name);
            Assert.IsTrue(mirror.IsWeakTo(WeaponType.Sword), "型を切り替えても弱点が変わらない");
            Assert.IsFalse(mirror.IsWeakTo(WeaponType.Hammer));
            Assert.IsTrue(mirror.ForceAttack(0));
            Assert.AreEqual("重撃", mirror.CurrentAttack.sourceSkill.displayName);
        }

        [UnityTest]
        public IEnumerator Mirror_SwapsByItselfAfterTheInterval()
        {
            yield return Load();
            var profile = Object.Instantiate(Enemy("写し身・杖銃"));
            profile.skillSwapInterval = 0.5f;
            var mirror = Spawn(profile);
            string first = mirror.CurrentSkillSet.name;

            float start = Time.realtimeSinceStartup;
            while (mirror.CurrentSkillSet.name == first)
            {
                if (Time.realtimeSinceStartup - start > 8f) Assert.Fail("時間がたっても型を切り替えない");
                yield return null;
            }
            Assert.Greater(mirror.SkillSetChangedAt, 0f);
            Object.Destroy(mirror.gameObject);
            yield return null;
            Object.Destroy(profile);
        }

        [UnityTest]
        public IEnumerator Bosses_MixPlayerSkillsIntoTheirAttacks()
        {
            yield return Load();
            var shadow = Enemy("影刃");
            var boss = Spawn(shadow);
            yield return null;

            // ボス自身の技のあとに、プレイヤーのスキルが並ぶ
            Assert.IsTrue(boss.ForceAttack(shadow.attacks.Count));
            Assert.NotNull(boss.CurrentAttack.sourceSkill, "ボスがプレイヤーのスキルを持っていない");
            Assert.AreEqual(WeaponType.Chain, boss.Weaknesses[0], "ボスの弱点は型に関係なくボスのまま");
        }

        [UnityTest]
        public IEnumerator WeaknessOfTheCurrentType_MultipliesPlayerDamage()
        {
            yield return Load();
            var mirror = Spawn(Enemy("写し身・剣槌"));
            yield return null;
            var damageable = mirror.Damageable;
            var executor = player.GetComponent<SkillExecutor>();
            var slots = player.GetComponent<SkillSlotController>();

            // 剣の型: 大槌が弱点。剣で当てたときと大槌で当てたときで、ダメージが変わる
            var sword = slots.Database.Find("Skill_Sword_Combo");
            var hammer = slots.Database.Find("Skill_Hammer_Combo");
            var targets = new System.Collections.Generic.List<Damageable> { damageable };
            float before = damageable.Health;
            executor.ResolveHits(new SkillHitContext { skill = sword, multiplier = 1f, swapStrikeId = -1 }, targets, player.position);
            float swordDamage = before - damageable.Health;
            before = damageable.Health;
            executor.ResolveHits(new SkillHitContext { skill = hammer, multiplier = 1f, swapStrikeId = -1 }, targets, player.position);
            float hammerDamage = before - damageable.Health;
            Assert.Greater(hammerDamage / hammer.GetStep(0).damage, swordDamage / sword.GetStep(0).damage * 1.3f, "今の型の弱点で当てても強くならない");
        }
    }
}
