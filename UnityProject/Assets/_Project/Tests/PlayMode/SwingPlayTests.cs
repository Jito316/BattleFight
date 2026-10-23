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
    /// <summary>振るだけで気持ちいい: 空振りでも斬撃の弧・体のひねり・武器ごとの振りの音が出る</summary>
    public class SwingPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        SkillExecutor mExecutor;

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
            var aDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            mExecutor.GetComponent<SkillSlotController>().SetMode(SwapMode.Preset);
            aDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            aDirector.StopAllCoroutines();
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

        static int CountArcs()
        {
            int aCount = 0;
            foreach (var aArc in Object.FindObjectsByType<SwingArcFx>(FindObjectsSortMode.None)) aCount++;
            return aCount;
        }

        [UnityTest]
        public IEnumerator SwingingAtNothing_StillShowsAnArc_TwistsAndSounds()
        {
            yield return LoadEmptyArena();
            Assert.AreEqual(0, CountArcs());
            var aBody = mExecutor.GetComponent<PlayerHitReaction>();

            // 敵のいないところで、剣士の連斬を空振りする
            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => CountArcs() > 0, 1f);
            Assert.AreEqual(Sfx.SwingSword, SoundPlayer.LastPlayed, "剣の振りの音が鳴らない");
            Assert.IsTrue(aBody.IsTwisting, "振っても体がひねられない");

            yield return new WaitForSecondsRealtime(SwingArcFx.Lifetime + 0.1f);
            yield return WaitUntil(() => !mExecutor.IsBusy, 3f);
            yield return new WaitForSecondsRealtime(SwingArcFx.Lifetime + 0.1f);
            Assert.AreEqual(0, CountArcs(), "斬撃の弧が消えない");
        }

        [UnityTest]
        public IEnumerator Guns_DoNotDrawArcs()
        {
            yield return LoadEmptyArena();
            yield return Tap(mKeyboard.digit3Key);
            Assert.AreEqual(WeaponType.Gun, mExecutor.GetComponent<SkillSlotController>().GetCurrent(SlotType.AttackA).Weapon);
            yield return Tap(mKeyboard.jKey);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(0, CountArcs(), "銃を撃っても斬撃の弧が出た");
        }
    }
}
