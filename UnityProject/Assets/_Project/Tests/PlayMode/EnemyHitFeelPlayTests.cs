using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>敵の攻撃の手ごたえ: 受けたときのヒットストップ・のけぞり・画面の演出・予備動作の音・HP が少ないときの赤い縁</summary>
    public class EnemyHitFeelPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        ArenaDirector mDirector;
        SkillExecutor mExecutor;
        Damageable mPlayer;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
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
            mExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            mPlayer = mExecutor.GetComponent<Damageable>();
            mDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            mDirector.StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            yield return null;
            // 最大 HP 100: 15 のダメージで「重い一撃」になる
            mPlayer.Configure(Team.Player, 100f, 0f, 0f);
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

        EnemyController SpawnNextToPlayer()
        {
            var aProfile = mDirector.Stages[1].Waves[0].Enemies[0];
            return mDirector.SpawnExtra(aProfile, mPlayer.transform.position + mPlayer.transform.forward * 1.5f);
        }

        HitOutcome HitPlayer(EnemyController iEnemy, float iDamage, bool iRanged) => mPlayer.ApplyHit(new HitInfo
        {
            Damage = iDamage,
            Stagger = 100f,
            Knockback = 4f,
            Direction = -mPlayer.transform.forward,
            Source = iEnemy.gameObject,
            Ranged = iRanged,
        });

        [UnityTest]
        public IEnumerator HeavyHit_FreezesThePlayerAndTheAttacker_AndShakesTheScreen()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnNextToPlayer();
            yield return null;

            Assert.AreEqual(HitOutcome.Hit, HitPlayer(aEnemy, 15f, false));
            Assert.IsTrue(mExecutor.IsHitFrozen, "受けたプレイヤーが止まらない");
            Assert.IsTrue(aEnemy.IsFrozen, "殴った敵が止まらない");
            Assert.AreEqual(1f, Time.timeScale, "ゲーム全体が遅くなっている");
            Assert.IsTrue(mPlayer.GetComponent<PlayerHitReaction>().IsReacting, "プレイヤーがのけぞらない");

            var aFeedback = CombatFeedback.Instance;
            Assert.Greater(aFeedback.VignetteAmount, 0f, "画面の縁が赤く光らない");
            Assert.AreEqual(new Color(1f, 0.15f, 0.1f), aFeedback.VignetteColor);
            Assert.Greater(aFeedback.FlashAmount, 0f, "重い一撃で画面が光らない");
            Assert.Greater(Rumble.LastStrength, 0.4f);
            Assert.AreEqual(Sfx.Hurt, SoundPlayer.LastPlayed);

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsFalse(mExecutor.IsHitFrozen);
            Assert.IsFalse(mPlayer.GetComponent<PlayerHitReaction>().IsReacting, "のけぞりが元に戻らない");
        }

        [UnityTest]
        public IEnumerator RangedHit_DoesNotFreezeTheShooter_AndLightHitsDoNotFlash()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnNextToPlayer();
            yield return null;

            Assert.AreEqual(HitOutcome.Hit, HitPlayer(aEnemy, 3f, true));
            Assert.IsTrue(mExecutor.IsHitFrozen);
            Assert.IsFalse(aEnemy.IsFrozen, "弾を撃った敵まで止まった");
            Assert.AreEqual(0f, CombatFeedback.Instance.FlashAmount, "軽い一撃で画面が光った");
        }

        [UnityTest]
        public IEnumerator EnemyWindup_PlaysAWarningSound()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnNextToPlayer();
            yield return null;
            Assert.IsTrue(aEnemy.ForceAttack(0));
            Assert.AreEqual(Sfx.EnemyWindup, SoundPlayer.LastPlayed, "予備動作の音が鳴らない");
        }

        [UnityTest]
        public IEnumerator LowHealth_PulsesTheScreenEdgesRed()
        {
            yield return LoadEmptyArena();
            var aHud = Object.FindFirstObjectByType<BattleHud>();
            yield return null;
            Assert.AreEqual(0f, aHud.DangerAmount, "HP が多いのに赤くなっている");

            var aEnemy = SpawnNextToPlayer();
            HitPlayer(aEnemy, 80f, false);
            yield return null;
            yield return null;
            Assert.Less(mPlayer.Health / mPlayer.MaxHealth, BattleHud.DangerHealthRatio);
            Assert.Greater(aHud.DangerAmount, 0f, "HP が少ないのに画面の縁が赤くならない");
        }
    }
}
