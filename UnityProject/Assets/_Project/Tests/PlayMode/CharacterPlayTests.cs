using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>プレイヤーの人型モデルとアニメーション(Quaternius)が組み込まれているか</summary>
    public class CharacterPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        CharacterAnimator mCharacter;
        SkillExecutor mExecutor;

        public override void Setup()
        {
            base.Setup();
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

        IEnumerator Load()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            mCharacter = Object.FindFirstObjectByType<CharacterAnimator>();
            mExecutor = Object.FindFirstObjectByType<SkillExecutor>();
            Object.FindFirstObjectByType<ArenaDirector>().StartStage(0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_HasAHumanoidModel_WithEveryMotion()
        {
            yield return Load();
            Assert.NotNull(mCharacter, "プレイヤーに CharacterAnimator がない");
            var aAnimator = mCharacter.Animator;
            Assert.NotNull(aAnimator);
            Assert.IsTrue(aAnimator.isHuman, "モデルが Humanoid になっていない");
            Assert.IsTrue(aAnimator.avatar.isValid, "アバターが正しく作れていない");
            float aFacing = Vector3.Dot(aAnimator.bodyRotation * Vector3.forward, mExecutor.transform.forward);
            Assert.Greater(aFacing, 0.7f, "モデルがプレイヤーの正面を向いていない");
            Assert.IsTrue(aAnimator.HasState(0, Animator.StringToHash(CharacterAnimator.LocomotionState)));
            Assert.IsTrue(aAnimator.HasState(0, Animator.StringToHash(CharacterAnimator.AirState)));
            foreach (var aState in CharacterAnimator.ActionStates)
            {
                Assert.IsTrue(aAnimator.HasState(0, Animator.StringToHash(aState)), $"{aState} のモーションがない");
            }
        }

        [UnityTest]
        public IEnumerator Attacking_PlaysTheSkillMotion_ThenReturnsToLocomotion()
        {
            yield return Load();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(CharacterAnimator.LocomotionState, mCharacter.CurrentState);

            Press(mKeyboard.jKey);
            yield return null;
            Release(mKeyboard.jKey);
            yield return null;
            Assert.IsTrue(mExecutor.IsBusy, "攻撃が出ていない");
            Assert.AreEqual(CharacterAnimator.ActionFor(mExecutor.Current, mExecutor.StepIndex), mCharacter.CurrentState);

            float aStart = Time.realtimeSinceStartup;
            while (mExecutor.IsBusy && Time.realtimeSinceStartup - aStart < 5f) yield return null;
            yield return null;
            Assert.AreEqual(CharacterAnimator.LocomotionState, mCharacter.CurrentState, "技が終わっても移動のモーションに戻らない");
        }

        [UnityTest]
        public IEnumerator WeaponFollowsTheRightHand()
        {
            yield return Load();
            yield return new WaitForSeconds(0.2f);
            var aHand = mCharacter.Animator.GetBoneTransform(HumanBodyBones.RightHand);
            var aAnchor = mExecutor.transform.Find("HandAnchor");
            Assert.NotNull(aAnchor);
            Assert.Less(Vector3.Distance(aHand.position, aAnchor.position), 0.01f, "武器が手の位置にない");
        }
    }
}
