using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BattleFight.Tests
{
    /// <summary>演出の差し替え表: すべての場面の行があるか・空の行はいつもの演出のままか</summary>
    public class EffectLibraryTests
    {
        const string mLibraryPath = "Assets/_Project/Data/EffectLibrary.asset";

        [Test]
        public void Library_HasOneRowForEveryCue()
        {
            var aLibrary = AssetDatabase.LoadAssetAtPath<EffectLibrary>(mLibraryPath);
            Assert.NotNull(aLibrary, "演出の差し替え表がない");
            foreach (EffectCue aCue in Enum.GetValues(typeof(EffectCue)))
            {
                int aCount = 0;
                foreach (var aEntry in aLibrary.Entries)
                {
                    if (aEntry.Cue == aCue) aCount++;
                }
                Assert.AreEqual(1, aCount, $"{aCue} の行が {aCount} 個ある(1 個にする)");
            }
        }

        [Test]
        public void EmptyRow_KeepsTheUsualEffect_FilledRow_IsFound()
        {
            var aLibrary = ScriptableObject.CreateInstance<EffectLibrary>();
            aLibrary.EnsureEntry(EffectCue.Parry);
            Assert.IsNull(aLibrary.Find(EffectCue.Parry), "プレハブが空なのに差し替えになる");
            var aPrefab = new GameObject("ParryEffect");
            aLibrary.Set(EffectCue.Parry, aPrefab);
            Assert.AreEqual(aPrefab, aLibrary.Find(EffectCue.Parry).Prefab);
            Assert.IsNull(aLibrary.Find(EffectCue.Guard), "決めていない場面まで差し替えになる");
            Object.DestroyImmediate(aPrefab);
            Object.DestroyImmediate(aLibrary);
        }

        [Test]
        public void SceneEffects_UseTheLibrary()
        {
            // シーンの CombatFeedback が差し替え表をつないでいるか(シーンのファイルの中を見る)
            var aLibraryGuid = AssetDatabase.AssetPathToGUID(mLibraryPath);
            var aScene = System.IO.File.ReadAllText("Assets/_Project/Scenes/Prototype_Arena.unity");
            StringAssert.Contains($"mEffects: {{fileID: 11400000, guid: {aLibraryGuid}", aScene, "シーンの CombatFeedback に差し替え表がつながっていない");
        }
    }
}
