using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>手ごたえ: 当てた本人と敵だけが止まるヒットストップ・敵ののけぞり・効果音</summary>
    public class HitFeelPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        SkillExecutor mExecutor;
        SkillSlotController mSlots;
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
            Time.captureDeltaTime = 0f;
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
            mSlots = mExecutor.GetComponent<SkillSlotController>();
            mPlayer = mExecutor.transform;
            mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            mSlots.SetMode(SwapMode.Preset);
            mDirector.StartStage(1);
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            mDirector.StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            yield return null;
        }

        EnemyProfile mGrunt => mDirector.Stages[1].Waves[0].Enemies[0];

        EnemyController Spawn(Vector3 iOffset)
        {
            var aEnemy = mDirector.SpawnExtra(mGrunt, mPlayer.position + iOffset);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
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

        void Hit(string iSkill, EnemyController iEnemy) =>
            mExecutor.ResolveHits(new SkillHitContext { Skill = mSlots.Database.Find(iSkill), Multiplier = 1f, SwapStrikeId = -1 },
                new List<Damageable> { iEnemy.Damageable }, mPlayer.position);

        [UnityTest]
        public IEnumerator Hitstop_FreezesOnlyTheAttackerAndTheTarget()
        {
            yield return LoadEmptyArena();
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);
            var aTarget = Spawn(mPlayer.forward * 1.6f);
            var aBystander = Spawn(mPlayer.forward * 12f + mPlayer.right * 6f);
            yield return null;

            // 最初の編成(剣士)の連斬を当てる
            float aHealth = aTarget.Damageable.Health;
            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => aTarget.Damageable.Health < aHealth, 2f);

            Assert.IsTrue(aTarget.IsFrozen, "当てた敵が止まっていない");
            Assert.IsTrue(mExecutor.IsHitFrozen, "当てたプレイヤーが止まっていない");
            Assert.IsFalse(aBystander.IsFrozen, "当てていない敵まで止まった");
            Assert.AreEqual(1f, Time.timeScale, "ゲーム全体が遅くなっている(全体のヒットストップのまま)");

            yield return new WaitForSecondsRealtime(WeaponFeel.MaxHitstop + 0.05f);
            Assert.IsFalse(aTarget.IsFrozen);
            Assert.IsFalse(mExecutor.IsHitFrozen);
        }

        [UnityTest]
        public IEnumerator Enemy_RecoilsAndSquashes_ThenSpringsBack()
        {
            yield return LoadEmptyArena();
            var aEnemy = Spawn(mPlayer.forward * 3f);
            yield return null;
            var aVisual = aEnemy.transform.Find(EnemyController.VisualName);
            Assert.NotNull(aVisual, "見た目の部品が Visual にまとまっていない");

            Hit("Skill_Hammer_Combo", aEnemy);
            Assert.IsTrue(aEnemy.IsReacting);
            yield return null;
            Assert.AreNotEqual(Vector3.one, aVisual.localScale, "当たってもつぶれない");

            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(aEnemy.IsReacting);
            Assert.AreEqual(Vector3.one, aVisual.localScale);
            Assert.AreEqual(Quaternion.identity, aVisual.localRotation);
        }

        [UnityTest]
        public IEnumerator Hits_PlayTheWeaponsSound()
        {
            yield return LoadEmptyArena();
            var aEnemy = Spawn(mPlayer.forward * 3f);
            yield return null;

            int aBefore = SoundPlayer.PlayCount;
            Hit("Skill_Hammer_Combo", aEnemy);
            Assert.Greater(SoundPlayer.PlayCount, aBefore, "当てても音が鳴らない");
            Assert.AreEqual(Sfx.HitHammer, SoundPlayer.LastPlayed);

            yield return new WaitForSecondsRealtime(0.1f);
            Hit("Skill_Gun_Rapid", aEnemy);
            Assert.AreEqual(Sfx.HitGun, SoundPlayer.LastPlayed, "武器で音が変わらない");
        }

        [UnityTest]
        public IEnumerator FastBullets_DoNotPassThroughEnemies_EvenAtLowFrameRates()
        {
            yield return LoadEmptyArena();
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);
            yield return Tap(mKeyboard.digit3Key);
            Assert.AreEqual("Skill_Gun_Rapid", mSlots.GetCurrent(SlotType.AttackA).name, "ガンナーの攻撃Aがラピッドショットではない");
            var aEnemy = Spawn(mPlayer.forward * 7f);
            yield return null;

            // 15 fps 相当: 弾(秒速 55)は 1 フレームで約 3.7 進み、敵の幅(約 1)を飛び越える
            Time.captureDeltaTime = 1f / 15f;
            float aHealth = aEnemy.Damageable.Health;
            yield return Tap(mKeyboard.jKey);
            float aStart = Time.realtimeSinceStartup;
            while (aEnemy.Damageable.Health >= aHealth && Time.realtimeSinceStartup - aStart < 3f) yield return null;
            Time.captureDeltaTime = 0f;
            Assert.Less(aEnemy.Damageable.Health, aHealth, "速い弾が敵をすり抜けた");
        }
    }
}
