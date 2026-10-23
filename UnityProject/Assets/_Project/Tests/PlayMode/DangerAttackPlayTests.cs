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
    /// <summary>逃げるしかない攻撃: ガード・回避の無敵が効かず、範囲の外へ出れば当たらない</summary>
    public class DangerAttackPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        Transform mPlayer;
        Damageable mPlayerHealth;

        public override void Setup()
        {
            base.Setup();
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            ArenaDirector.EliteChanceOverride = 0f;
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            base.TearDown();
        }

        IEnumerator LoadEmptyArena()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mPlayer = Object.FindFirstObjectByType<SkillExecutor>().transform;
            mPlayerHealth = mPlayer.GetComponent<Damageable>();
            mPlayerHealth.Configure(Team.Player, 1000f, 0f, 0f);
            mDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            mDirector.StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            yield return null;
            yield return WaitUntil(() => mPlayer.GetComponent<PlayerMotor>().IsGrounded, 3f);
        }

        static IEnumerator WaitUntil(Func<bool> iCondition, float iTimeout)
        {
            float aStart = Time.realtimeSinceStartup;
            while (!iCondition())
            {
                if (Time.realtimeSinceStartup - aStart > iTimeout) Assert.Fail("タイムアウトしました");
                yield return null;
            }
        }

        /// <summary>「地割れ」を持つ重装兵を、プレイヤーのすぐ前に出す</summary>
        EnemyController SpawnQuaker(out int oQuakeIndex)
        {
            EnemyProfile aProfile = null;
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Waves == null) continue;
                aProfile = aStage.Waves.SelectMany(iWave => iWave.Enemies).FirstOrDefault(iEnemy => iEnemy != null && iEnemy.DisplayName == "重装兵");
                if (aProfile != null) break;
            }
            Assert.NotNull(aProfile, "重装兵がどのステージにもいない");
            oQuakeIndex = aProfile.Attacks.FindIndex(iAttack => iAttack.Unblockable);
            Assert.GreaterOrEqual(oQuakeIndex, 0, "重装兵が逃げるしかない攻撃を持っていない");
            var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.position + mPlayer.forward * 2f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
        }

        HitOutcome HitPlayer(bool iUnblockable) => mPlayerHealth.ApplyHit(new HitInfo
        {
            Damage = 20f,
            Stagger = 100f,
            Direction = -mPlayer.forward,
            Unblockable = iUnblockable,
        });

        [UnityTest]
        public IEnumerator Guard_DoesNotStopIt()
        {
            yield return LoadEmptyArena();
            Press(mKeyboard.qKey);
            yield return new WaitForSecondsRealtime(GuardRules.ParryWindow + 0.1f);
            Assert.AreEqual(HitOutcome.Guarded, HitPlayer(false), "ふつうの攻撃をガードできない(前提)");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer(true), "逃げるしかない攻撃をガードできてしまった");
            Release(mKeyboard.qKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DodgeInvulnerability_DoesNotStopIt()
        {
            yield return LoadEmptyArena();
            mPlayerHealth.Invulnerable = true;
            Assert.AreEqual(HitOutcome.Evaded, HitPlayer(false), "ふつうの攻撃を無敵で避けられない(前提)");
            Assert.AreEqual(HitOutcome.Hit, HitPlayer(true), "逃げるしかない攻撃を無敵で避けられてしまった");
            mPlayerHealth.Invulnerable = false;
        }

        [UnityTest]
        public IEnumerator QuakeWindup_IsShownAsDanger()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnQuaker(out int aQuake);
            yield return null;
            Assert.IsTrue(aEnemy.ForceAttack(aQuake));
            yield return null;
            Assert.IsTrue(aEnemy.Telegraph.ShowingArea, "地割れの範囲が出ない");
            Assert.IsTrue(aEnemy.Telegraph.IsDanger, "逃げるしかない攻撃として見せていない");
        }

        [UnityTest]
        public IEnumerator EscapingTheArea_AvoidsTheHit_AndIsPraised()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnQuaker(out int aQuake);
            var aStyle = mPlayer.GetComponent<StyleRankSystem>();
            yield return null;
            Assert.IsTrue(aEnemy.ForceAttack(aQuake));
            yield return null;

            // 範囲(半径 5 × 体の大きさ)の外まで逃げる
            mPlayer.GetComponent<PlayerMotor>().Teleport(-mPlayer.forward * 12f);
            float aHealth = mPlayerHealth.Health;
            yield return WaitUntil(() => !aEnemy.IsTelegraphing && aEnemy.CurrentAttack == null, 4f);
            Assert.AreEqual(aHealth, mPlayerHealth.Health, "範囲の外にいたのに当たった");
            Assert.IsTrue(aStyle.Announcements.Any(iAnnouncement => iAnnouncement.Text == "ESCAPE!"), "逃げ切っても褒められない");
        }

        [UnityTest]
        public IEnumerator StayingInTheArea_GetsHit()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnQuaker(out int aQuake);
            yield return null;
            float aHealth = mPlayerHealth.Health;
            Assert.IsTrue(aEnemy.ForceAttack(aQuake));
            yield return WaitUntil(() => mPlayerHealth.Health < aHealth, 4f);
        }
    }
}
