using UnityEngine;
using UnityEngine.InputSystem;

namespace BattleFight
{
    /// <summary>
    /// 三人称のオービットカメラ。ロックオン中は対象の方へ自動で回り込む。
    /// ヒットストップ中も動くように unscaledDeltaTime で動かす。
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputReader input;
        [SerializeField] LockOnSystem lockOn;

        [SerializeField] float distance = 7f;
        [SerializeField] float focusHeight = 1.6f;
        [SerializeField] float mouseSensitivity = 0.15f;
        [SerializeField] float stickSensitivity = 180f;
        [SerializeField] float minPitch = -15f;
        [SerializeField] float maxPitch = 65f;
        [SerializeField] float lockOnTurnSpeed = 8f;
        [SerializeField] float lockOnPitch = 15f;
        [SerializeField] float followSharpness = 20f;

        float yaw;
        float pitch = 18f;
        float shake;
        Vector3 focus;

        void Start()
        {
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                focus = target.position + Vector3.up * focusHeight;
            }
            SetCursorLocked(true);
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void AddShake(float amount) => shake = Mathf.Min(0.5f, shake + amount);

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.unscaledDeltaTime;

            Vector2 look = input.StickLook * (stickSensitivity * dt);
            if (Cursor.lockState == CursorLockMode.Locked) look += input.MouseDelta * mouseSensitivity;

            var lockTarget = lockOn != null ? lockOn.Target : null;
            if (lockTarget != null)
            {
                Vector3 to = lockTarget.CenterPoint - target.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                {
                    float desiredYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    yaw = Mathf.LerpAngle(yaw, desiredYaw, 1f - Mathf.Exp(-lockOnTurnSpeed * dt));
                }
                pitch = Mathf.Lerp(pitch, lockOnPitch, 1f - Mathf.Exp(-4f * dt));
            }
            else
            {
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            Vector3 desiredFocus = target.position + Vector3.up * focusHeight;
            if (lockTarget != null) desiredFocus = Vector3.Lerp(desiredFocus, lockTarget.CenterPoint, 0.25f);
            focus = Vector3.Lerp(focus, desiredFocus, 1f - Mathf.Exp(-followSharpness * dt));

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = focus - rotation * Vector3.forward * distance;
            position.y = Mathf.Max(position.y, 0.4f);
            position += Random.insideUnitSphere * shake;
            shake = Mathf.Lerp(shake, 0f, 1f - Mathf.Exp(-10f * dt));

            transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
        }
    }
}
