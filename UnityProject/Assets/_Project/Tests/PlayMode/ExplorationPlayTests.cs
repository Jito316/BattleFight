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
    /// <summary>探索モード(メトロイドヴァニア): スキルを集め、スキルで進入制限を越えられるかを確かめる。</summary>
    public class ExplorationPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        SkillSlotController mSlots;
        SkillExecutor mExecutor;
        ArenaDirector mDirector;
        ExplorationRegion mRegion;
        Transform mPlayer;

        public override void Setup()
        {
            base.Setup();
            // エリートはランダムに出るので、ここでは出さない(FunPlayTests で確かめる)
            ArenaDirector.EliteChanceOverride = 0f;
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            base.TearDown();
        }

        IEnumerator LoadExploration()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mSlots = Object.FindFirstObjectByType<SkillSlotController>();
            mExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mPlayer = mExecutor.transform;

            int aIndex = -1;
            for (int aStageIndex = 0; aStageIndex < mDirector.Stages.Count; aStageIndex++)
            {
                if (mDirector.Stages[aStageIndex].Kind == StageKind.Exploration) aIndex = aStageIndex;
            }
            Assert.GreaterOrEqual(aIndex, 0, "探索のステージがない");
            mDirector.StartStage(aIndex);
            mRegion = mDirector.Exploration;
            yield return WaitUntil(() => mDirector.State == ArenaDirector.GameState.Fighting, 5f);
        }

        SkillData Skill(string iAssetName) => mSlots.Database.Find(iAssetName);

        static T FindInRegion<T>(string iName) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(iComponent => iComponent.name == iName);

        /// <summary>マップの原点からの相対位置へプレイヤーを動かす</summary>
        void Teleport(Vector3 iLocal, Vector3 iForward)
        {
            var aController = mPlayer.GetComponent<CharacterController>();
            aController.enabled = false;
            mPlayer.position = mRegion.transform.TransformPoint(iLocal);
            mPlayer.rotation = Quaternion.LookRotation(iForward);
            aController.enabled = true;
        }

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
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

        [UnityTest]
        public IEnumerator Exploration_StartsWithSwordOnly_AndHidesArena()
        {
            yield return LoadExploration();

            Assert.IsTrue(mSlots.IsCollecting);
            Assert.AreEqual(SwapMode.Preset, mSlots.Mode, "探索はプリセット方式だけ");
            Assert.AreEqual("連斬", mSlots.GetCurrent(SlotType.AttackA).DisplayName);
            Assert.AreEqual("居合", mSlots.GetCurrent(SlotType.AttackB).DisplayName);
            Assert.AreEqual("ステップ斬り", mSlots.GetCurrent(SlotType.Movement).DisplayName);
            Assert.IsFalse(mSlots.IsAvailable(Skill("Skill_Hammer_Combo")), "まだ手に入れていないスキルが選べる");
            Assert.IsFalse(mSlots.AssignPresetSkill(0, SlotType.AttackA, Skill("Skill_Hammer_Combo")), "手に入れていないスキルを編成できた");
            Assert.AreEqual(3, ExplorationSave.UnlockedCount);
            Assert.AreEqual(15, mRegion.TotalSkills);

            Assert.IsTrue(mRegion.gameObject.activeInHierarchy, "探索マップが出ていない");
            Assert.IsNull(GameObject.Find("Arena"), "探索中にアリーナの地形が出ている");
            Assert.AreEqual("入口の広間", mRegion.CurrentRoomName());

            // 空のプリセットには切り替わらない(何もできなくなるので)
            yield return Tap(mKeyboard.digit2Key);
            Assert.AreEqual(0, mSlots.PresetIndex);
        }

        [UnityTest]
        public IEnumerator Pickup_UnlocksSkill_AndEquipsItInAnEmptyPreset()
        {
            yield return LoadExploration();
            var aHammer = Skill("Skill_Hammer_Combo");
            var aPickup = FindInRegion<SkillPickup>("pickup_hammer_combo");

            Teleport(aPickup.transform.localPosition - Vector3.up, Vector3.forward);
            yield return WaitUntil(() => ExplorationSave.IsUnlocked(aHammer), 3f);

            Assert.IsFalse(aPickup.gameObject.activeSelf, "取ったスキルが残っている");
            Assert.IsTrue(mSlots.IsAvailable(aHammer));
            Assert.AreEqual(aHammer, mSlots.Presets[1].Get(SlotType.AttackA), "空いているプリセットに入っていない");
            StringAssert.Contains("重撃", mRegion.Message);

            // 入れたプリセットに切り替えられる
            yield return Tap(mKeyboard.digit2Key);
            Assert.AreEqual(aHammer, mSlots.GetCurrent(SlotType.AttackA));
        }

        [UnityTest]
        public IEnumerator CrackedWall_BreaksOnlyWithHammer()
        {
            yield return LoadExploration();
            var aWall = FindInRegion<BreakableWall>("wall_hall_north");
            Teleport(new Vector3(0f, 0f, 12.2f), Vector3.forward);
            yield return new WaitForSeconds(0.3f);

            // 剣では壊れない
            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => !mExecutor.IsBusy, 3f);
            Assert.IsTrue(aWall.gameObject.activeSelf, "剣で壁が壊れた");
            StringAssert.Contains("大槌", mRegion.Message, "壊せないときのヒントが出ていない");

            // 大槌の技なら壊れる
            var aHammer = Skill("Skill_Hammer_Combo");
            ExplorationSave.Unlock(aHammer);
            Assert.IsTrue(mSlots.AssignPresetSkill(0, SlotType.AttackA, aHammer));
            Teleport(new Vector3(0f, 0f, 12.2f), Vector3.forward);
            yield return new WaitForSeconds(0.3f);
            yield return Tap(mKeyboard.jKey);
            yield return WaitUntil(() => !aWall.gameObject.activeSelf, 3f);
            Assert.IsTrue(ExplorationSave.HasFlag("wall_hall_north"), "壊したことが保存されていない");
        }

        [UnityTest]
        public IEnumerator Encounter_SpawnsEnemies_WhenEnteringTheRoom()
        {
            yield return LoadExploration();
            Assert.AreEqual(0, EnemyController.Active.Count, "部屋に入る前から敵がいる");

            Teleport(new Vector3(30f, 0f, 0f), Vector3.right);
            yield return WaitUntil(() => EnemyController.Active.Count >= 3, 3f);
            Assert.AreEqual("兵舎", mRegion.CurrentRoomName());
        }

        [UnityTest]
        public IEnumerator FallingIntoTheChasm_ReturnsToTheLastSafePoint()
        {
            yield return LoadExploration();
            var aDamageable = mPlayer.GetComponent<Damageable>();

            // 谷の手前の安全地点を通ってから、谷へ落ちる
            Teleport(new Vector3(-24f, 3f, 9f), Vector3.forward);
            yield return new WaitForSeconds(0.2f);
            Teleport(new Vector3(-24f, 3f, 20f), Vector3.forward);
            // (庭の敵にも攻撃されうるので、体力ではなく高さで「落ちて戻った」を見る)
            yield return WaitUntil(() => mPlayer.position.y < -2f, 5f);
            yield return WaitUntil(() => mPlayer.position.y > 2f, 5f);
            StringAssert.Contains("落ちて", mRegion.Message);
            Assert.Less(aDamageable.Health, aDamageable.MaxHealth, "落ちてもダメージを受けていない");

            var aLocal = mRegion.transform.InverseTransformPoint(mPlayer.position);
            Assert.Greater(aLocal.y, 2f, "谷の底から戻っていない");
            Assert.Less(Vector3.Distance(new Vector3(aLocal.x, 0f, aLocal.z), new Vector3(-24f, 0f, 9f)), 2f, "最後の安全地点に戻っていない");
        }

        [UnityTest]
        public IEnumerator Altar_SavesCheckpoint_AndRestartsThere()
        {
            yield return LoadExploration();
            Teleport(new Vector3(-24f, 3f, 37f), Vector3.back);
            yield return WaitUntil(() => ExplorationSave.Checkpoint == "cp_tower", 3f);

            // 倒れて再開すると、塔の祭壇から始まる
            mPlayer.GetComponent<Damageable>().Kill();
            yield return WaitUntil(() => mDirector.State == ArenaDirector.GameState.GameOver, 3f);
            mDirector.Retry();
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mRegion = mDirector.Exploration;
            mPlayer = Object.FindFirstObjectByType<SkillExecutor>().transform;
            yield return WaitUntil(() => mDirector.State == ArenaDirector.GameState.Fighting, 5f);
            Assert.AreEqual("塔の麓", mRegion.CurrentRoomName());
        }
    }
}
