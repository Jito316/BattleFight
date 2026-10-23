using NUnit.Framework;

namespace BattleFight.Tests
{
    /// <summary>設定: 初期値・範囲・保存と読み直し・初期値に戻す</summary>
    public class GameSettingsTests
    {
        MemorySaveStore mStore;

        [SetUp]
        public void SetUp()
        {
            mStore = new MemorySaveStore();
            SaveProfiles.Store = mStore;
            GameSettings.Reload();
        }

        [TearDown]
        public void TearDown()
        {
            SaveProfiles.Store = new PlayerPrefsStore();
            GameSettings.Reload();
        }

        [Test]
        public void Defaults_ShowEverythingAsDesigned()
        {
            Assert.AreEqual(1f, GameSettings.Volume);
            Assert.IsFalse(GameSettings.Muted);
            Assert.AreEqual(1f, GameSettings.Shake);
            Assert.AreEqual(1f, GameSettings.Flash);
            Assert.AreEqual(1f, GameSettings.Zoom);
            Assert.IsTrue(GameSettings.Vibration);
            Assert.AreEqual(1f, GameSettings.CameraSensitivity);
        }

        [Test]
        public void Values_StayInRange()
        {
            GameSettings.Volume = 1.5f;
            Assert.AreEqual(1f, GameSettings.Volume);
            GameSettings.Shake = -0.2f;
            Assert.AreEqual(0f, GameSettings.Shake);
            GameSettings.CameraSensitivity = 9f;
            Assert.AreEqual(GameSettings.MaxCameraSensitivity, GameSettings.CameraSensitivity);
            GameSettings.CameraSensitivity = 0f;
            Assert.AreEqual(GameSettings.MinCameraSensitivity, GameSettings.CameraSensitivity);
        }

        [Test]
        public void Mute_SilencesButRemembersTheVolume()
        {
            GameSettings.Volume = 0.6f;
            GameSettings.Muted = true;
            Assert.AreEqual(0f, GameSettings.EffectiveVolume);
            GameSettings.Muted = false;
            Assert.AreEqual(0.6f, GameSettings.EffectiveVolume, 1e-5f, "ミュートを戻したら元の音量にならない");
        }

        [Test]
        public void Settings_AreSaved_AndCanBeReset()
        {
            GameSettings.Flash = 0.3f;
            GameSettings.Vibration = false;
            GameSettings.Reload();
            Assert.AreEqual(0.3f, GameSettings.Flash, 1e-5f, "読み直すと設定が消えている");
            Assert.IsFalse(GameSettings.Vibration);

            GameSettings.ResetToDefaults();
            Assert.AreEqual(1f, GameSettings.Flash);
            Assert.IsTrue(GameSettings.Vibration);
        }
    }
}
