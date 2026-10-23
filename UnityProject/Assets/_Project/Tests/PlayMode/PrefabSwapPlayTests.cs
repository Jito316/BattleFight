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
    /// <summary>見た目の差し替え: プレハブの見た目で出るか・別の見た目に替えられるか・アニメーションの合図が出るか</summary>
    public class PrefabSwapPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        ArenaDirector mDirector;
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

        EnemyProfile FirstEnemy() => mDirector.Stages[1].Waves[0].Enemies[0];

        [UnityTest]
        public IEnumerator Player_HoldsTheWeaponPrefab_InTheVisualsHand()
        {
            yield return LoadEmptyArena();
            var aVisual = CharacterVisual.Of(mPlayer);
            Assert.NotNull(aVisual, "プレイヤーに CharacterVisual がない");
            var aWeapon = aVisual.HandAnchor.GetComponentInChildren<WeaponVisual>(false);
            Assert.NotNull(aWeapon, "手に武器(WeaponVisual)が付いていない");
        }

        [UnityTest]
        public IEnumerator Enemies_SpawnFromTheirPrefab()
        {
            yield return LoadEmptyArena();
            var aProfile = FirstEnemy();
            Assert.NotNull(aProfile.Prefab, "(前提)敵のデータにプレハブがない");
            var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.position + mPlayer.forward * 8f);
            yield return null;
            Assert.NotNull(CharacterVisual.Of(aEnemy), "敵に CharacterVisual がない");
            Assert.NotNull(aEnemy.GetComponent<CharacterAnimation>(), "敵にアニメーションの合図がない");
        }

        [UnityTest]
        public IEnumerator SwappingTheEnemyPrefab_ChangesItsLook()
        {
            yield return LoadEmptyArena();
            // アーティストが作った見た目のつもりの、別のプレハブ(中身は球 1 つ)
            // (中身を動かす部品は持たないので、置いたままでも動かない。遠くに置いておく)
            var aTemplate = new GameObject("ArtistEnemy");
            aTemplate.transform.position = new Vector3(0f, -200f, 0f);
            EnemyFactory.ConfigureBody(aTemplate.AddComponent<CharacterController>());
            var aVisualRoot = new GameObject(PrimitiveVisuals.VisualName);
            aVisualRoot.transform.SetParent(aTemplate.transform, false);
            var aModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aModel.name = "ArtistModel";
            Object.Destroy(aModel.GetComponent<Collider>());
            aModel.transform.SetParent(aVisualRoot.transform, false);
            aVisualRoot.AddComponent<CharacterVisual>().Setup(aModel.transform, null, null, null);

            var aProfile = Object.Instantiate(FirstEnemy());
            aProfile.Prefab = aTemplate;
            var aEnemy = mDirector.SpawnExtra(aProfile, mPlayer.position + mPlayer.forward * 8f);
            yield return null;
            Assert.NotNull(aEnemy.transform.Find(PrimitiveVisuals.VisualName + "/ArtistModel"), "差し替えたプレハブの見た目で出ない");
            Assert.AreEqual(FirstEnemy().MaxHealth, aEnemy.Damageable.MaxHealth, 1e-3f, "見た目を替えると、中身(体力)まで変わってしまう");
            Object.Destroy(aTemplate);
        }

        [UnityTest]
        public IEnumerator Player_SendsAnimationSignals()
        {
            yield return LoadEmptyArena();
            var aAnimation = mPlayer.GetComponent<CharacterAnimation>();
            Assert.NotNull(aAnimation);

            yield return Tap(mKeyboard.jKey);
            Assert.AreEqual(CharacterAnimation.AttackTrigger, aAnimation.LastAction, "攻撃しても Attack の合図が出ない");
            yield return new WaitForSeconds(0.8f);

            yield return Tap(mKeyboard.spaceKey);
            Assert.AreEqual(CharacterAnimation.JumpTrigger, aAnimation.LastAction, "ジャンプしても Jump の合図が出ない");
            yield return WaitUntil(() => aAnimation.LastAction == CharacterAnimation.LandTrigger, 3f);

            mPlayer.GetComponent<Damageable>().ApplyHit(new HitInfo { Damage = 5f, Stagger = 100f, Direction = -mPlayer.forward });
            Assert.AreEqual(CharacterAnimation.HitTrigger, aAnimation.LastAction, "攻撃を受けても Hit の合図が出ない");
            yield return new WaitForSeconds(1f);

            Press(mKeyboard.qKey);
            yield return null;
            yield return null;
            Assert.IsTrue(aAnimation.IsGuarding, "ガードしても Guard にならない");
            Release(mKeyboard.qKey);
            yield return null;
            yield return null;
            Assert.IsFalse(aAnimation.IsGuarding);
        }

        [UnityTest]
        public IEnumerator EnemyAttack_SendsTheAttackSignal()
        {
            yield return LoadEmptyArena();
            var aEnemy = mDirector.SpawnExtra(FirstEnemy(), mPlayer.position + mPlayer.forward * 3f);
            aEnemy.Damageable.Configure(Team.Enemy, 100000f, 0f, 0f);
            yield return null;
            Assert.IsTrue(aEnemy.ForceAttack(0));
            Assert.AreEqual(CharacterAnimation.AttackTrigger, aEnemy.GetComponent<CharacterAnimation>().LastAction);
        }

        [UnityTest]
        public IEnumerator TurningOffProceduralMotion_LeavesTheBodyToTheAnimator()
        {
            yield return LoadEmptyArena();
            var aVisual = CharacterVisual.Of(mPlayer);
            aVisual.ProceduralMotion = false;
            var aBody = aVisual.Body;
            var aRotation = aBody.localRotation;
            var aScale = aBody.localScale;
            mPlayer.GetComponent<Damageable>().ApplyHit(new HitInfo { Damage = 5f, Stagger = 100f, Direction = -mPlayer.forward });
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(aRotation, aBody.localRotation, "仮の動きを止めても、コードが体を傾けている");
            Assert.AreEqual(aScale, aBody.localScale, "仮の動きを止めても、コードが体をつぶしている");
            aVisual.ProceduralMotion = true;
        }
    }
}
