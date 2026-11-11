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
    /// <summary>双剣: 交差ステップで敵の背後へ回る・回転斬りが後ろの敵にも当たる・刃を 2 本持つ</summary>
    public class DualBladesPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
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
            var aExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            mSlots = aExecutor.GetComponent<SkillSlotController>();
            mPlayer = aExecutor.transform;
            mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            mSlots.SetMode(SwapMode.Preset);
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

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
        }

        /// <summary>今のプリセットに双剣の技を入れる</summary>
        void Equip(SlotType iSlot, string iSkillName)
        {
            var aSkill = mSlots.Database.Find(iSkillName);
            Assert.NotNull(aSkill, $"{iSkillName} が編成の一覧にない");
            Assert.IsTrue(mSlots.AssignPresetSkill(mSlots.PresetIndex, iSlot, aSkill));
        }

        /// <summary>動かない、倒れない敵を置く</summary>
        EnemyController SpawnStill(Vector3 iPosition)
        {
            var aEnemy = mDirector.SpawnExtra(mDirector.Stages[1].Waves[0].Enemies[0], iPosition);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            // 止める(動きを切るとロックオンの対象からも外れるので、時間を止めるだけにする)
            aEnemy.Freeze(30f);
            return aEnemy;
        }

        [UnityTest]
        public IEnumerator CrossStep_GoesBehindTheLockedEnemy_AndFacesIt()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.Movement, "Skill_Dual_Cross");
            Vector3 aStart = mPlayer.position;
            var aEnemy = SpawnStill(aStart + mPlayer.forward * 5f);
            yield return null;
            yield return Tap(mKeyboard.tabKey);
            Assert.AreEqual(aEnemy, mPlayer.GetComponent<LockOnSystem>().Target, "(前提)ロックオンできない");

            yield return Tap(mKeyboard.leftShiftKey);
            yield return new WaitForSeconds(0.3f);
            Vector3 aForward = (aEnemy.transform.position - aStart).normalized;
            Vector3 aFromEnemy = mPlayer.position - aEnemy.transform.position;
            aFromEnemy.y = 0f;
            Assert.Greater(Vector3.Dot(aFromEnemy, aForward), 0.5f, "敵の背後に回り込めていない");
            Vector3 aToEnemy = -aFromEnemy.normalized;
            Assert.Greater(Vector3.Dot(mPlayer.forward, aToEnemy), 0.8f, "回り込んだあと、敵の方を向いていない");
        }

        static Vector3 Flat(Vector3 iVector) => new Vector3(iVector.x, 0f, iVector.z).normalized;

        [UnityTest]
        public IEnumerator CrossStep_DoesNotWhipTheCameraAround()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.Movement, "Skill_Dual_Cross");
            var aEnemy = SpawnStill(mPlayer.position + mPlayer.forward * 5f);
            yield return null;
            yield return Tap(mKeyboard.tabKey);
            // ロックオンして、カメラが敵の方を向くまで待つ
            yield return new WaitForSeconds(1f);
            var aCamera = Camera.main.transform;
            Vector3 aBefore = Flat(aCamera.forward);

            yield return Tap(mKeyboard.leftShiftKey);
            yield return new WaitForSeconds(0.25f);
            // 回り込んだ直後は、カメラを振り回さない(敵の向きは 180° 反対になっている)
            Assert.Less(Vector3.Angle(aBefore, Flat(aCamera.forward)), 30f, "回り込んだ直後に、カメラが大きく振り回された");

            // しばらくすると、敵の方へなめらかに向き直す
            yield return new WaitForSeconds(1.5f);
            Vector3 aToEnemy = Flat(aEnemy.transform.position - mPlayer.position);
            Assert.Greater(Vector3.Dot(Flat(aCamera.forward), aToEnemy), 0.8f, "回り込んだあと、カメラが敵の方へ向き直らない");
        }

        [UnityTest]
        public IEnumerator Spin_HitsAnEnemyBehindThePlayer()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackB, "Skill_Dual_Spin");
            var aEnemy = SpawnStill(mPlayer.position - mPlayer.forward * 1.6f);
            yield return null;
            yield return Tap(mKeyboard.eKey);
            yield return new WaitForSeconds(1f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "回転斬りが後ろの敵に当たらない");
        }

        [UnityTest]
        public IEnumerator DualBlades_HoldTwoBlades()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackA, "Skill_Dual_Combo");
            yield return null;
            var aHand = CharacterVisual.Of(mPlayer).HandAnchor;
            WeaponVisual aShown = null;
            foreach (var aWeapon in aHand.GetComponentsInChildren<WeaponVisual>(false)) aShown = aWeapon;
            Assert.NotNull(aShown, "手に武器がない");
            Assert.NotNull(aShown.transform.Find("Head2"), "双剣の 2 本目の刃がない");
        }
    }
}
