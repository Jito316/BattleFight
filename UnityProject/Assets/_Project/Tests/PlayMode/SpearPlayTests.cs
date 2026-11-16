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
    /// <summary>槍: 突きが遠くの敵に届く・薙ぎ払いで敵を押し返す・投げ槍が遠くの敵に当たる・棒高跳びで前へ高く跳ぶ</summary>
    public class SpearPlayTests : InputTestFixture
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

        EnemyController Spawn(Vector3 iPosition)
        {
            var aEnemy = mDirector.SpawnExtra(mDirector.Stages[1].Waves[0].Enemies[0], iPosition);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            return aEnemy;
        }

        [UnityTest]
        public IEnumerator Thrust_ReachesAnEnemyThreeMetersAway()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackA, "Skill_Spear_Combo");
            var aEnemy = Spawn(mPlayer.position + mPlayer.forward * 3f);
            aEnemy.Freeze(30f);
            yield return null;
            yield return Tap(mKeyboard.jKey);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "3m 先の敵に突きが届かない");
        }

        [UnityTest]
        public IEnumerator Sweep_PushesTheEnemyBack()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackB, "Skill_Spear_Sweep");
            var aEnemy = Spawn(mPlayer.position + mPlayer.forward * 1.8f);
            yield return null;
            float aBefore = Vector3.Distance(aEnemy.transform.position, mPlayer.position);
            yield return Tap(mKeyboard.eKey);
            yield return new WaitForSeconds(0.6f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "薙ぎ払いが当たらない");
            Assert.Greater(Vector3.Distance(aEnemy.transform.position, mPlayer.position), aBefore + 1f, "薙ぎ払いで敵が押し返されない");
        }

        [UnityTest]
        public IEnumerator Javelin_HitsAnEnemyTenMetersAway()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackB, "Skill_Spear_Javelin");
            var aEnemy = Spawn(mPlayer.position + mPlayer.forward * 10f);
            aEnemy.Freeze(30f);
            yield return null;
            yield return Tap(mKeyboard.eKey);
            yield return new WaitForSeconds(1.0f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "10m 先の敵に投げ槍が届かない");
        }

        [UnityTest]
        public IEnumerator Vault_JumpsHighAndForward()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.Movement, "Skill_Spear_Vault");
            Vector3 aStart = mPlayer.position;
            Vector3 aForward = mPlayer.forward;
            float aHighest = aStart.y;
            yield return Tap(mKeyboard.leftShiftKey);
            float aEnd = Time.time + 1.4f;
            while (Time.time < aEnd)
            {
                aHighest = Mathf.Max(aHighest, mPlayer.position.y);
                yield return null;
            }
            Assert.Greater(aHighest - aStart.y, 2f, "棒高跳びで高く跳ばない");
            Assert.Greater(Vector3.Dot(mPlayer.position - aStart, aForward), 4f, "棒高跳びで前へ進まない");
        }
    }
}
