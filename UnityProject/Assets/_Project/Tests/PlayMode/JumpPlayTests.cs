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
    /// <summary>ジャンプと着地の手ごたえ</summary>
    public class JumpPlayTests : InputTestFixture
    {
        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;
        PlayerMotor mMotor;
        PlayerJumpFeel mFeel;
        PlayerHitReaction mBody;

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

        IEnumerator Load()
        {
            LoadoutStorage.Clear();
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
            var aDirector = Object.FindFirstObjectByType<ArenaDirector>();
            mMotor = Object.FindFirstObjectByType<PlayerMotor>();
            mFeel = mMotor.GetComponent<PlayerJumpFeel>();
            mBody = mMotor.GetComponent<PlayerHitReaction>();
            aDirector.StartStage(0);
            yield return WaitUntil(() => mMotor.IsGrounded, 3f);
            yield return new WaitForSeconds(0.3f);
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

        [UnityTest]
        public IEnumerator Jumping_StretchesTheBody_AndLandingSquashesIt()
        {
            yield return Load();
            Assert.IsNotNull(mFeel, "プレイヤーに PlayerJumpFeel がない");
            int aLands = mFeel.LandCount;

            yield return Tap(mKeyboard.spaceKey);
            Assert.IsFalse(mMotor.IsGrounded, "ジャンプしていない");
            Assert.IsTrue(mBody.IsSquashing, "踏み切りで体が伸びない");
            Assert.AreEqual(Sfx.Jump, SoundPlayer.LastPlayed);

            yield return WaitUntil(() => mFeel.LandCount > aLands, 3f);
            Assert.AreEqual(Sfx.Land, SoundPlayer.LastPlayed, "着地の音が鳴らない");
            Assert.IsTrue(mBody.IsSquashing, "着地で体がつぶれない");

            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(mBody.IsSquashing, "体の形が元に戻らない");
        }

        [UnityTest]
        public IEnumerator FallingFromHigh_LandsHarder_AndDipsTheCamera()
        {
            yield return Load();
            int aLands = mFeel.LandCount;
            var aCamera = CombatFeedback.Instance.CameraRig;

            // 高いところから落とす
            mMotor.Teleport(Vector3.up * 9f);
            yield return WaitUntil(() => mFeel.LandCount > aLands, 4f);
            Assert.Greater(mFeel.LastLandStrength, 0.5f, "高いところから落ちても重い着地にならない");
            Assert.Less(aCamera.KickOffset.y, 0f, "着地でカメラが沈まない");
        }

        [UnityTest]
        public IEnumerator SmallSteps_DoNotCountAsLanding()
        {
            yield return Load();
            int aLands = mFeel.LandCount;
            // 歩いて段差を下りる程度(ごく短い空中)は着地にしない
            mMotor.Teleport(Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(aLands, mFeel.LandCount, "ちょっとした段差まで着地扱いになった");
        }
    }
}
