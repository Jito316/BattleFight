using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 手に持つ武器の表示。普段は攻撃Aの武器を持ち、攻撃B・移動の発動中だけその武器に持ち替える。
    /// 仮モデルなので、振りは手元のアンカーを回して表現する。
    /// </summary>
    public class WeaponHolder : MonoBehaviour
    {
        const float mPopDuration = 0.12f;

        [SerializeField, FormerlySerializedAs("handAnchor")] Transform mHandAnchor;
        [SerializeField, FormerlySerializedAs("weaponDatabase")] WeaponTypeData[] mWeaponDatabase;
        [SerializeField, FormerlySerializedAs("slots")] SkillSlotController mSlots;
        [SerializeField, Tooltip("武器の仮モデルに使うマテリアル(色は武器種ごとに変える)"), FormerlySerializedAs("surfaceMaterial")] Material mSurfaceMaterial;

        readonly Dictionary<WeaponType, GameObject> mVisuals = new Dictionary<WeaponType, GameObject>();
        WeaponType mMainWeapon;
        WeaponType? mTemporaryWeapon;
        WeaponType? mShownWeapon;
        float mPopTimer;

        void Start()
        {
            if (mWeaponDatabase != null)
            {
                foreach (var aData in mWeaponDatabase)
                {
                    if (aData != null && !mVisuals.ContainsKey(aData.Weapon)) mVisuals[aData.Weapon] = BuildVisual(aData);
                }
            }
            var aCurrent = mSlots != null ? mSlots.GetCurrent(SlotType.AttackA) : null;
            SetMainWeapon(aCurrent != null ? aCurrent.Weapon : WeaponType.Sword);
            // 編成画面や方式の切り替えで攻撃Aが変わったときも持ち替える
            if (mSlots != null) mSlots.SlotChanged += OnSlotChanged;
        }

        void OnDestroy()
        {
            if (mSlots != null) mSlots.SlotChanged -= OnSlotChanged;
        }

        void OnSlotChanged(SlotType iSlot)
        {
            if (iSlot != SlotType.AttackA) return;
            var aCurrent = mSlots.GetCurrent(SlotType.AttackA);
            if (aCurrent != null) SetMainWeapon(aCurrent.Weapon);
        }

        GameObject BuildVisual(WeaponTypeData iData)
        {
            var aRoot = new GameObject(iData.DisplayName);
            aRoot.transform.SetParent(mHandAnchor, false);

            if (iData.HandleLength > 0f)
            {
                var aHandle = CreatePart(PrimitiveType.Cube, aRoot.transform, new Color(0.25f, 0.2f, 0.15f));
                aHandle.localScale = new Vector3(0.05f, 0.05f, iData.HandleLength);
                aHandle.localPosition = new Vector3(0f, 0f, iData.HandleLength * 0.5f);
            }

            var aHead = CreatePart(iData.HeadShape, aRoot.transform, iData.Color);
            aHead.localPosition = iData.HeadOffset;
            aHead.localRotation = Quaternion.Euler(iData.HeadRotation);
            aHead.localScale = iData.HeadScale;

            aRoot.SetActive(false);
            return aRoot;
        }

        Transform CreatePart(PrimitiveType iShape, Transform iParent, Color iColor)
        {
            var aPart = GameObject.CreatePrimitive(iShape);
            Destroy(aPart.GetComponent<Collider>());
            aPart.transform.SetParent(iParent, false);
            var aRenderer = aPart.GetComponent<Renderer>();
            if (mSurfaceMaterial != null) aRenderer.sharedMaterial = mSurfaceMaterial;
            aRenderer.material.color = iColor;
            return aPart.transform;
        }

        public void SetMainWeapon(WeaponType iWeapon)
        {
            mMainWeapon = iWeapon;
            Refresh();
        }

        public void ShowTemporary(WeaponType iWeapon)
        {
            mTemporaryWeapon = iWeapon;
            Refresh();
        }

        public void ClearTemporary()
        {
            mTemporaryWeapon = null;
            Refresh();
        }

        void Refresh()
        {
            var aWeapon = mTemporaryWeapon ?? mMainWeapon;
            if (mShownWeapon == aWeapon) return;
            mShownWeapon = aWeapon;
            foreach (var aPair in mVisuals) aPair.Value.SetActive(aPair.Key == aWeapon);
            mPopTimer = mPopDuration;
        }

        /// <summary>振りの姿勢。normalized は -1(振りかぶり)〜 1(振り抜き)。</summary>
        public void SetSwing(float iNormalized, bool iOverhead, float iSide)
        {
            mHandAnchor.localRotation = iOverhead
                ? Quaternion.Euler(Mathf.Lerp(-100f, 60f, (iNormalized + 1f) * 0.5f), 0f, 0f)
                : Quaternion.Euler(10f, iNormalized * 80f * iSide, 0f);
        }

        public void SetIdle()
        {
            var aIdle = Quaternion.Euler(35f, 0f, 0f);
            mHandAnchor.localRotation = Quaternion.Slerp(mHandAnchor.localRotation, aIdle, 1f - Mathf.Exp(-15f * Time.deltaTime));
        }

        void LateUpdate()
        {
            if (mPopTimer <= 0f || !mShownWeapon.HasValue) return;
            mPopTimer -= Time.unscaledDeltaTime;
            if (mVisuals.TryGetValue(mShownWeapon.Value, out var aVisual))
            {
                // 持ち替えた瞬間に少し大きくして目立たせる
                aVisual.transform.localScale = Vector3.one * (1f + Mathf.Max(0f, mPopTimer / mPopDuration) * 0.6f);
            }
        }
    }
}
