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
        const string mSceneName = "Prototype_Arena";

        ArenaDirector mDirector;
        Transform mPlayer;

        public override void Setup()
        {
            base.Setup();
            // エリートはランダムに出るので、ここでは出さない(FunPlayTests で確かめる)
            ArenaDirector.EliteChanceOverride = 0f;
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            base.TearDown();
        }

        IEnumerator Load()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mPlayer = Object.FindFirstObjectByType<SkillExecutor>().transform;
            mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            mDirector.StartStage(0);
            yield return new WaitForSeconds(2f);
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            mDirector.StopAllCoroutines();
            yield return null;
        }

        EnemyProfile Enemy(string iDisplayName)
        {
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage.Waves == null) continue;
                foreach (var aWave in aStage.Waves)
                {
                    var aFound = aWave.Enemies.FirstOrDefault(iE => iE != null && iE.DisplayName == iDisplayName);
                    if (aFound != null) return aFound;
                }
            }
            Assert.Fail($"{iDisplayName} がどのステージにもいない");
            return null;
        }

        EnemyController Spawn(EnemyProfile iProfile) => mDirector.SpawnExtra(iProfile, mPlayer.position + mPlayer.forward * 5f);

        [UnityTest]
        public IEnumerator Mirror_AttacksWithPlayerSkills_AndSwapsItsTypeAndWeakness()
        {
            yield return Load();
            var aMirror = Spawn(Enemy("写し身・剣槌"));
            yield return null;

            Assert.AreEqual("剣の型", aMirror.CurrentSkillSet.Name);
            Assert.IsTrue(aMirror.IsWeakTo(WeaponType.Hammer));
            Assert.IsFalse(aMirror.IsWeakTo(WeaponType.Sword));

            // 攻撃はすべてプレイヤーのスキルから作ったもの
            Assert.IsTrue(aMirror.ForceAttack(0));
            Assert.NotNull(aMirror.CurrentAttack.SourceSkill);
            Assert.AreEqual("連斬", aMirror.CurrentAttack.SourceSkill.DisplayName);

            // 型を切り替えると、使う技と弱点が変わる
            yield return new WaitForSeconds(3f);
            Assert.IsTrue(aMirror.SwapSkillSet());
            Assert.AreEqual("大槌の型", aMirror.CurrentSkillSet.Name);
            Assert.IsTrue(aMirror.IsWeakTo(WeaponType.Sword), "型を切り替えても弱点が変わらない");
            Assert.IsFalse(aMirror.IsWeakTo(WeaponType.Hammer));
            Assert.IsTrue(aMirror.ForceAttack(0));
            Assert.AreEqual("重撃", aMirror.CurrentAttack.SourceSkill.DisplayName);
        }

        [UnityTest]
        public IEnumerator Mirror_SwapsByItselfAfterTheInterval()
        {
            yield return Load();
            var aProfile = Object.Instantiate(Enemy("写し身・杖銃"));
            aProfile.SkillSwapInterval = 0.5f;
            var aMirror = Spawn(aProfile);
            string aFirst = aMirror.CurrentSkillSet.Name;

            float aStart = Time.realtimeSinceStartup;
            while (aMirror.CurrentSkillSet.Name == aFirst)
            {
                if (Time.realtimeSinceStartup - aStart > 8f) Assert.Fail("時間がたっても型を切り替えない");
                yield return null;
            }
            Assert.Greater(aMirror.SkillSetChangedAt, 0f);
            Object.Destroy(aMirror.gameObject);
            yield return null;
            Object.Destroy(aProfile);
        }

        [UnityTest]
        public IEnumerator Bosses_MixPlayerSkillsIntoTheirAttacks()
        {
            yield return Load();
            var aShadow = Enemy("影刃");
            var aBoss = Spawn(aShadow);
            yield return null;

            // ボス自身の技のあとに、プレイヤーのスキルが並ぶ
            Assert.IsTrue(aBoss.ForceAttack(aShadow.Attacks.Count));
            Assert.NotNull(aBoss.CurrentAttack.SourceSkill, "ボスがプレイヤーのスキルを持っていない");
            Assert.AreEqual(WeaponType.Chain, aBoss.Weaknesses[0], "ボスの弱点は型に関係なくボスのまま");
        }

        [UnityTest]
        public IEnumerator WeaknessOfTheCurrentType_MultipliesPlayerDamage()
        {
            yield return Load();
            var aMirror = Spawn(Enemy("写し身・剣槌"));
            yield return null;
            var aDamageable = aMirror.Damageable;
            var aExecutor = mPlayer.GetComponent<SkillExecutor>();
            var aSlots = mPlayer.GetComponent<SkillSlotController>();

            // 剣の型: 大槌が弱点。剣で当てたときと大槌で当てたときで、ダメージが変わる
            var aSword = aSlots.Database.Find("Skill_Sword_Combo");
            var aHammer = aSlots.Database.Find("Skill_Hammer_Combo");
            var aTargets = new System.Collections.Generic.List<Damageable> { aDamageable };
            float aBefore = aDamageable.Health;
            aExecutor.ResolveHits(new SkillHitContext { Skill = aSword, Multiplier = 1f, SwapStrikeId = -1 }, aTargets, mPlayer.position);
            float aSwordDamage = aBefore - aDamageable.Health;
            aBefore = aDamageable.Health;
            aExecutor.ResolveHits(new SkillHitContext { Skill = aHammer, Multiplier = 1f, SwapStrikeId = -1 }, aTargets, mPlayer.position);
            float aHammerDamage = aBefore - aDamageable.Health;
            Assert.Greater(aHammerDamage / aHammer.GetStep(0).Damage, aSwordDamage / aSword.GetStep(0).Damage * 1.3f, "今の型の弱点で当てても強くならない");
        }
    }
}
