using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>見切り・チェンジドライブ・コンボ・エリート・最後の1体のスロー・ランクによる強化を、実際の戦闘で確かめる</summary>
    public class FunPlayTests : InputTestFixture
    {
        const string SceneName = "Prototype_Arena";

        Keyboard keyboard;
        ArenaDirector director;
        SkillExecutor executor;
        SkillSlotController slots;
        StyleRankSystem style;
        Transform player;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
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

        /// <summary>第1幕を始めて、最初の敵を消す(自分で敵を置くテスト用)</summary>
        IEnumerator LoadEmptyArena()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            director.StopAllCoroutines();
            foreach (var enemy in EnemyController.Active.ToArray()) Object.Destroy(enemy.gameObject);
            yield return null;
        }

        IEnumerator LoadArena()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;
            director = Object.FindFirstObjectByType<ArenaDirector>();
            executor = Object.FindFirstObjectByType<SkillExecutor>();
            slots = executor.GetComponent<SkillSlotController>();
            style = executor.GetComponent<StyleRankSystem>();
            player = executor.transform;
            player.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            slots.SetMode(SwapMode.Preset);
            director.StartStage(1);
        }

        EnemyProfile Grunt => director.Stages[1].waves[0].enemies[0];

        EnemyController SpawnInFront(float distance = 2.5f) => director.SpawnExtra(Grunt, player.position + player.forward * distance);

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

        void Hit(SkillData skill, Damageable target) =>
            executor.ResolveHits(new SkillHitContext { skill = skill, multiplier = 1f, swapStrikeId = -1 },
                new List<Damageable> { target }, player.position);

        [UnityTest]
        public IEnumerator JustDodge_SlowsOnlyTheEnemies()
        {
            yield return LoadEmptyArena();
            yield return WaitUntil(() => executor.GetComponent<PlayerMotor>().IsGrounded, 3f);
            Assert.IsFalse(EnemyTime.IsSlowed);

            // 移動スキル(ステップ斬り)の無敵中に攻撃を受ける = ジャスト回避
            yield return Tap(keyboard.lKey);
            var outcome = player.GetComponent<Damageable>().ApplyHit(new HitInfo { damage = 10f, direction = Vector3.forward });
            Assert.AreEqual(HitOutcome.Evaded, outcome, "移動スキルの無敵で避けられていない");
            Assert.IsTrue(EnemyTime.IsSlowed, "ジャスト回避しても敵が遅くならない");
            Assert.Less(EnemyTime.Scale, 1f);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Greater(Time.timeScale, 0.9f, "プレイヤー側の時間まで遅くなっている");
        }

        [UnityTest]
        public IEnumerator ChangeDrive_HitsNearbyEnemiesWithEveryPreset()
        {
            yield return LoadEmptyArena();
            var enemy = SpawnInFront(3f);
            enemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            yield return null;

            // 満タンでないと出ない
            yield return Tap(keyboard.vKey);
            Assert.IsFalse(executor.IsDriving);

            executor.Drive.Add(1f);
            yield return Tap(keyboard.vKey);
            Assert.IsTrue(executor.IsDriving, "満タンでもチェンジドライブが出ない");
            Assert.AreEqual(0f, executor.Drive.Value);
            yield return WaitUntil(() => !executor.IsDriving, 6f);

            Assert.Less(enemy.Damageable.Health, 100000f);
            // 4つのプリセットの武器で1回ずつ当たる
            Assert.GreaterOrEqual(executor.Combo.Count, 4);
            Assert.GreaterOrEqual(executor.SwapChain.Chain, 3, "持ち替えるたびにチェインがつながっていない");
        }

        [UnityTest]
        public IEnumerator Combo_CountsHits_AndBreaksWhenThePlayerIsHit()
        {
            yield return LoadEmptyArena();
            var enemy = SpawnInFront();
            enemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            var sword = slots.Database.Find("Skill_Sword_Combo");
            Hit(sword, enemy.Damageable);
            Hit(sword, enemy.Damageable);
            Hit(sword, enemy.Damageable);
            Assert.AreEqual(3, executor.Combo.Count);

            executor.Interrupt(0.1f, 0.1f);
            Assert.AreEqual(0, executor.Combo.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HigherRank_DealsMoreDamage()
        {
            yield return LoadEmptyArena();
            var enemy = SpawnInFront();
            enemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            var sword = slots.Database.Find("Skill_Sword_Combo");

            float before = enemy.Damageable.Health;
            Hit(sword, enemy.Damageable);
            float low = before - enemy.Damageable.Health;

            style.AddBonus(style.Config.rankThresholds[style.Config.rankThresholds.Length - 1] + 50f);
            Assert.AreEqual(style.Config.rankNames.Length - 1, style.Meter.RankIndex);
            before = enemy.Damageable.Health;
            Hit(sword, enemy.Damageable);
            float high = before - enemy.Damageable.Health;
            Assert.Greater(high, low * 1.2f, "ランクを上げても攻撃力が上がらない");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SplitterElite_SplitsIntoTwoWhenDefeated()
        {
            yield return LoadEmptyArena();
            var elite = director.SpawnElite(Grunt, EliteAffix.Splitter, player.position + player.forward * 6f);
            yield return null;
            Assert.AreEqual(EliteAffix.Splitter, elite.Profile.elite);
            Assert.Greater(elite.Damageable.MaxHealth, Grunt.maxHealth);

            elite.Damageable.Kill();
            yield return null;
            var splits = EnemyController.Active.Where(e => e.Profile.displayName.EndsWith("の分身")).ToList();
            Assert.AreEqual(2, splits.Count, "分裂のエリートを倒しても分身が出ない");
            Assert.IsTrue(style.Announcements.Any(a => a.text == "ELITE DOWN"));
        }

        [UnityTest]
        public IEnumerator Elites_AppearInWaves_WhenTheChanceIsSet()
        {
            ArenaDirector.EliteChanceOverride = 1f;
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            Assert.IsTrue(EnemyController.Active.All(e => e.Profile.isBoss || e.Profile.elite != EliteAffix.None),
                "確率を 100% にしてもエリートにならない敵がいる");
        }

        [UnityTest]
        public IEnumerator DefeatingTheLastEnemyOfAWave_SlowsTime()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0 && director.State == ArenaDirector.GameState.Fighting, 5f);
            var feedback = Object.FindFirstObjectByType<CombatFeedback>();

            var enemies = EnemyController.Active.ToArray();
            for (int i = 0; i < enemies.Length - 1; i++) enemies[i].Damageable.Kill();
            yield return null;
            Assert.IsFalse(feedback.InSlowMotion, "最後の1体の前にスローになった");

            enemies[enemies.Length - 1].Damageable.Kill();
            yield return null;
            Assert.IsTrue(feedback.InSlowMotion, "最後の1体を倒してもスローにならない");
            Assert.Less(Time.timeScale, 1f);
        }
    }
}
