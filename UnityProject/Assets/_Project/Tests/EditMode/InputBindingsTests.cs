using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight.Tests
{
    /// <summary>操作の割り当て: .inputactions に、ゲームが読むアクションがすべてあるか・プレイヤーにつながっているか</summary>
    public class InputBindingsTests
    {
        const string mActionsPath = "Assets/_Project/Input/BattleFight.inputactions";

        static void AssertHasEveryAction(InputActionAsset iAsset, string iWhere)
        {
            var aMap = iAsset.FindActionMap(PlayerInputBindings.MapName);
            Assert.NotNull(aMap, $"{iWhere} に {PlayerInputBindings.MapName} のマップがない");
            foreach (var aName in new[] { PlayerInputBindings.MoveAction, PlayerInputBindings.StickLookAction, PlayerInputBindings.MouseLookAction })
            {
                Assert.NotNull(aMap.FindAction(aName), $"{iWhere} に {aName} がない");
            }
            foreach (PlayerAction aAction in Enum.GetValues(typeof(PlayerAction)))
            {
                var aInputAction = aMap.FindAction(aAction.ToString());
                Assert.NotNull(aInputAction, $"{iWhere} に {aAction} がない(名前を変えると、ゲームが読めなくなる)");
                Assert.Greater(aInputAction.bindings.Count, 0, $"{iWhere} の {aAction} に割り当てがない");
            }
        }

        [Test]
        public void ActionsFile_HasEveryActionTheGameReads()
        {
            var aAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(mActionsPath);
            Assert.NotNull(aAsset, "操作の割り当て(.inputactions)がない");
            AssertHasEveryAction(aAsset, "BattleFight.inputactions");
        }

        [Test]
        public void DefaultBindings_HaveEveryAction()
        {
            var aDefault = PlayerInputBindings.CreateDefault();
            AssertHasEveryAction(aDefault, "初期の割り当て");
            UnityEngine.Object.DestroyImmediate(aDefault);
        }

        [Test]
        public void Player_UsesTheActionsFile()
        {
            var aPlayer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            var aSerialized = new SerializedObject(aPlayer.GetComponent<PlayerInputReader>());
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<InputActionAsset>(mActionsPath), aSerialized.FindProperty("mActions").objectReferenceValue,
                "プレイヤーに操作の割り当てがつながっていない");
        }
    }
}
