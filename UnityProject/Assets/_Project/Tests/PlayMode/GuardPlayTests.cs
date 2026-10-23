using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>ガード・パリィ・居合のカウンターを、実際のキー操作で確かめる</summary>
    public class GuardPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        SkillExecutor mExecutor;
        PlayerGuard mGuard;
        Damageable mPlayer;
        StyleRankSystem mStyle;

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
            mGuard = mExecutor.GetComponent<PlayerGuard>();
            mPlayer = mExecutor.GetComponent<Damageable>();
            mStyle = mExecutor.GetComponent<StyleRankSystem>();
            mExecutor.GetComponent<SkillSlotController>().SetMode(SwapMode.Preset);
            mDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            mDirector.StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            yield return null;
            mPlayer.Configure(Team.Player, 100f, 0f, 0f);
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

        EnemyController SpawnInFront()
        {
            var aProfile = mDirector.Stages[1].Waves[0].Enemies[0];
            var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.transform.position + mPlayer.transform.forward * 1.5f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
        }

        /// <summary>前にいる敵から(iFromBehind なら後ろから)殴られる</summary>
        HitOutcome HitPlayer(EnemyController iEnemy, float iDamage, bool iFromBehind = false) => mPlayer.ApplyHit(new HitInfo
        {
            Damage = iDamage,
            Stagger = 100f,
            Knockback = 4f,
            Direction = iFromBehind ? mPlayer.transform.forward : -mPlayer.transform.forward,
            Source = iEnemy.gameObject,
        });

        [UnityTest]
        public IEnumerator HoldingGuard_BlocksFrontHits_WithLittleDamage()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront();
            Press(mKeyboard.qKey);
            // パリィの受付が終わってから受ける
            yield return new WaitForSecondsRealtime(GuardRules.ParryWindow + 0.1f);
            Assert.IsTrue(mGuard.IsGuarding, "Q でガードにならない");

            Assert.AreEqual(HitOutcome.Guarded, HitPlayer(aEnemy, 20f));
            Assert.AreEqual(100f - 20f * GuardRules.ChipRatio, mPlayer.Health, 1e-4f, "ガードで受けたダメージが減っていない");
            Assert.IsFalse(mExecutor.IsStunned, "ガードで受けたのにひるんだ");
            Assert.IsFalse(aEnemy.IsStaggered, "ガードしただけで敵がよろけた");
            Assert.IsTrue(aEnemy.IsFrozen, "受け止めたときに敵が止まらない");
            Assert.AreEqual(Sfx.Guard, SoundPlayer.LastPlayed);
            Release(mKeyboard.qKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Guard_DoesNotCoverTheBack()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront();
            Press(mKeyboard.qKey);
            yield return new WaitForSecondsRealtime(GuardRules.ParryWindow + 0.1f);
            Assert.AreEqual(HitOutcome.Hit, HitPlayer(aEnemy, 20f, true), "後ろからの攻撃まで受け止めた");
            Release(mKeyboard.qKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PressingGuardRightBeforeTheHit_Parries()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront();
            Press(mKeyboard.qKey);
            yield return null;

            Assert.AreEqual(HitOutcome.Parried, HitPlayer(aEnemy, 20f));
            Assert.AreEqual(100f, mPlayer.Health, "パリィしたのにダメージを受けた");
            Assert.IsTrue(aEnemy.IsStaggered, "パリィしても敵がよろけない");
            Assert.AreEqual(1, mGuard.ParryCount);
            Assert.IsTrue(mStyle.Announcements.Any(iAnnouncement => iAnnouncement.Text == "PARRY!"));
            Assert.Greater(CombatFeedback.Instance.FlashAmount, 0f, "パリィで画面が光らない");
            Release(mKeyboard.qKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CannotAttackWhileGuarding_ButCanRightAfter()
        {
            yield return LoadEmptyArena();
            Press(mKeyboard.qKey);
            yield return null;
            yield return Tap(mKeyboard.jKey);
            Assert.IsFalse(mExecutor.IsBusy, "ガード中に技が出た");

            Release(mKeyboard.qKey);
            yield return null;
            yield return Tap(mKeyboard.jKey);
            Assert.IsTrue(mExecutor.IsBusy, "ガードを放しても技が出ない");
        }

        [UnityTest]
        public IEnumerator IaiCounter_FlashesAndKnocksTheAttackerBack()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront();
            // 剣士の攻撃B = 居合。押している間は溜め(カウンターの構え)
            Press(mKeyboard.kKey);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(HitOutcome.Countered, HitPlayer(aEnemy, 20f));
            Assert.Greater(CombatFeedback.Instance.FlashAmount, 0f, "カウンターで画面が光らない");
            Assert.IsTrue(aEnemy.IsFrozen, "カウンターで敵が止まらない");
            Release(mKeyboard.kKey);
            yield return null;
        }
    }
}
