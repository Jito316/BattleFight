using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>回避の手ごたえ: 残像・見切りの演出・見切り中の反撃</summary>
    public class DodgePlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        SkillExecutor mExecutor;
        Damageable mPlayer;

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
            mExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            mPlayer = mExecutor.GetComponent<Damageable>();
            mPlayer.Configure(Team.Player, 100000f, 0f, 0f);
            mExecutor.GetComponent<SkillSlotController>().SetMode(SwapMode.Preset);
            mDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            mDirector.StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            yield return null;
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);
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

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
        }

        EnemyController SpawnInFront(float iDistance)
        {
            var aProfile = mDirector.Stages[1].Waves[0].Enemies[0];
            var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.transform.position + mPlayer.transform.forward * iDistance);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
        }

        static int CountAfterimages()
        {
            int aCount = 0;
            foreach (var aTransform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (aTransform.name == "Afterimage") aCount++;
            return aCount;
        }

        [UnityTest]
        public IEnumerator Dodging_LeavesAfterimages()
        {
            yield return LoadEmptyArena();
            Assert.AreEqual(0, CountAfterimages());
            // 剣士の移動 = ステップ斬り(出始めは無敵)
            yield return Tap(mKeyboard.lKey);
            yield return WaitUntil(() => CountAfterimages() > 0, 1f);
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(0, CountAfterimages(), "残像が消えない");
        }

        [UnityTest]
        public IEnumerator JustDodge_FreezesTheAttacker_AndTintsTheScreen()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront(2f);
            var aHud = Object.FindFirstObjectByType<BattleHud>();
            yield return null;

            yield return Tap(mKeyboard.lKey);
            var aOutcome = mPlayer.ApplyHit(new HitInfo { Damage = 10f, Direction = -mPlayer.transform.forward, Source = aEnemy.gameObject });
            Assert.AreEqual(HitOutcome.Evaded, aOutcome, "移動スキルの無敵で避けられていない");
            Assert.IsTrue(EnemyTime.IsSlowed);
            Assert.IsTrue(aEnemy.IsFrozen, "空振りした敵が止まらない");
            Assert.Greater(CombatFeedback.Instance.VignetteAmount, 0f, "見切りで画面の縁が光らない");

            yield return null;
            yield return null;
            Assert.Greater(aHud.SlowAmount, 0f, "見切りの間、画面の縁が紫にならない");
            Assert.Greater(CombatFeedback.Instance.CameraRig.Zoom, 0f, "見切りでズームしない");

            EnemyTime.Clear();
            yield return null;
            yield return null;
            Assert.AreEqual(0f, aHud.SlowAmount, "見切りが終わっても紫のまま");
        }

        [UnityTest]
        public IEnumerator HittingDuringTheSlow_IsShownAsAPunish()
        {
            yield return LoadEmptyArena();
            SpawnInFront(1.6f);
            yield return null;
            EnemyTime.Slow();

            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => CombatFeedback.Instance.FlashAmount > 0f, 1.5f);
        }
    }
}
