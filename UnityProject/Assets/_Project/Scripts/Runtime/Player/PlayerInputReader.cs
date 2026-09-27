using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight
{
    public enum PlayerAction
    {
        AttackA,
        AttackB,
        Movement,
        Jump,
        LockOn,
        SwapAttackA,
        SwapAttackB,
        SwapMovement,
        Finisher,
        Restart,
    }

    /// <summary>
    /// 入力の読み取りと先行入力バッファ。
    /// アクションはコードで定義する(ゲームパッド + キーボード/マウス)。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        static readonly int ActionCount = Enum.GetValues(typeof(PlayerAction)).Length;

        [SerializeField, Tooltip("先行入力を受け付ける秒数")] float bufferTime = 0.15f;

        InputAction move;
        InputAction stickLook;
        InputAction mouseLook;
        InputAction[] buttons;
        float[] pressedAt;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 StickLook => stickLook.ReadValue<Vector2>();
        public Vector2 MouseDelta => mouseLook.ReadValue<Vector2>();

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            stickLook = new InputAction("StickLook", InputActionType.Value, "<Gamepad>/rightStick");
            mouseLook = new InputAction("MouseLook", InputActionType.Value, "<Mouse>/delta");

            buttons = new InputAction[ActionCount];
            pressedAt = new float[ActionCount];
            for (int i = 0; i < ActionCount; i++) pressedAt[i] = float.NegativeInfinity;

            Bind(PlayerAction.AttackA, "<Gamepad>/buttonWest", "<Mouse>/leftButton", "<Keyboard>/j");
            Bind(PlayerAction.AttackB, "<Gamepad>/buttonNorth", "<Mouse>/rightButton", "<Keyboard>/k");
            Bind(PlayerAction.Movement, "<Gamepad>/buttonEast", "<Keyboard>/leftShift", "<Keyboard>/l");
            Bind(PlayerAction.Jump, "<Gamepad>/buttonSouth", "<Keyboard>/space");
            Bind(PlayerAction.LockOn, "<Gamepad>/leftShoulder", "<Keyboard>/tab", "<Mouse>/middleButton");
            Bind(PlayerAction.SwapAttackA, "<Gamepad>/dpad/left", "<Keyboard>/1");
            Bind(PlayerAction.SwapAttackB, "<Gamepad>/dpad/right", "<Keyboard>/2");
            Bind(PlayerAction.SwapMovement, "<Gamepad>/dpad/down", "<Keyboard>/3");
            Bind(PlayerAction.Finisher, "<Gamepad>/rightShoulder", "<Keyboard>/f");
            Bind(PlayerAction.Restart, "<Gamepad>/start", "<Keyboard>/r");
        }

        void Bind(PlayerAction action, params string[] paths)
        {
            int index = (int)action;
            var inputAction = new InputAction(action.ToString(), InputActionType.Button);
            foreach (var path in paths) inputAction.AddBinding(path);
            inputAction.performed += _ => pressedAt[index] = Time.unscaledTime;
            buttons[index] = inputAction;
        }

        void OnEnable()
        {
            move.Enable();
            stickLook.Enable();
            mouseLook.Enable();
            foreach (var button in buttons) button.Enable();
        }

        void OnDisable()
        {
            move.Disable();
            stickLook.Disable();
            mouseLook.Disable();
            foreach (var button in buttons) button.Disable();
        }

        void OnDestroy()
        {
            move.Dispose();
            stickLook.Dispose();
            mouseLook.Dispose();
            foreach (var button in buttons) button.Dispose();
        }

        /// <summary>バッファ内に押下があるか(消費しない)</summary>
        public bool Peek(PlayerAction action) => Time.unscaledTime - pressedAt[(int)action] <= bufferTime;

        /// <summary>バッファ内の押下を消費する</summary>
        public bool Consume(PlayerAction action)
        {
            if (!Peek(action)) return false;
            pressedAt[(int)action] = float.NegativeInfinity;
            return true;
        }

        public bool IsHeld(PlayerAction action) => buttons[(int)action].IsPressed();

        public static PlayerAction ActionFor(SlotType slot) => slot switch
        {
            SlotType.AttackA => PlayerAction.AttackA,
            SlotType.AttackB => PlayerAction.AttackB,
            _ => PlayerAction.Movement,
        };
    }
}
