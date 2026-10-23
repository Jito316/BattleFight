using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>設定画面と、設定が演出に効くこと・演出の使い回し</summary>
    public class SettingsAndPoolPlayTests : InputTestFixture
    {
        /// <summary>テスト用: メモリに保存する(エディタの PlayerPrefs を汚さない)</summary>
        class MemoryStore : ISaveStore
        {
            readonly Dictionary<string, object> mValues = new Dictionary<string, object>();

            public bool HasKey(string iKey) => mValues.ContainsKey(iKey);
            public string GetString(string iKey, string iDefault) => mValues.TryGetValue(iKey, out var aValue) && aValue is string aText ? aText : iDefault;
            public void SetString(string iKey, string iValue) => mValues[iKey] = iValue;
            public int GetInt(string iKey, int iDefault) => mValues.TryGetValue(iKey, out var aValue) && aValue is int aNumber ? aNumber : iDefault;
            public void SetInt(string iKey, int iValue) => mValues[iKey] = iValue;
            public float GetFloat(string iKey, float iDefault) => mValues.TryGetValue(iKey, out var aValue) && aValue is float aNumber ? aNumber : iDefault;
            public void SetFloat(string iKey, float iValue) => mValues[iKey] = iValue;
            public void DeleteKey(string iKey) => mValues.Remove(iKey);
            public void Save() { }
        }

        const string mSceneName = "Prototype_Arena";

        Keyboard mKeyboard;

        public override void Setup()
        {
            base.Setup();
            mKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            ArenaDirector.EliteChanceOverride = 0f;
            SaveProfiles.Store = new MemoryStore();
            GameSettings.Reload();
        }

        public override void TearDown()
        {
            if (GamePause.IsPaused) GamePause.Set(false);
            SaveProfiles.Store = new PlayerPrefsStore();
            GameSettings.Reload();
            ArenaDirector.EliteChanceOverride = -1f;
            EnemyTime.Clear();
            base.TearDown();
        }

        IEnumerator Load()
        {
            ExplorationSave.Reset();
            SceneManager.LoadScene(mSceneName);
            yield return null;
            yield return null;
        }

        IEnumerator Tap(ButtonControl iKey)
        {
            Press(iKey);
            yield return null;
            Release(iKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsScreen_OpensWithO_PausesAndChangesValues()
        {
            yield return Load();
            Assert.IsFalse(SettingsUI.IsOpen);

            yield return Tap(mKeyboard.oKey);
            Assert.IsTrue(SettingsUI.IsOpen, "O で設定画面が開かない");
            Assert.IsTrue(GamePause.IsPaused, "設定画面を開いてもゲームが止まらない");

            // 1 行目(音量)を左へ
            yield return Tap(mKeyboard.leftArrowKey);
            Assert.AreEqual(0.9f, GameSettings.Volume, 1e-4f, "← で音量が下がらない");
            // 2 行目(画面の揺れ)へ移って、左へ
            yield return Tap(mKeyboard.downArrowKey);
            Assert.AreEqual(1, SettingsUI.Instance.Selected);
            yield return Tap(mKeyboard.leftArrowKey);
            Assert.AreEqual(0.9f, GameSettings.Shake, 1e-4f, "画面の揺れが変わらない");

            yield return Tap(mKeyboard.escapeKey);
            Assert.IsFalse(SettingsUI.IsOpen, "Esc で閉じない");
            Assert.IsFalse(GamePause.IsPaused, "閉じてもゲームが止まったまま");
        }

        [UnityTest]
        public IEnumerator M_TogglesMute_AnyTime()
        {
            yield return Load();
            yield return Tap(mKeyboard.mKey);
            Assert.IsTrue(GameSettings.Muted, "M でミュートにならない");
            yield return Tap(mKeyboard.mKey);
            Assert.IsFalse(GameSettings.Muted);
        }

        [UnityTest]
        public IEnumerator TurningEffectsOff_StopsFlashZoomAndShake()
        {
            yield return Load();
            var aFeedback = CombatFeedback.Instance;
            GameSettings.Flash = 0f;
            GameSettings.Zoom = 0f;
            aFeedback.Flash(1f);
            aFeedback.Vignette(Color.red, 1f);
            aFeedback.Punch(10f);
            yield return null;
            Assert.AreEqual(0f, aFeedback.FlashAmount, "画面の光を 0 にしても光った");
            Assert.AreEqual(0f, aFeedback.VignetteAmount);
            Assert.AreEqual(0f, aFeedback.CameraRig.Zoom, 1e-4f, "ズームを 0 にしてもズームした");

            GameSettings.Flash = 1f;
            aFeedback.Flash(1f);
            Assert.Greater(aFeedback.FlashAmount, 0.5f, "設定を戻しても光らない");
        }

        [UnityTest]
        public IEnumerator Effects_AreReused_InsteadOfCreatedEveryTime()
        {
            yield return Load();
            var aFeedback = CombatFeedback.Instance;
            aFeedback.SpawnSparks(Vector3.up * 2f, Color.white, 20);
            aFeedback.SpawnShockwave(Vector3.zero, 2f, Color.white);
            int aCreated = EffectPool.CreatedCount;
            int aReused = EffectPool.ReusedCount;

            // 火花・衝撃波が消えてから、同じ数をもう一度出す
            yield return new WaitForSeconds(0.6f);
            aFeedback.SpawnSparks(Vector3.up * 2f, Color.white, 20);
            aFeedback.SpawnShockwave(Vector3.zero, 2f, Color.white);
            Assert.AreEqual(aCreated, EffectPool.CreatedCount, "しまっておいた演出を使わず、新しく作った");
            Assert.AreEqual(aReused + 21, EffectPool.ReusedCount);
        }
    }
}
