using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight
{
    /// <summary>
    /// 操作の割り当ての初期値。エディタはこれから Assets/_Project/Input/BattleFight.inputactions を最初に作り、
    /// そのあとは .inputactions のほうを直す(プランナーが Input System のウィンドウで直せる)。
    /// .inputactions がないとき(テストで組み立てたプレイヤーなど)も、これを使う。
    /// アクションの名前は PlayerAction の名前と、移動・視点の 3 つ(下の定数)。名前を変えると読めなくなる。
    /// </summary>
    public static class PlayerInputBindings
    {
        public const string MapName = "Player";
        public const string MoveAction = "Move";
        public const string StickLookAction = "StickLook";
        public const string MouseLookAction = "MouseLook";
        public const string KeyboardMouseScheme = "Keyboard&Mouse";
        public const string GamepadScheme = "Gamepad";

        /// <summary>初期の割り当て(ゲームパッド + キーボード・マウス)</summary>
        public static InputActionAsset CreateDefault()
        {
            var aAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            aAsset.name = "BattleFight";
            aAsset.AddControlScheme(KeyboardMouseScheme).WithRequiredDevice("<Keyboard>").WithOptionalDevice("<Mouse>");
            aAsset.AddControlScheme(GamepadScheme).WithRequiredDevice("<Gamepad>");
            var aMap = aAsset.AddActionMap(MapName);

            var aMove = aMap.AddAction(MoveAction, InputActionType.Value);
            aMove.expectedControlType = "Vector2";
            aMove.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KeyboardMouseScheme)
                .With("Down", "<Keyboard>/s", KeyboardMouseScheme)
                .With("Left", "<Keyboard>/a", KeyboardMouseScheme)
                .With("Right", "<Keyboard>/d", KeyboardMouseScheme);
            aMove.AddBinding("<Gamepad>/leftStick", groups: GamepadScheme);

            var aStickLook = aMap.AddAction(StickLookAction, InputActionType.Value);
            aStickLook.expectedControlType = "Vector2";
            aStickLook.AddBinding("<Gamepad>/rightStick", groups: GamepadScheme);
            var aMouseLook = aMap.AddAction(MouseLookAction, InputActionType.Value);
            aMouseLook.expectedControlType = "Vector2";
            aMouseLook.AddBinding("<Mouse>/delta", groups: KeyboardMouseScheme);

            Button(aMap, PlayerAction.AttackA, "<Gamepad>/buttonWest", "<Mouse>/leftButton", "<Keyboard>/j");
            Button(aMap, PlayerAction.AttackB, "<Gamepad>/buttonNorth", "<Mouse>/rightButton", "<Keyboard>/e", "<Keyboard>/k");
            Button(aMap, PlayerAction.Movement, "<Gamepad>/buttonEast", "<Keyboard>/leftShift", "<Keyboard>/l");
            Button(aMap, PlayerAction.Jump, "<Gamepad>/buttonSouth", "<Keyboard>/space");
            Button(aMap, PlayerAction.LockOn, "<Gamepad>/leftShoulder", "<Keyboard>/tab", "<Mouse>/middleButton");
            Button(aMap, PlayerAction.Swap1, "<Gamepad>/dpad/left", "<Keyboard>/1");
            Button(aMap, PlayerAction.Swap2, "<Gamepad>/dpad/right", "<Keyboard>/2");
            Button(aMap, PlayerAction.Swap3, "<Gamepad>/dpad/down", "<Keyboard>/3");
            Button(aMap, PlayerAction.Swap4, "<Gamepad>/dpad/up", "<Keyboard>/4");
            Button(aMap, PlayerAction.Finisher, "<Gamepad>/rightShoulder", "<Keyboard>/f");
            Button(aMap, PlayerAction.Restart, "<Gamepad>/start", "<Keyboard>/r");
            Button(aMap, PlayerAction.OpenLoadout, "<Gamepad>/select", "<Keyboard>/p");
            Button(aMap, PlayerAction.Drive, "<Gamepad>/leftTrigger", "<Keyboard>/v");
            Button(aMap, PlayerAction.Guard, "<Gamepad>/rightTrigger", "<Keyboard>/q");
            Button(aMap, PlayerAction.LockOnLeft, "<Keyboard>/z");
            Button(aMap, PlayerAction.LockOnRight, "<Keyboard>/c");
            return aAsset;
        }

        static void Button(InputActionMap iMap, PlayerAction iAction, params string[] iPaths)
        {
            var aAction = iMap.AddAction(iAction.ToString(), InputActionType.Button);
            foreach (var aPath in iPaths)
            {
                aAction.AddBinding(aPath, groups: aPath.StartsWith("<Gamepad>") ? GamepadScheme : KeyboardMouseScheme);
            }
        }
    }
}
