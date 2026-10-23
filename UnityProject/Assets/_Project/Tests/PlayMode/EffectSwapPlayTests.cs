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
    /// <summary>演出の差し替え: 差し替え表にプレハブを入れると、いつもの演出の代わりにそれが出るか</summary>
    public class EffectSwapPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
        Transform mPlayer;
        GameObject mTemplate;

        public override void Setup()
        {
            base.Setup();
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            ArenaDirector.EliteChanceOverride = 0f;
        }

        public override void TearDown()
        {
            if (mTemplate != null) Object.Destroy(mTemplate);
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
            mPlayer = Object.FindFirstObjectByType<SkillExecutor>().transform;
            mPlayer.GetComponent<Damageable>().Configure(Team.Player, 100000f, 0f, 0f);
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

        /// <summary>アーティストが作った演出のつもりのプレハブ(パーティクル 1 つ)を、iCue に入れた差し替え表を使う</summary>
        EffectLibrary UseLibrary(EffectCue iCue)
        {
            mTemplate = new GameObject("ArtistEffect");
            mTemplate.SetActive(false);
            var aParticle = mTemplate.AddComponent<ParticleSystem>();
            var aMain = aParticle.main;
            aMain.duration = 0.5f;
            aMain.loop = false;
            var aLibrary = ScriptableObject.CreateInstance<EffectLibrary>();
            aLibrary.Set(iCue, mTemplate);
            CombatFeedback.Instance.Effects = aLibrary;
            return aLibrary;
        }

        static int CountActive(string iPrefix)
        {
            int aCount = 0;
            foreach (var aTransform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (aTransform.gameObject.activeInHierarchy && aTransform.name.StartsWith(iPrefix)) aCount++;
            }
            return aCount;
        }

        [UnityTest]
        public IEnumerator SwappedJumpEffect_ReplacesTheUsualDustRing()
        {
            yield return LoadEmptyArena();
            UseLibrary(EffectCue.Jump);
            yield return new WaitForSeconds(0.6f);
            int aRingsBefore = CountActive("Shockwave");

            yield return Tap(mKeyboard.spaceKey);
            Assert.AreEqual(1, CountActive("Cue_Jump"), "差し替えたジャンプの演出が出ない");
            Assert.AreEqual(aRingsBefore, CountActive("Shockwave"), "差し替えたのに、いつもの土煙の輪も出ている");
        }

        [UnityTest]
        public IEnumerator SwappedEffect_GetsTheSceneColor_AndGoesAway()
        {
            yield return LoadEmptyArena();
            UseLibrary(EffectCue.WeaponSwap);
            var aColor = new Color(0.2f, 0.9f, 0.4f);
            Assert.IsTrue(CombatFeedback.Instance.PlayCue(EffectCue.WeaponSwap, mPlayer.position, 1f, aColor));
            yield return null;
            ParticleSystem aParticle = null;
            foreach (var aSystem in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (aSystem.name.StartsWith("Cue_WeaponSwap")) aParticle = aSystem;
            }
            Assert.NotNull(aParticle, "差し替えた演出が出ない");
            Assert.AreEqual(aColor, aParticle.main.startColor.color, "場面の色がパーティクルに入らない");

            yield return new WaitForSeconds(aParticle.main.duration + aParticle.main.startLifetime.constantMax + 0.3f);
            Assert.AreEqual(0, CountActive("Cue_WeaponSwap"), "差し替えた演出が消えない");
        }

        [UnityTest]
        public IEnumerator UnswappedCue_KeepsTheUsualEffect()
        {
            yield return LoadEmptyArena();
            UseLibrary(EffectCue.Parry);
            Assert.IsFalse(CombatFeedback.Instance.PlayCue(EffectCue.Jump, mPlayer.position, 1f, Color.white), "決めていない場面まで差し替えになる");
            int aRingsBefore = CountActive("Shockwave");
            yield return Tap(mKeyboard.spaceKey);
            Assert.Greater(CountActive("Shockwave"), aRingsBefore, "差し替えていない場面で、いつもの演出が出ない");
        }

        [UnityTest]
        public IEnumerator SwappedDefeat_StillSendsTheShardsToThePlayer()
        {
            yield return LoadEmptyArena();
            UseLibrary(EffectCue.Defeat);
            var aEnemy = mDirector.SpawnExtra(mDirector.Stages[1].Waves[0].Enemies[0], mPlayer.position + mPlayer.forward * 6f);
            yield return null;
            aEnemy.Damageable.ApplyHit(new HitInfo { Damage = 100000f, Stagger = 100f, Direction = mPlayer.forward, Source = mPlayer.gameObject });
            yield return null;
            Assert.AreEqual(1, CountActive("Cue_Defeat"), "差し替えた撃破の演出が出ない");
            Assert.Greater(CountActive("Shard"), 0, "差し替えると、刻片がプレイヤーへ飛んでこない");
        }
    }
}
