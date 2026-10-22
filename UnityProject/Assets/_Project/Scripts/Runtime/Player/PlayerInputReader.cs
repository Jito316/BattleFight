using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace BattleFight
{
    public enum PlayerAction
    {
        AttackA,
        AttackB,
        Movement,
        Jump,
        LockOn,
        /// <summary>ラック方式: 攻撃Aを切り替え / プリセット方式: プリセット1</summary>
        Swap1,
        /// <summary>ラック方式: 攻撃Bを切り替え / プリセット方式: プリセット2</summary>
        Swap2,
        /// <summary>ラック方式: 移動を切り替え / プリセット方式: プリセット3</summary>
        Swap3,
        /// <summary>プリセット方式: プリセット4</summary>
        Swap4,
        Finisher,
        Restart,
        OpenLoadout,
        /// <summary>チェンジドライブ(切り替えの必殺技)</summary>
        Drive,
    }

    /// <summary>
    /// 入力の読み取りと先行入力バッファ。
    /// アクションはコードで定義する(ゲームパッド + キーボード/マウス)。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        static readonly int mActionCount = Enum.GetValues(typeof(PlayerAction)).Length;

        [SerializeField, Tooltip("先行入力を受け付ける秒数"), FormerlySerializedAs("bufferTime")] float mBufferTime = 0.15f;

        InputAction mMove;
        InputAction mStickLook;
        InputAction mMouseLook;
        InputAction[] mButtons;
        float[] mPressedAt;

        public Vector2 Move => mMove.ReadValue<Vector2>();
        public Vector2 StickLook => mStickLook.ReadValue<Vector2>();
        public Vector2 MouseDelta => mMouseLook.ReadValue<Vector2>();

        void Awake()
        {
            mMove = new InputAction("Move", InputActionType.Value);
            mMove.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            mMove.AddBinding("<Gamepad>/leftStick");

            mStickLook = new InputAction("StickLook", InputActionType.Value, "<Gamepad>/rightStick");
            mMouseLook = new InputAction("MouseLook", InputActionType.Value, "<Mouse>/delta");

            mButtons = new InputAction[mActionCount];
            mPressedAt = new float[mActionCount];
            for (int aIndex = 0; aIndex < mActionCount; aIndex++) mPressedAt[aIndex] = float.NegativeInfinity;

            Bind(PlayerAction.AttackA, "<Gamepad>/buttonWest", "<Mouse>/leftButton", "<Keyboard>/j");
            Bind(PlayerAction.AttackB, "<Gamepad>/buttonNorth", "<Mouse>/rightButton", "<Keyboard>/k");
            Bind(PlayerAction.Movement, "<Gamepad>/buttonEast", "<Keyboard>/leftShift", "<Keyboard>/l");
            Bind(PlayerAction.Jump, "<Gamepad>/buttonSouth", "<Keyboard>/space");
            Bind(PlayerAction.LockOn, "<Gamepad>/leftShoulder", "<Keyboard>/tab", "<Mouse>/middleButton");
            Bind(PlayerAction.Swap1, "<Gamepad>/dpad/left", "<Keyboard>/1");
            Bind(PlayerAction.Swap2, "<Gamepad>/dpad/right", "<Keyboard>/2");
            Bind(PlayerAction.Swap3, "<Gamepad>/dpad/down", "<Keyboard>/3");
            Bind(PlayerAction.Swap4, "<Gamepad>/dpad/up", "<Keyboard>/4");
            Bind(PlayerAction.Finisher, "<Gamepad>/rightShoulder", "<Keyboard>/f");
            Bind(PlayerAction.Restart, "<Gamepad>/start", "<Keyboard>/r");
            Bind(PlayerAction.OpenLoadout, "<Gamepad>/select", "<Keyboard>/p");
            Bind(PlayerAction.Drive, "<Gamepad>/leftTrigger", "<Keyboard>/v");
        }

        void Bind(PlayerAction iAction, params string[] iPaths)
        {
            int aIndex = (int)iAction;
            var aInputAction = new InputAction(iAction.ToString(), InputActionType.Button);
            foreach (var aPath in iPaths) aInputAction.AddBinding(aPath);
            aInputAction.performed += _ => mPressedAt[aIndex] = Time.unscaledTime;
            mButtons[aIndex] = aInputAction;
        }

        void OnEnable()
        {
            mMove.Enable();
            mStickLook.Enable();
            mMouseLook.Enable();
            foreach (var aButton in mButtons) aButton.Enable();
        }

        void OnDisable()
        {
            mMove.Disable();
            mStickLook.Disable();
            mMouseLook.Disable();
            foreach (var aButton in mButtons) aButton.Disable();
        }

        void OnDestroy()
        {
            mMove.Dispose();
            mStickLook.Dispose();
            mMouseLook.Dispose();
            foreach (var aButton in mButtons) aButton.Dispose();
        }

        /// <summary>バッファ内に押下があるか(消費しない)</summary>
        public bool Peek(PlayerAction iAction) => Time.unscaledTime - mPressedAt[(int)iAction] <= mBufferTime;

        /// <summary>バッファ内の押下を消費する</summary>
        public bool Consume(PlayerAction iAction)
        {
            if (!Peek(iAction)) return false;
            mPressedAt[(int)iAction] = float.NegativeInfinity;
            return true;
        }

        public bool IsHeld(PlayerAction iAction) => mButtons[(int)iAction].IsPressed();

        /// <summary>
        /// フォーカスが外れると、押していたキーを離したことが届かず移動しっぱなしになるので、
        /// 入力をすべて離した状態に戻す。
        /// </summary>
        void OnApplicationFocus(bool iHasFocus)
        {
            if (!iHasFocus) ReleaseAll();
        }

        /// <summary>キーボード・マウス・ゲームパッドをすべて離した状態に戻し、先行入力も捨てる</summary>
        public void ReleaseAll()
        {
            foreach (var aDevice in InputSystem.devices)
            {
                if (aDevice is Keyboard || aDevice is Mouse || aDevice is Gamepad) InputSystem.ResetDevice(aDevice);
            }
            ClearBuffer();
        }

        /// <summary>先行入力をすべて捨てる(編成画面を閉じたときのクリックなどが技として出ないように)</summary>
        public void ClearBuffer()
        {
            for (int aPressedAtIndex = 0; aPressedAtIndex < mPressedAt.Length; aPressedAtIndex++) mPressedAt[aPressedAtIndex] = float.NegativeInfinity;
        }

        public static PlayerAction SwapAction(int iIndex) => iIndex switch
        {
            0 => PlayerAction.Swap1,
            1 => PlayerAction.Swap2,
            2 => PlayerAction.Swap3,
            _ => PlayerAction.Swap4,
        };

        public static PlayerAction ActionFor(SlotType iSlot) => iSlot switch
        {
            SlotType.AttackA => PlayerAction.AttackA,
            SlotType.AttackB => PlayerAction.AttackB,
            _ => PlayerAction.Movement,
        };
    }
}
