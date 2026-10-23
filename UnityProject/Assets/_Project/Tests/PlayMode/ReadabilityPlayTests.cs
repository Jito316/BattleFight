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
    /// <summary>見た目・わかりやすさ: 敵の予備動作・倒した瞬間の決め・ステージの見た目・ロックオン</summary>
    public class ReadabilityPlayTests : InputTestFixture
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

        /// <summary>どこかのステージにいる、最初の攻撃が条件に合う敵</summary>
        EnemyProfile FindEnemy(Func<EnemyAttack, bool> iCondition)
        {
            foreach (var aStage in mDirector.Stages)
            {
                if (aStage == null || aStage.Waves == null) continue;
                foreach (var aWave in aStage.Waves)
                {
                    foreach (var aProfile in aWave.Enemies)
                    {
                        if (aProfile != null && !aProfile.IsBoss && aProfile.Attacks != null && aProfile.Attacks.Count > 0 && iCondition(aProfile.Attacks[0]))
                        {
                            return aProfile;
                        }
                    }
                }
            }
            Assert.Fail("条件に合う敵がいない");
            return null;
        }

        EnemyController Spawn(EnemyProfile iProfile, Vector3 iOffset)
        {
            var aEnemy = mDirector.SpawnExtra(iProfile, mPlayer.position + iOffset);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
        }

        [UnityTest]
        public IEnumerator MeleeWindup_ShowsTheArea_FillsUp_AndGlintsBeforeTheHit()
        {
            yield return LoadEmptyArena();
            var aEnemy = Spawn(FindEnemy(iAttack => !iAttack.Ranged && !iAttack.TargetedStrike && iAttack.Summon == null && !iAttack.JumpSlam),
                mPlayer.forward * 6f);
            yield return null;
            Assert.IsTrue(aEnemy.ForceAttack(0));
            yield return null;
            var aTelegraph = aEnemy.Telegraph;
            Assert.IsTrue(aTelegraph.ShowingArea, "予備動作で攻撃の範囲が出ない");
            float aFirst = aTelegraph.FillAmount;

            yield return WaitUntil(() => aTelegraph.IsGlinting || !aEnemy.IsTelegraphing, 3f);
            Assert.IsTrue(aTelegraph.IsGlinting, "攻撃の直前に光らない");
            Assert.Greater(aTelegraph.FillAmount, aFirst, "範囲が埋まっていかない");

            yield return WaitUntil(() => !aEnemy.IsTelegraphing, 2f);
            yield return null;
            Assert.IsFalse(aTelegraph.ShowingArea, "攻撃が出ても範囲が残っている");
        }

        [UnityTest]
        public IEnumerator RangedWindup_ShowsAnAimLine()
        {
            yield return LoadEmptyArena();
            var aEnemy = Spawn(FindEnemy(iAttack => iAttack.Ranged), mPlayer.forward * 8f);
            yield return null;
            Assert.IsTrue(aEnemy.ForceAttack(0));
            yield return null;
            Assert.IsTrue(aEnemy.Telegraph.ShowingAim, "弾の敵が狙いの線を出さない");
            Assert.IsFalse(aEnemy.Telegraph.ShowingArea);
        }

        [UnityTest]
        public IEnumerator KillingTheLastEnemyOfAWave_FocusesTheCamera()
        {
            yield return LoadEmptyArena();
            yield return WaitUntil(() => mDirector.State == ArenaDirector.GameState.Fighting, 3f);
            var aEnemy = Spawn(FindEnemy(iAttack => true), mPlayer.forward * 5f);
            yield return null;
            aEnemy.Damageable.Kill();
            yield return null;
            Assert.IsTrue(CombatFeedback.Instance.CameraRig.IsFocusing, "最後の 1 体を倒してもカメラが寄らない");
            Assert.Greater(CombatFeedback.Instance.CameraRig.Zoom, 0f);
        }

        /// <summary>色を、わずかな誤差は許して比べる(部品に当てた色を読み戻すと誤差が出る)</summary>
        static void AssertColor(Color iExpected, Color? iActual, string iMessage)
        {
            Assert.IsTrue(iActual.HasValue, iMessage);
            var aActual = iActual.Value;
            Assert.AreEqual(iExpected.r, aActual.r, 1e-3f, iMessage);
            Assert.AreEqual(iExpected.g, aActual.g, 1e-3f, iMessage);
            Assert.AreEqual(iExpected.b, aActual.b, 1e-3f, iMessage);
        }

        [UnityTest]
        public IEnumerator EachStage_HasItsOwnLook()
        {
            yield return LoadEmptyArena(3);
            var aTheme = mDirector.Stages[3].Theme;
            Assert.AreSame(aTheme, StageThemeApplier.Current, "ステージの見た目が当たっていない");
            Assert.AreEqual(aTheme.FogColor, RenderSettings.fogColor);
            Assert.AreEqual(aTheme.SkyColor, Camera.main.backgroundColor);
            var aFloor = GameObject.Find("Arena/Floor").GetComponent<Renderer>();
            AssertColor(aTheme.FloorColor, StageThemeApplier.TintOf(aFloor), "床の色が変わっていない");

            mDirector.StartStage(4);
            yield return null;
            Assert.AreNotEqual(aTheme.FogColor, RenderSettings.fogColor, "ステージを変えても霧の色が同じ");
            AssertColor(mDirector.Stages[4].Theme.FloorColor, StageThemeApplier.TintOf(aFloor), "ステージを変えても床の色が変わらない");
        }

        [UnityTest]
        public IEnumerator LockOn_ShowsARing_AndSwitchesLeftAndRight()
        {
            yield return LoadEmptyArena();
            var aProfile = FindEnemy(iAttack => true);
            var aCenter = Spawn(aProfile, mPlayer.forward * 8f);
            var aRight = Spawn(aProfile, mPlayer.forward * 8f + mPlayer.right * 5f);
            var aLeft = Spawn(aProfile, mPlayer.forward * 8f - mPlayer.right * 5f);
            yield return null;
            var aLockOn = mPlayer.GetComponent<LockOnSystem>();

            yield return Tap(mKeyboard.tabKey);
            Assert.AreSame(aCenter, aLockOn.Target, "正面の敵をロックオンしない");
            Assert.IsTrue(aLockOn.ShowingRing, "標的の足元に輪が出ない");

            yield return Tap(mKeyboard.cKey);
            Assert.AreSame(aRight, aLockOn.Target, "C で右の敵へ切り替わらない");
            yield return Tap(mKeyboard.zKey);
            yield return Tap(mKeyboard.zKey);
            Assert.AreSame(aLeft, aLockOn.Target, "Z で左の敵へ切り替わらない");

            yield return Tap(mKeyboard.tabKey);
            Assert.IsNull(aLockOn.Target);
            Assert.IsFalse(aLockOn.ShowingRing, "ロックオンをやめても輪が残る");
        }
    }
}
