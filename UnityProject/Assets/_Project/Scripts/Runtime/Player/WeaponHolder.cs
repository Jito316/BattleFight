using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 手に持つ武器の表示。普段は攻撃Aの武器を持ち、攻撃B・移動の発動中だけその武器に持ち替える。
    /// 仮モデルなので、振りは手元のアンカーを回して表現する。
    /// </summary>
    public class WeaponHolder : MonoBehaviour
    {
        const float PopDuration = 0.12f;

        [SerializeField] Transform handAnchor;
        [SerializeField] WeaponTypeData[] weaponDatabase;
        [SerializeField] SkillSlotController slots;
        [SerializeField, Tooltip("武器の仮モデルに使うマテリアル(色は武器種ごとに変える)")] Material surfaceMaterial;

        readonly Dictionary<WeaponType, GameObject> visuals = new Dictionary<WeaponType, GameObject>();
        WeaponType mainWeapon;
        WeaponType? temporaryWeapon;
        WeaponType? shownWeapon;
        float popTimer;

        void Start()
        {
            if (weaponDatabase != null)
            {
                foreach (var data in weaponDatabase)
                {
                    if (data != null && !visuals.ContainsKey(data.weapon)) visuals[data.weapon] = BuildVisual(data);
                }
            }
            var current = slots != null ? slots.GetCurrent(SlotType.AttackA) : null;
            SetMainWeapon(current != null ? current.weapon : WeaponType.Sword);
        }

        GameObject BuildVisual(WeaponTypeData data)
        {
            var root = new GameObject(data.displayName);
            root.transform.SetParent(handAnchor, false);

            if (data.handleLength > 0f)
            {
                var handle = CreatePart(PrimitiveType.Cube, root.transform, new Color(0.25f, 0.2f, 0.15f));
                handle.localScale = new Vector3(0.05f, 0.05f, data.handleLength);
                handle.localPosition = new Vector3(0f, 0f, data.handleLength * 0.5f);
            }

            var head = CreatePart(data.headShape, root.transform, data.color);
            head.localPosition = data.headOffset;
            head.localRotation = Quaternion.Euler(data.headRotation);
            head.localScale = data.headScale;

            root.SetActive(false);
            return root;
        }

        Transform CreatePart(PrimitiveType shape, Transform parent, Color color)
        {
            var part = GameObject.CreatePrimitive(shape);
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            var renderer = part.GetComponent<Renderer>();
            if (surfaceMaterial != null) renderer.sharedMaterial = surfaceMaterial;
            renderer.material.color = color;
            return part.transform;
        }

        public void SetMainWeapon(WeaponType weapon)
        {
            mainWeapon = weapon;
            Refresh();
        }

        public void ShowTemporary(WeaponType weapon)
        {
            temporaryWeapon = weapon;
            Refresh();
        }

        public void ClearTemporary()
        {
            temporaryWeapon = null;
            Refresh();
        }

        void Refresh()
        {
            var weapon = temporaryWeapon ?? mainWeapon;
            if (shownWeapon == weapon) return;
            shownWeapon = weapon;
            foreach (var pair in visuals) pair.Value.SetActive(pair.Key == weapon);
            popTimer = PopDuration;
        }

        /// <summary>振りの姿勢。normalized は -1(振りかぶり)〜 1(振り抜き)。</summary>
        public void SetSwing(float normalized, bool overhead, float side)
        {
            handAnchor.localRotation = overhead
                ? Quaternion.Euler(Mathf.Lerp(-100f, 60f, (normalized + 1f) * 0.5f), 0f, 0f)
                : Quaternion.Euler(10f, normalized * 80f * side, 0f);
        }

        public void SetIdle()
        {
            var idle = Quaternion.Euler(35f, 0f, 0f);
            handAnchor.localRotation = Quaternion.Slerp(handAnchor.localRotation, idle, 1f - Mathf.Exp(-15f * Time.deltaTime));
        }

        void LateUpdate()
        {
            if (popTimer <= 0f || !shownWeapon.HasValue) return;
            popTimer -= Time.unscaledDeltaTime;
            if (visuals.TryGetValue(shownWeapon.Value, out var visual))
            {
                // 持ち替えた瞬間に少し大きくして目立たせる
                visual.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, popTimer / PopDuration) * 0.6f);
            }
        }
    }
}
