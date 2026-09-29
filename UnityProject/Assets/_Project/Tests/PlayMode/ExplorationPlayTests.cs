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
        const string SceneName = "Prototype_Arena";

        Keyboard keyboard;
        SkillSlotController slots;
        SkillExecutor executor;
        ArenaDirector director;
        ExplorationRegion region;
        Transform player;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            base.TearDown();
        }

        IEnumerator LoadExploration()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            slots = Object.FindFirstObjectByType<SkillSlotController>();
            executor = Object.FindFirstObjectByType<SkillExecutor>();
            director = Object.FindFirstObjectByType<ArenaDirector>();
            player = executor.transform;

            int index = -1;
            for (int i = 0; i < director.Stages.Count; i++)
            {
                if (director.Stages[i].kind == StageKind.Exploration) index = i;
            }
            Assert.GreaterOrEqual(index, 0, "探索のステージがない");
            director.StartStage(index);
            region = director.Exploration;
            yield return WaitUntil(() => director.State == ArenaDirector.GameState.Fighting, 5f);
        }

        SkillData Skill(string assetName) => slots.Database.Find(assetName);

        static T FindInRegion<T>(string name) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == name);

        /// <summary>マップの原点からの相対位置へプレイヤーを動かす</summary>
        void Teleport(Vector3 local, Vector3 forward)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.position = region.transform.TransformPoint(local);
            player.rotation = Quaternion.LookRotation(forward);
            controller.enabled = true;
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        static IEnumerator WaitUntil(Func<bool> condition, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeout) Assert.Fail("タイムアウトしました");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Exploration_StartsWithSwordOnly_AndHidesArena()
        {
            yield return LoadExploration();

            Assert.IsTrue(slots.IsCollecting);
            Assert.AreEqual(SwapMode.Preset, slots.Mode, "探索はプリセット方式だけ");
            Assert.AreEqual("連斬", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.AreEqual("居合", slots.GetCurrent(SlotType.AttackB).displayName);
            Assert.AreEqual("ステップ斬り", slots.GetCurrent(SlotType.Movement).displayName);
            Assert.IsFalse(slots.IsAvailable(Skill("Skill_Hammer_Combo")), "まだ手に入れていないスキルが選べる");
            Assert.IsFalse(slots.AssignPresetSkill(0, SlotType.AttackA, Skill("Skill_Hammer_Combo")), "手に入れていないスキルを編成できた");
            Assert.AreEqual(3, ExplorationSave.UnlockedCount);
            Assert.AreEqual(15, region.TotalSkills);

            Assert.IsTrue(region.gameObject.activeInHierarchy, "探索マップが出ていない");
            Assert.IsNull(GameObject.Find("Arena"), "探索中にアリーナの地形が出ている");
            Assert.AreEqual("入口の広間", region.CurrentRoomName());

            // 空のプリセットには切り替わらない(何もできなくなるので)
            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(0, slots.PresetIndex);
        }

        [UnityTest]
        public IEnumerator Pickup_UnlocksSkill_AndEquipsItInAnEmptyPreset()
        {
            yield return LoadExploration();
            var hammer = Skill("Skill_Hammer_Combo");
            var pickup = FindInRegion<SkillPickup>("pickup_hammer_combo");

            Teleport(pickup.transform.localPosition - Vector3.up, Vector3.forward);
            yield return WaitUntil(() => ExplorationSave.IsUnlocked(hammer), 3f);

            Assert.IsFalse(pickup.gameObject.activeSelf, "取ったスキルが残っている");
            Assert.IsTrue(slots.IsAvailable(hammer));
            Assert.AreEqual(hammer, slots.Presets[1].Get(SlotType.AttackA), "空いているプリセットに入っていない");
            StringAssert.Contains("重撃", region.Message);

            // 入れたプリセットに切り替えられる
            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(hammer, slots.GetCurrent(SlotType.AttackA));
        }

        [UnityTest]
        public IEnumerator CrackedWall_BreaksOnlyWithHammer()
        {
            yield return LoadExploration();
            var wall = FindInRegion<BreakableWall>("wall_hall_north");
            Teleport(new Vector3(0f, 0f, 12.2f), Vector3.forward);
            yield return new WaitForSeconds(0.3f);

            // 剣では壊れない
            yield return Tap(keyboard.jKey);
            yield return WaitUntil(() => !executor.IsBusy, 3f);
            Assert.IsTrue(wall.gameObject.activeSelf, "剣で壁が壊れた");
            StringAssert.Contains("大槌", region.Message, "壊せないときのヒントが出ていない");

            // 大槌の技なら壊れる
            var hammer = Skill("Skill_Hammer_Combo");
            ExplorationSave.Unlock(hammer);
            Assert.IsTrue(slots.AssignPresetSkill(0, SlotType.AttackA, hammer));
            Teleport(new Vector3(0f, 0f, 12.2f), Vector3.forward);
            yield return new WaitForSeconds(0.3f);
            yield return Tap(keyboard.jKey);
            yield return WaitUntil(() => !wall.gameObject.activeSelf, 3f);
            Assert.IsTrue(ExplorationSave.HasFlag("wall_hall_north"), "壊したことが保存されていない");
        }

        [UnityTest]
        public IEnumerator Encounter_SpawnsEnemies_WhenEnteringTheRoom()
        {
            yield return LoadExploration();
            Assert.AreEqual(0, EnemyController.Active.Count, "部屋に入る前から敵がいる");

            Teleport(new Vector3(30f, 0f, 0f), Vector3.right);
            yield return WaitUntil(() => EnemyController.Active.Count >= 3, 3f);
            Assert.AreEqual("兵舎", region.CurrentRoomName());
        }

        [UnityTest]
        public IEnumerator FallingIntoTheChasm_ReturnsToTheLastSafePoint()
        {
            yield return LoadExploration();
            var damageable = player.GetComponent<Damageable>();

            // 谷の手前の安全地点を通ってから、谷へ落ちる
            Teleport(new Vector3(-24f, 3f, 9f), Vector3.forward);
            yield return new WaitForSeconds(0.2f);
            Teleport(new Vector3(-24f, 3f, 20f), Vector3.forward);
            // (庭の敵にも攻撃されうるので、体力ではなく高さで「落ちて戻った」を見る)
            yield return WaitUntil(() => player.position.y < -2f, 5f);
            yield return WaitUntil(() => player.position.y > 2f, 5f);
            StringAssert.Contains("落ちて", region.Message);
            Assert.Less(damageable.Health, damageable.MaxHealth, "落ちてもダメージを受けていない");

            var local = region.transform.InverseTransformPoint(player.position);
            Assert.Greater(local.y, 2f, "谷の底から戻っていない");
            Assert.Less(Vector3.Distance(new Vector3(local.x, 0f, local.z), new Vector3(-24f, 0f, 9f)), 2f, "最後の安全地点に戻っていない");
        }

        [UnityTest]
        public IEnumerator Altar_SavesCheckpoint_AndRestartsThere()
        {
            yield return LoadExploration();
            Teleport(new Vector3(-24f, 3f, 37f), Vector3.back);
            yield return WaitUntil(() => ExplorationSave.Checkpoint == "cp_tower", 3f);

            // 倒れて再開すると、塔の祭壇から始まる
            player.GetComponent<Damageable>().Kill();
            yield return WaitUntil(() => director.State == ArenaDirector.GameState.GameOver, 3f);
            director.Retry();
            yield return null;
            yield return null;
            director = Object.FindFirstObjectByType<ArenaDirector>();
            region = director.Exploration;
            player = Object.FindFirstObjectByType<SkillExecutor>().transform;
            yield return WaitUntil(() => director.State == ArenaDirector.GameState.Fighting, 5f);
            Assert.AreEqual("塔の麓", region.CurrentRoomName());
        }
    }
}
