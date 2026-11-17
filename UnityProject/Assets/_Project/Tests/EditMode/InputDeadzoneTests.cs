using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight.Tests
{
    /// <summary>移動の入力: 少しずれたままのゲームパッドのスティックでは歩かない・倒せば歩く(.inputactions と初期の割り当ての両方)</summary>
    public class InputDeadzoneTests : InputTestFixture
    {
        const string mActionsPath = "Assets/_Project/Input/BattleFight.inputactions";

        static Vector2 MoveWithStick(InputTestFixture iFixture, InputActionAsset iAsset, Gamepad iGamepad, Vector2 iStick)
        {
            var aMove = iAsset.FindActionMap(PlayerInputBindings.MapName, true).FindAction(PlayerInputBindings.MoveAction, true);
            iFixture.Set(iGamepad.leftStick, iStick);
            InputSystem.Update();
            return aMove.ReadValue<Vector2>();
        }

        void AssertIgnoresDrift(InputActionAsset iAsset, string iWhere)
        {
            var aGamepad = InputSystem.AddDevice<Gamepad>();
            iAsset.Enable();
            try
            {
                Assert.AreEqual(Vector2.zero, MoveWithStick(this, iAsset, aGamepad, new Vector2(0.17f, 0.05f)), $"{iWhere}: スティックの少しのずれで歩いてしまう");
                Assert.Greater(MoveWithStick(this, iAsset, aGamepad, new Vector2(0.8f, 0f)).x, 0.5f, $"{iWhere}: スティックを倒しても歩かない");
                Assert.AreEqual(Vector2.zero, MoveWithStick(this, iAsset, aGamepad, Vector2.zero), $"{iWhere}: スティックを戻しても止まらない");
            }
            finally
            {
                iAsset.Disable();
            }
        }

        [Test]
        public void ActionsFile_IgnoresStickDrift()
        {
            var aAsset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(mActionsPath));
            AssertIgnoresDrift(aAsset, "BattleFight.inputactions");
            Object.DestroyImmediate(aAsset);
        }

        [Test]
        public void DefaultBindings_IgnoreStickDrift()
        {
            var aAsset = PlayerInputBindings.CreateDefault();
            AssertIgnoresDrift(aAsset, "初期の割り当て");
            Object.DestroyImmediate(aAsset);
        }
    }
}
