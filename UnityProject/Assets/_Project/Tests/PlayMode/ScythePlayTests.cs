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
    /// <summary>大鎌: 刈り取りで敵を引き寄せる・円月刈りで周りの敵を寄せる・死神の鎌が遠くの敵に当たる・影走りで長く進む</summary>
    public class ScythePlayTests : InputTestFixture
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

        /// <summary>当たったときの押し返しが、プレイヤーの方を向いていたか(敵が歩いて近づいただけでは通らないように)</summary>
        static bool[] WatchPull(EnemyController iEnemy, Transform iPlayer)
        {
            var aPulled = new bool[1];
            iEnemy.Damageable.Damaged += (iHit, _) =>
            {
                Vector3 aToPlayer = iPlayer.position - iEnemy.transform.position;
                aToPlayer.y = 0f;
                if (Vector3.Dot(iHit.Direction * iHit.Knockback, aToPlayer) > 0f) aPulled[0] = true;
            };
            return aPulled;
        }

        [UnityTest]
        public IEnumerator Reap_PullsTheEnemyCloser()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackA, "Skill_Scythe_Reap");
            var aEnemy = Spawn(mPlayer.position + mPlayer.forward * 2.8f);
            var aPulled = WatchPull(aEnemy, mPlayer);
            yield return null;
            float aBefore = Vector3.Distance(aEnemy.transform.position, mPlayer.position);
            yield return Tap(mKeyboard.jKey);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "刈り取りが当たらない");
            Assert.IsTrue(aPulled[0], "刈り取りの押し返しがプレイヤーの方を向いていない");
            Assert.Less(Vector3.Distance(aEnemy.transform.position, mPlayer.position), aBefore - 0.3f, "刈り取りで敵が引き寄せられない");
        }

        [UnityTest]
        public IEnumerator Harvest_HitsEnemiesBehind_AndPullsThemIn()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackB, "Skill_Scythe_Harvest");
            var aEnemy = Spawn(mPlayer.position - mPlayer.forward * 3f);
            var aPulled = WatchPull(aEnemy, mPlayer);
            yield return null;
            float aBefore = Vector3.Distance(aEnemy.transform.position, mPlayer.position);
            yield return Tap(mKeyboard.eKey);
            yield return new WaitForSeconds(0.7f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "円月刈りが背後の敵に当たらない");
            Assert.IsTrue(aPulled[0], "円月刈りの押し返しがプレイヤーの方を向いていない");
            Assert.Less(Vector3.Distance(aEnemy.transform.position, mPlayer.position), aBefore - 0.8f, "円月刈りで敵が引き寄せられない");
        }

        [UnityTest]
        public IEnumerator Doom_HitsAnEnemySevenMetersAway()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.AttackB, "Skill_Scythe_Doom");
            var aEnemy = Spawn(mPlayer.position + mPlayer.forward * 7f);
            aEnemy.Freeze(30f);
            yield return null;
            yield return Tap(mKeyboard.eKey);
            yield return new WaitForSeconds(1.2f);
            Assert.Less(aEnemy.Damageable.Health, aEnemy.Damageable.MaxHealth, "7m 先の敵に死神の鎌が当たらない");
        }

        [UnityTest]
        public IEnumerator Shade_DashesFarForward()
        {
            yield return LoadEmptyArena();
            Equip(SlotType.Movement, "Skill_Scythe_Shade");
            Vector3 aStart = mPlayer.position;
            Vector3 aForward = mPlayer.forward;
            yield return Tap(mKeyboard.leftShiftKey);
            yield return new WaitForSeconds(0.6f);
            Assert.Greater(Vector3.Dot(mPlayer.position - aStart, aForward), 4f, "影走りで前へ進まない");
        }
    }
}
