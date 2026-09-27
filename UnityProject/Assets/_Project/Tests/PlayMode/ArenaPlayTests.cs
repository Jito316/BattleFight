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

        IEnumerator LoadArena()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            slots = Object.FindFirstObjectByType<SkillSlotController>();
            executor = Object.FindFirstObjectByType<SkillExecutor>();
            style = Object.FindFirstObjectByType<StyleRankSystem>();
            player = executor.transform;
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
