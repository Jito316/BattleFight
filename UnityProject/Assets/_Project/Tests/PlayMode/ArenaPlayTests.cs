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
    /// <summary>試作アリーナを実際に動かし、キー入力で切り替えと戦闘ができるかを確かめる。</summary>
    public class ArenaPlayTests : InputTestFixture
    {
        const string SceneName = "Prototype_Arena";

        Keyboard keyboard;
        SkillSlotController slots;
        SkillExecutor executor;
        StyleRankSystem style;
        Transform player;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        IEnumerator LoadArena(SwapMode mode = SwapMode.Rack)
        {
            // 前回保存した編成に左右されないようにする
            LoadoutStorage.Clear();
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            slots = Object.FindFirstObjectByType<SkillSlotController>();
            executor = Object.FindFirstObjectByType<SkillExecutor>();
            style = Object.FindFirstObjectByType<StyleRankSystem>();
            player = executor.transform;
            slots.SetMode(mode);
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            LoadoutStorage.Clear();
            base.TearDown();
        }

        void MakePlayerSturdy() => player.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);

        /// <summary>ウェーブを止めて敵を消す(被弾で技が中断されると困るテスト用)</summary>
        static void ClearEnemies()
        {
            Object.FindFirstObjectByType<ArenaDirector>().StopAllCoroutines();
            foreach (var enemy in EnemyController.Active.ToArray()) Object.Destroy(enemy.gameObject);
        }

        IEnumerator UseSlot(SlotType slot, float timeout = 6f)
        {
            var keys = new[] { keyboard.jKey, keyboard.kKey, keyboard.lKey };
            yield return WaitUntil(() => !executor.IsBusy && !executor.IsStunned && executor.GetComponent<PlayerMotor>().IsGrounded, timeout);
            yield return Tap(keys[(int)slot]);
            yield return WaitUntil(() => !executor.IsBusy, timeout);
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
        public IEnumerator SwapKeys_CycleEachSlotInRealTime()
        {
            yield return LoadArena();

            Assert.AreEqual("連斬", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.AreEqual("居合", slots.GetCurrent(SlotType.AttackB).displayName);
            Assert.AreEqual("ステップ斬り", slots.GetCurrent(SlotType.Movement).displayName);
            Assert.AreEqual(WeaponBonusKind.Mastery, slots.Bonus.Kind);

            yield return Tap(keyboard.digit1Key);
            Assert.AreEqual("重撃", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.AreEqual(WeaponBonusKind.Synergy, slots.Bonus.Kind);
            Assert.IsTrue(executor.SwapStrikeReady);

            yield return Tap(keyboard.digit2Key);
            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual("引き寄せ", slots.GetCurrent(SlotType.AttackB).displayName);
            Assert.AreEqual(WeaponBonusKind.Arsenal, slots.Bonus.Kind);

            yield return Tap(keyboard.digit3Key);
            Assert.AreEqual("ハンマージャンプ", slots.GetCurrent(SlotType.Movement).displayName);
        }

        [UnityTest]
        public IEnumerator Attack_StartsCombo_AndSwapDuringRecoveryCancelsIt()
        {
            yield return LoadArena();
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            yield return Tap(keyboard.jKey);
            Assert.IsTrue(executor.IsBusy);
            Assert.AreEqual("連斬", executor.Current.displayName);

            yield return WaitUntil(() => executor.DebugLabel.Contains("Recovery"), 3f);
            yield return Tap(keyboard.digit1Key);

            Assert.IsFalse(executor.IsBusy, "硬直中の切り替えで技がキャンセルされていない");
            Assert.AreEqual("重撃", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.IsTrue(style.Announcements.Exists(a => a.text == "SWAP CANCEL"));
        }

        [UnityTest]
        public IEnumerator Attack_DamagesEnemy_AndRaisesStyle()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);

            // 敵をプレイヤーの目の前に置く
            var enemy = EnemyController.Active[0];
            enemy.transform.position = player.position + player.forward * 1.6f;
            Physics.SyncTransforms();
            float before = enemy.Damageable.Health;

            yield return Tap(keyboard.jKey);
            yield return WaitUntil(() => enemy == null || enemy.Damageable.Health < before, 2f);

            Assert.Greater(style.Meter.Points, 0f);
        }

        [UnityTest]
        public IEnumerator Wire_ZipsToNearestVisiblePoint_AndHangs()
        {
            yield return LoadArena();
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            // 目の前の近いポイントと、同じ方向の遠いポイントを置く
            var near = new GameObject("TestPoint_Near").AddComponent<GrapplePoint>();
            near.transform.position = player.position + player.forward * 7f + Vector3.up * 4f;
            var far = new GameObject("TestPoint_Far").AddComponent<GrapplePoint>();
            far.transform.position = player.position + player.forward * 14f + Vector3.up * 5f;

            // 移動スロットをワイヤーにする(ステップ斬り → ハンマージャンプ → ワイヤー)
            yield return Tap(keyboard.digit3Key);
            yield return Tap(keyboard.digit3Key);
            Assert.AreEqual("ワイヤー", slots.GetCurrent(SlotType.Movement).displayName);
            yield return null;
            Assert.AreSame(near, executor.GrapplePreview, "視界内で一番近いポイントが選ばれていない");

            yield return Tap(keyboard.lKey);
            yield return WaitUntil(() => executor.DebugLabel.Contains("Hang"), 3f);

            float distance = Vector3.Distance(player.position + Vector3.up * 1.8f, near.transform.position);
            Assert.Less(distance, 1.5f, "ポイントの下にぶら下がっていない");

            // ぶら下がり中は次の近いポイント(遠い方)が候補になる
            yield return null;
            Assert.AreSame(far, executor.GrapplePreview);
        }

        [UnityTest]
        public IEnumerator PresetKeys_SwitchAllSlotsAtOnce()
        {
            yield return LoadArena(SwapMode.Preset);

            Assert.AreEqual(0, slots.PresetIndex);
            Assert.AreEqual("連斬", slots.GetCurrent(SlotType.AttackA).displayName);

            yield return Tap(keyboard.digit2Key);
            Assert.AreEqual(1, slots.PresetIndex);
            Assert.AreEqual("魔弾", slots.GetCurrent(SlotType.AttackA).displayName);
            Assert.AreEqual("落雷", slots.GetCurrent(SlotType.AttackB).displayName);
            Assert.AreEqual("ブリンク", slots.GetCurrent(SlotType.Movement).displayName);
            Assert.AreEqual(WeaponBonusKind.Mastery, slots.Bonus.Kind);
            Assert.AreEqual(WeaponType.Staff, slots.Bonus.Weapon);
            Assert.IsTrue(executor.SwapStrikeReady);

            yield return Tap(keyboard.digit4Key);
            Assert.AreEqual(3, slots.PresetIndex);
            Assert.AreEqual(WeaponBonusKind.Arsenal, slots.Bonus.Kind);
        }

        [UnityTest]
        public IEnumerator PresetSwitch_DuringRecovery_CancelsTheSkill()
        {
            yield return LoadArena(SwapMode.Preset);
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);

            yield return Tap(keyboard.jKey);
            Assert.AreEqual("連斬", executor.Current.displayName);
            yield return WaitUntil(() => executor.DebugLabel.Contains("Recovery"), 3f);
            Assert.IsTrue(executor.CanPresetCancel(2));

            yield return Tap(keyboard.digit3Key);
            Assert.IsFalse(executor.IsBusy, "プリセットの切り替えで技がキャンセルされていない");
            Assert.IsTrue(style.Announcements.Exists(a => a.text == "SWAP CANCEL"));
        }

        [UnityTest]
        public IEnumerator EveryDatabaseSkill_CanBeUsedWithoutErrors()
        {
            yield return LoadArena(SwapMode.Preset);
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            yield return Tap(keyboard.tabKey);

            foreach (var skill in slots.Database.skills)
            {
                Assert.IsTrue(slots.AssignPresetSkill(0, skill.slot, skill), skill.name);
                yield return UseSlot(skill.slot);
            }
            // 弾や遅れて落ちる攻撃が消えるのを待つ
            yield return new WaitForSeconds(1.5f);
        }

        [UnityTest]
        public IEnumerator EveryMasteryFinisher_CanBeUsed()
        {
            yield return LoadArena(SwapMode.Preset);
            MakePlayerSturdy();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            ClearEnemies();
            yield return null;

            foreach (var weapon in slots.Weapons)
            {
                if (weapon.masteryFinisher == null) continue;
                for (int s = 0; s < 3; s++)
                {
                    SkillData pick = null;
                    foreach (var skill in slots.Database.ForSlot((SlotType)s))
                    {
                        if (skill.weapon == weapon.weapon)
                        {
                            pick = skill;
                            break;
                        }
                    }
                    Assert.NotNull(pick, $"{weapon.displayName} の {(SlotType)s} スキルがない");
                    slots.AssignPresetSkill(0, (SlotType)s, pick);
                }
                Assert.AreSame(weapon.masteryFinisher, slots.MasteryFinisher);

                yield return WaitUntil(() => !executor.IsBusy && !executor.IsStunned, 6f);
                yield return Tap(keyboard.fKey);
                Assert.AreSame(weapon.masteryFinisher, executor.Current, $"{weapon.displayName} のフィニッシャーが出ない");
                yield return WaitUntil(() => !executor.IsBusy, 8f);
            }
        }

        [UnityTest]
        public IEnumerator LoadoutEditor_PausesAndSaves()
        {
            yield return LoadArena(SwapMode.Preset);

            yield return Tap(keyboard.pKey);
            Assert.IsTrue(LoadoutEditorUI.IsOpen);
            Assert.IsTrue(GamePause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            var fireball = slots.Database.Find("Skill_Staff_Fireball");
            Assert.IsTrue(slots.AssignPresetSkill(0, SlotType.AttackB, fireball));
            Assert.AreSame(fireball, slots.GetCurrent(SlotType.AttackB));

            yield return Tap(keyboard.pKey);
            Assert.IsFalse(LoadoutEditorUI.IsOpen);
            Assert.IsFalse(GamePause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);

            Assert.IsTrue(LoadoutStorage.TryLoad(slots.Database, out var saved));
            Assert.AreEqual(SwapMode.Preset, saved.mode);
            Assert.AreSame(fireball, saved.presets[0].attackB);
        }

        [UnityTest]
        public IEnumerator EveryRackSkill_CanBeUsedWithoutErrors()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            yield return Tap(keyboard.tabKey);

            var keys = new[] { keyboard.jKey, keyboard.kKey, keyboard.lKey };
            var swaps = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key };
            for (int candidate = 0; candidate < SkillRack.Capacity; candidate++)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    yield return WaitUntil(() => !executor.IsBusy && executor.GetComponent<PlayerMotor>().IsGrounded, 5f);
                    yield return Tap(keys[slot]);
                    yield return WaitUntil(() => !executor.IsBusy, 5f);
                }
                foreach (var swap in swaps) yield return Tap(swap);
            }
        }
    }
}
