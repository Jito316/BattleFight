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
    /// <summary>大剣: 豪剣が当たる・剛断は溜めている間は出ず、離すと当たる・地裂きが遠くの敵に当たる・肩当てで前へ踏み込む</summary>
    public class GreatswordPlayTests : InputTestFixture
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

        void Equip(SlotType iSlot, string iSkillName)
        {
            var aSkill = mSlots.Database.Find(iSkillName);
            Assert.NotNull(aSkill, $"{iSkillName} が編成の一覧にない");
            Assert.IsTrue(mSlots.AssignPresetSkill(mSlots.PresetIndex, iSlot, aSkill));
        }

        EnemyController SpawnStill(Vector3 iPosition)
        {
            var aEnemy = mDirector.SpawnExtra(mDirector.Stages[1].Waves[0].Enemies[0], iPosition);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            aEnemy.Freeze(30f);
            return aEnemy;
        }

        [UnityTest]
        public IEnumerator Combo_HitsAnEnemyInFront()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackA, "Skill_Greatsword_Combo");
            var aEnemy = SpawnStill(mPlayer.position + mPlayer.forward * 2.2f);
            yield return null;
            yield return Tap(mKeyboard.jKey);
            yield return new WaitForSeconds(0.6f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "豪剣が前の敵に当たらない");
        }

        [UnityTest]
        public IEnumerator Charge_HitsOnlyAfterRelease()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackB, "Skill_Greatsword_Charge");
            var aEnemy = SpawnStill(mPlayer.position + mPlayer.forward * 2.2f);
            yield return null;
            Press(mKeyboard.eKey);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(aEnemy.Damageable.MaxHealth, aEnemy.Damageable.Health, "剛断が溜めている間に出てしまう");
            Release(mKeyboard.eKey);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "剛断を離しても当たらない");
        }

        [UnityTest]
        public IEnumerator Rift_HitsAnEnemySevenMetersAway()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackB, "Skill_Greatsword_Rift");
            var aEnemy = SpawnStill(mPlayer.position + mPlayer.forward * 7f);
            yield return null;
            yield return Tap(mKeyboard.eKey);
            yield return new WaitForSeconds(0.8f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "7m 先の敵に地裂きが当たらない");
        }

        [UnityTest]
        public IEnumerator Tackle_StepsForward()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.Movement, "Skill_Greatsword_Tackle");
            Vector3 aStart = mPlayer.position;
            Vector3 aForward = mPlayer.forward;
            yield return Tap(mKeyboard.leftShiftKey);
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(Vector3.Dot(mPlayer.position - aStart, aForward), 3f, "肩当てで前へ踏み込まない");
        }
    }
}
