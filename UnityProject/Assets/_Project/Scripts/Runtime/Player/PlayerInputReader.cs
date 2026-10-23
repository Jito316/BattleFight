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
        /// <summary>ガード(押した直後に受けるとパリィ)</summary>
        Guard,
        /// <summary>ロックオン中: 標的を左の敵へ</summary>
        LockOnLeft,
        /// <summary>ロックオン中: 標的を右の敵へ</summary>
        LockOnRight,
    }

    /// <summary>
    /// 入力の読み取りと先行入力バッファ。
    /// 操作の割り当ては .inputactions(Assets/_Project/Input/BattleFight.inputactions)から読む。
    /// 空なら PlayerInputBindings の初期の割り当てを使う。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        static readonly int mActionCount = Enum.GetValues(typeof(PlayerAction)).Length;

        [SerializeField, Tooltip("先行入力を受け付ける秒数"), FormerlySerializedAs("bufferTime")] float mBufferTime = 0.15f;
        [SerializeField, Tooltip("操作の割り当て(.inputactions)。空なら PlayerInputBindings の初期の割り当て")] InputActionAsset mActions;

        /// <summary>使っている割り当て(複製。ほかのプレイヤーや元のアセットに影響しない)</summary>
        InputActionAsset mAsset;
        InputAction mMove;
        InputAction mStickLook;
        InputAction mMouseLook;
        InputAction[] mButtons;
        float[] mPressedAt;

        public Vector2 Move => mMove.ReadValue<Vector2>();
        public Vector2 StickLook => mStickLook.ReadValue<Vector2>();
        public Vector2 MouseDelta => mMouseLook.ReadValue<Vector2>();
        /// <summary>使っている割り当て(表示やテスト用)</summary>
        public InputActionAsset Actions => mAsset;

        void Awake()
        {
            mPressedAt = new float[mActionCount];
            for (int aIndex = 0; aIndex < mActionCount; aIndex++) mPressedAt[aIndex] = float.NegativeInfinity;
            Setup(mActions);
        }

        /// <summary>割り当てを替える(テストや、設定画面で割り当てを変えたとき)。空なら初期の割り当て</summary>
        public void UseActions(InputActionAsset iActions)
        {
            Teardown();
            Setup(iActions);
            if (isActiveAndEnabled) mAsset.Enable();
        }

        void Setup(InputActionAsset iActions)
        {
            mAsset = iActions != null ? Instantiate(iActions) : PlayerInputBindings.CreateDefault();
            // 名前で探す。ない(名前を変えた)ときは、すぐわかるように止める
            var aMap = mAsset.FindActionMap(PlayerInputBindings.MapName, true);
            mMove = aMap.FindAction(PlayerInputBindings.MoveAction, true);
            mStickLook = aMap.FindAction(PlayerInputBindings.StickLookAction, true);
            mMouseLook = aMap.FindAction(PlayerInputBindings.MouseLookAction, true);
            mButtons = new InputAction[mActionCount];
            for (int aIndex = 0; aIndex < mActionCount; aIndex++)
            {
                int aActionIndex = aIndex;
                var aAction = aMap.FindAction(((PlayerAction)aIndex).ToString(), true);
                aAction.performed += _ => mPressedAt[aActionIndex] = Time.unscaledTime;
                mButtons[aIndex] = aAction;
            }
        }

        void Teardown()
        {
            if (mAsset == null) return;
            mAsset.Disable();
            Destroy(mAsset);
            mAsset = null;
        }

        void OnEnable() => mAsset.Enable();

        void OnDisable() => mAsset.Disable();

        void OnDestroy() => Teardown();

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
