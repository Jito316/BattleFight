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
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        SkillExecutor mExecutor;
        SkillSlotController mSlots;
        StyleRankSystem mStyle;
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

        /// <summary>第1幕を始めて、最初の敵を消す(自分で敵を置くテスト用)</summary>
        IEnumerator LoadEmptyArena()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            mDirector.StopAllCoroutines();
            foreach (var aEnemy in EnemyController.Active.ToArray()) Object.Destroy(aEnemy.gameObject);
            yield return null;
        }

        IEnumerator LoadArena()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            mSlots = mExecutor.GetComponent<SkillSlotController>();
            mStyle = mExecutor.GetComponent<StyleRankSystem>();
            mPlayer = mExecutor.transform;
            mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
            mSlots.SetMode(SwapMode.Preset);
            mDirector.StartStage(1);
        }

        EnemyProfile mGrunt => mDirector.Stages[1].Waves[0].Enemies[0];

        EnemyController SpawnInFront(float iDistance = 2.5f) => mDirector.SpawnExtra(mGrunt, mPlayer.position + mPlayer.forward * iDistance);

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

        void Hit(SkillData iSkill, Damageable iTarget) =>
            mExecutor.ResolveHits(new SkillHitContext { Skill = iSkill, Multiplier = 1f, SwapStrikeId = -1 },
                new List<Damageable> { iTarget }, mPlayer.position);

        [UnityTest]
        public IEnumerator JustDodge_SlowsOnlyTheEnemies()
        {
            yield return LoadEmptyArena();
            yield return WaitUntil(() => mExecutor.GetComponent<PlayerMotor>().IsGrounded, 3f);
            Assert.IsFalse(EnemyTime.IsSlowed);

            // 移動スキル(ステップ斬り)の無敵中に攻撃を受ける = ジャスト回避
            yield return Tap(mKeyboard.lKey);
            var aOutcome = mPlayer.GetComponent<Damageable>().ApplyHit(new HitInfo { Damage = 10f, Direction = Vector3.forward });
            Assert.AreEqual(HitOutcome.Evaded, aOutcome, "移動スキルの無敵で避けられていない");
            Assert.IsTrue(EnemyTime.IsSlowed, "ジャスト回避しても敵が遅くならない");
            Assert.Less(EnemyTime.Scale, 1f);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.Greater(Time.timeScale, 0.9f, "プレイヤー側の時間まで遅くなっている");
        }

        [UnityTest]
        public IEnumerator ChangeDrive_HitsNearbyEnemiesWithEveryPreset()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront(3f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            yield return null;

            // 満タンでないと出ない
            yield return Tap(mKeyboard.vKey);
            Assert.IsFalse(mExecutor.IsDriving);

            mExecutor.Drive.Add(1f);
            yield return Tap(mKeyboard.vKey);
            Assert.IsTrue(mExecutor.IsDriving, "満タンでもチェンジドライブが出ない");
            Assert.AreEqual(0f, mExecutor.Drive.Value);
            yield return WaitUntil(() => !mExecutor.IsDriving, 6f);

            Assert.Less(aEnemy.Damageable.Health, 100000f);
            // 4つのプリセットの武器で1回ずつ当たる
            Assert.GreaterOrEqual(mExecutor.Combo.Count, 4);
            Assert.GreaterOrEqual(mExecutor.SwapChain.Chain, 3, "持ち替えるたびにチェインがつながっていない");
        }

        [UnityTest]
        public IEnumerator Combo_CountsHits_AndBreaksWhenThePlayerIsHit()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront();
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            var aSword = mSlots.Database.Find("Skill_Sword_Combo");
            Hit(aSword, aEnemy.Damageable);
            Hit(aSword, aEnemy.Damageable);
            Hit(aSword, aEnemy.Damageable);
            Assert.AreEqual(3, mExecutor.Combo.Count);

            mExecutor.Interrupt(0.1f, 0.1f);
            Assert.AreEqual(0, mExecutor.Combo.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HigherRank_DealsMoreDamage()
        {
            yield return LoadEmptyArena();
            var aEnemy = SpawnInFront();
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            var aSword = mSlots.Database.Find("Skill_Sword_Combo");

            float aBefore = aEnemy.Damageable.Health;
            Hit(aSword, aEnemy.Damageable);
            float aLow = aBefore - aEnemy.Damageable.Health;

            mStyle.AddBonus(mStyle.Config.RankThresholds[mStyle.Config.RankThresholds.Length - 1] + 50f);
            Assert.AreEqual(mStyle.Config.RankNames.Length - 1, mStyle.Meter.RankIndex);
            aBefore = aEnemy.Damageable.Health;
            Hit(aSword, aEnemy.Damageable);
            float aHigh = aBefore - aEnemy.Damageable.Health;
            Assert.Greater(aHigh, aLow * 1.2f, "ランクを上げても攻撃力が上がらない");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SplitterElite_SplitsIntoTwoWhenDefeated()
        {
            yield return LoadEmptyArena();
            var aElite = mDirector.SpawnElite(mGrunt, EliteAffix.Splitter, mPlayer.position + mPlayer.forward * 6f);
            yield return null;
            Assert.AreEqual(EliteAffix.Splitter, aElite.Profile.Elite);
            Assert.Greater(aElite.Damageable.MaxHealth, mGrunt.MaxHealth);

            aElite.Damageable.Kill();
            yield return null;
            var aSplits = EnemyController.Active.Where(iE => iE.Profile.DisplayName.EndsWith("の分身")).ToList();
            Assert.AreEqual(2, aSplits.Count, "分裂のエリートを倒しても分身が出ない");
            Assert.IsTrue(mStyle.Announcements.Any(iA => iA.Text == "ELITE DOWN"));
        }

        [UnityTest]
        public IEnumerator Elites_AppearInWaves_WhenTheChanceIsSet()
        {
            ArenaDirector.EliteChanceOverride = 1f;
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0, 5f);
            Assert.IsTrue(EnemyController.Active.All(iE => iE.Profile.IsBoss || iE.Profile.Elite != EliteAffix.None),
                "確率を 100% にしてもエリートにならない敵がいる");
        }

        [UnityTest]
        public IEnumerator DefeatingTheLastEnemyOfAWave_SlowsTime()
        {
            yield return LoadArena();
            yield return WaitUntil(() => EnemyController.Active.Count > 0 && mDirector.State == ArenaDirector.GameState.Fighting, 5f);
            var aFeedback = Object.FindFirstObjectByType<CombatFeedback>();

            var aEnemies = EnemyController.Active.ToArray();
            for (int aI = 0; aI < aEnemies.Length - 1; aI++) aEnemies[aI].Damageable.Kill();
            yield return null;
            Assert.IsFalse(aFeedback.InSlowMotion, "最後の1体の前にスローになった");

            aEnemies[aEnemies.Length - 1].Damageable.Kill();
            yield return null;
            Assert.IsTrue(aFeedback.InSlowMotion, "最後の1体を倒してもスローにならない");
            Assert.Less(Time.timeScale, 1f);
        }
    }
}
