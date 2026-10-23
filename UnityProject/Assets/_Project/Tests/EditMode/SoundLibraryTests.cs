using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>音の差し替え表: すべての音の行があるか・空の行はコードで作った音のままか・ゲーム進行につながっているか</summary>
    public class SoundLibraryTests
    {
        const string mLibraryPath = "Assets/_Project/Data/SoundLibrary.asset";

        [Test]
        public void Library_HasOneRowForEverySound()
        {
            var aLibrary = AssetDatabase.LoadAssetAtPath<SoundLibrary>(mLibraryPath);
            Assert.NotNull(aLibrary, "音の差し替え表がない");
            foreach (Sfx aSound in Enum.GetValues(typeof(Sfx)))
            {
                int aCount = 0;
                foreach (var aEntry in aLibrary.Entries)
                {
                    if (aEntry.Sound == aSound) aCount++;
                }
                Assert.AreEqual(1, aCount, $"{aSound} の行が {aCount} 個ある(1 個にする)");
            }
        }

        [Test]
        public void EmptyRow_KeepsTheSynthesizedSound_FilledRow_IsFound()
        {
            var aLibrary = ScriptableObject.CreateInstance<SoundLibrary>();
            aLibrary.EnsureEntry(Sfx.Parry);
            Assert.IsNull(aLibrary.Find(Sfx.Parry), "ファイルが空なのに差し替えになる");
            var aClip = AudioClip.Create("ArtistParry", 100, 1, 22050, false);
            aLibrary.Set(Sfx.Parry, aClip);
            Assert.AreEqual(aClip, aLibrary.Find(Sfx.Parry).Clips[0]);
            Assert.IsNull(aLibrary.Find(Sfx.Guard), "決めていない音まで差し替えになる");
            Object.DestroyImmediate(aClip);
            Object.DestroyImmediate(aLibrary);
        }

        [Test]
        public void GameDirector_UsesTheLibrary()
        {
            var aDirector = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Scene/GameDirector.prefab");
            var aSerialized = new SerializedObject(aDirector.GetComponent<CombatFeedback>());
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<SoundLibrary>(mLibraryPath), aSerialized.FindProperty("mSounds").objectReferenceValue,
                "CombatFeedback に音の差し替え表がつながっていない");
        }

        [Test]
        public void SoundNumbers_StayInPlace()
        {
            // データには番号で保存されるので、途中に足したり消したりすると、差し替えた音がずれる
            Assert.AreEqual(0, (int)Sfx.Swing);
            Assert.AreEqual(9, (int)Sfx.Defeat);
            Assert.AreEqual(16, (int)Sfx.Parry);
            Assert.AreEqual(23, (int)Sfx.SwingFist);
        }
    }
}
