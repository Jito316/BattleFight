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
    /// <summary>通しで遊んで見つけたところの調整: ロックオン中の向き・カメラ・暗いステージの照り返し</summary>
    public class PlaytestTuningPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        Transform mPlayer;

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

        IEnumerator LoadEmptyArena(int iStage = 1)
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mPlayer = Object.FindFirstObjectByType<SkillExecutor>().transform;
            mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            mDirector.StartStage(iStage);
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

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
        }

        EnemyController SpawnAhead()
        {
            var aProfile = mDirector.Stages[1].Waves[0].Enemies[0];
            var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.position + mPlayer.forward * 9f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
        }

        [UnityTest]
        public IEnumerator BackingAwayWhileLockedOn_KeepsFacingTheEnemy()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnAhead();
            yield return null;
            yield return Tap(mKeyboard.tabKey);
            Assert.IsNotNull(mPlayer.GetComponent<LockOnSystem>().Target);

            // 後ろへ下がる
            Press(mKeyboard.sKey);
            yield return new WaitForSeconds(0.6f);
            Release(mKeyboard.sKey);
            Vector3 aToEnemy = aEnemy.transform.position - mPlayer.position;
            aToEnemy.y = 0f;
            Assert.Greater(Vector3.Dot(mPlayer.forward, aToEnemy.normalized), 0.9f, "ロックオン中に下がると、敵に背を向けた");
        }

        [UnityTest]
        public IEnumerator WithoutLockOn_BackingAwayTurnsAround()
        {
            yield return LoadEmptyArena();
            Vector3 aForward = mPlayer.forward;
            Press(mKeyboard.sKey);
            yield return new WaitForSeconds(0.6f);
            Release(mKeyboard.sKey);
            Assert.Less(Vector3.Dot(mPlayer.forward, aForward), 0f, "ロックオンしていないのに、歩く向きを向かない");
        }

        [UnityTest]
        public IEnumerator LockedOnCamera_SitsOverTheRightShoulder()
        {
            yield return LoadEmptyArena();
            SpawnAhead();
            yield return null;
            var aCamera = Camera.main.transform;
            float aBefore = Vector3.Dot(aCamera.position - mPlayer.position, aCamera.right);
            yield return Tap(mKeyboard.tabKey);
            yield return new WaitForSeconds(0.8f);
            float aAfter = Vector3.Dot(aCamera.position - mPlayer.position, aCamera.right);
            Assert.Greater(aAfter, aBefore + 0.6f, "ロックオンしても、カメラが右肩へずれない");
        }

        [UnityTest]
        public IEnumerator DarkStages_ReflectLess()
        {
            yield return LoadEmptyArena(4);
            Assert.AreEqual(mDirector.Stages[4].Theme.Reflection, RenderSettings.reflectionIntensity, 1e-4f);
            Assert.Less(RenderSettings.reflectionIntensity, 0.3f, "夜のステージの照り返しが強いまま");
            mDirector.StartStage(2);
            yield return null;
            Assert.Greater(RenderSettings.reflectionIntensity, 0.4f, "明るいステージまで照り返しが弱い");
        }
    }
}
