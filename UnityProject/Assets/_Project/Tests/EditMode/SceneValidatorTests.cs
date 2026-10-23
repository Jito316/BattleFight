using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleFight.Tests
{
    /// <summary>シーン: 壊れていないか・置いてある物がプレハブか(シーンを分けて作業できるように)</summary>
    public class SceneValidatorTests
    {
        const string mScenePath = "Assets/_Project/Scenes/Prototype_Arena.unity";

        [Test]
        public void Scene_HasNoProblems()
        {
            var aProblems = BattleFight.EditorTools.SceneValidator.ValidateScenePath(mScenePath);
            Assert.IsEmpty(aProblems, string.Join("\n", aProblems));
        }

        /// <summary>シーンを足して開く(テストの間だけ。テストを動かしているシーンはそのまま)</summary>
        static Scene OpenAdditive() => EditorSceneManager.OpenScene(mScenePath, OpenSceneMode.Additive);

        [Test]
        public void SceneObjects_ArePrefabInstances()
        {
            var aScene = OpenAdditive();
            try
            {
                var aRoots = aScene.GetRootGameObjects();
                foreach (var aName in new[] { "Arena", "Exploration", "Player", "Main Camera", "GameDirector", "UI" })
                {
                    var aRoot = aRoots.FirstOrDefault(iRoot => iRoot.name == aName);
                    Assert.NotNull(aRoot, $"シーンに {aName} がない");
                    Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(aRoot), $"{aName} がプレハブとして置かれていない(シーンに直接作られている)");
                }
                // プレハブでない物は、光だけ(シーンごとに決めるもの)
                var aLoose = aRoots.Where(iRoot => !PrefabUtility.IsAnyPrefabInstanceRoot(iRoot) && iRoot.GetComponent<Light>() == null)
                    .Select(iRoot => iRoot.name).ToArray();
                Assert.IsEmpty(aLoose, "プレハブでない物がシーンに直接ある: " + string.Join(", ", aLoose));
            }
            finally
            {
                EditorSceneManager.CloseScene(aScene, true);
            }
        }

        [Test]
        public void Validator_FindsADisconnectedReference()
        {
            var aScene = OpenAdditive();
            var aDirector = aScene.GetRootGameObjects().Select(iRoot => iRoot.GetComponentInChildren<ArenaDirector>(true)).First(iDirector => iDirector != null);
            var aSerialized = new SerializedObject(aDirector);
            aSerialized.FindProperty("mPlayer").objectReferenceValue = null;
            aSerialized.ApplyModifiedPropertiesWithoutUndo();
            var aProblems = BattleFight.EditorTools.SceneValidator.Validate(aScene);
            // 保存せずに閉じる(ファイルは変わらない)
            EditorSceneManager.CloseScene(aScene, true);
            Assert.IsTrue(aProblems.Any(iProblem => iProblem.Contains("ArenaDirector.mPlayer")), "切った参照を見つけられない");
        }
    }
}
