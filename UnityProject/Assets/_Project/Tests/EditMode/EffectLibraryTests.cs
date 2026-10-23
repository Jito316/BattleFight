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
        public void GameDirector_UsesTheLibrary()
        {
            // ゲーム進行のプレハブの CombatFeedback が、差し替え表をつないでいるか
            var aDirector = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Scene/GameDirector.prefab");
            Assert.NotNull(aDirector, "ゲーム進行のプレハブがない");
            var aFeedback = aDirector.GetComponent<CombatFeedback>();
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<EffectLibrary>(mLibraryPath), aFeedback.Effects, "CombatFeedback に差し替え表がつながっていない");
        }
    }
}
