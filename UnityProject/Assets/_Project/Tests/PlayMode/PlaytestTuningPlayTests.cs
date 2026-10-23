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

        EnemyProfile FindArmored()
        {
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Waves == null) continue;
                foreach (var aWave in aStage.Waves)
                {
                    foreach (var aProfile in aWave.Enemies)
                    {
                        if (aProfile != null && aProfile.DisplayName == "重装兵") return aProfile;
                    }
                }
            }
            Assert.Fail("重装兵がいない");
            return null;
        }

        [UnityTest]
        public IEnumerator FirstWave_HasNoElites()
        {
            ArenaDirector.EliteChanceOverride = -1f;
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            Assert.AreEqual(1, mDirector.WaveNumber);
            foreach (var aEnemy in EnemyController.Active)
            {
                Assert.AreEqual(EliteAffix.None, aEnemy.Profile.Elite, "最初のウェーブにエリートが出た");
            }
        }

        [UnityTest]
        public IEnumerator GiantElite_DangerArea_StaysEscapable()
        {
            yield return LoadEmptyArena();
            var aGiant = mDirector.SpawnElite(FindArmored(), EliteAffix.Giant, mPlayer.position + mPlayer.forward * 8f);
            yield return null;
            var aQuake = aGiant.Profile.Attacks.Find(iAttack => iAttack.Unblockable);
            Assert.NotNull(aQuake);
            Assert.Greater(aQuake.Radius * aGiant.Profile.Scale, EnemyController.MaxDangerRadius, "(前提)巨大になると範囲が上限を超える");
            Assert.AreEqual(EnemyController.MaxDangerRadius, aGiant.AttackRadius(aQuake), 1e-4f, "逃げるしかない攻撃の範囲が上限で止まらない");
        }

        [UnityTest]
        public IEnumerator LockingOnToABigEnemy_PullsTheCameraBack()
        {
            yield return LoadEmptyArena();
            var aRig = Object.FindFirstObjectByType<ThirdPersonCamera>();
            float aBase = aRig.CurrentDistance;
            mDirector.SpawnElite(FindArmored(), EliteAffix.Giant, mPlayer.position + mPlayer.forward * 9f);
            yield return null;
            yield return Tap(mKeyboard.tabKey);
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(aRig.CurrentDistance, aBase + 1f, "大きな敵にロックオンしても、カメラが引かない");
        }

        [UnityTest]
        public IEnumerator Announcements_DoNotPileUp()
        {
            yield return LoadEmptyArena();
            var aStyle = mPlayer.GetComponent<StyleRankSystem>();
            aStyle.Announce("WEAK!", Color.white);
            aStyle.Announce("CHAIN ×1", Color.white);
            aStyle.Announce("WEAK!", Color.white);
            aStyle.Announce("SWAP STRIKE!", Color.white);
            aStyle.Announce("CHANGE ATTACK", Color.white);
            aStyle.Announce("WEAK!", Color.white);
            Assert.LessOrEqual(aStyle.Announcements.Count, StyleRankSystem.MaxAnnouncements, "右側の文字が多すぎる");
            Assert.AreEqual(1, aStyle.Announcements.FindAll(iAnnouncement => iAnnouncement.Text == "WEAK!").Count, "同じ文字が並んでいる");
            Assert.AreEqual("WEAK!", aStyle.Announcements[aStyle.Announcements.Count - 1].Text, "最後に出た文字がいちばん新しくない");
        }
    }
}
