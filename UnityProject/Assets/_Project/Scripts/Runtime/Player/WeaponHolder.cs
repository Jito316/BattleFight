using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 手に持つ武器の表示。普段は攻撃Aの武器を持ち、攻撃B・移動の発動中だけその武器に持ち替える。
    /// 武器の見た目は WeaponTypeData.Prefab(なければ仮モデル)。手は CharacterVisual.HandAnchor。
    /// 振りは手を回して表現する(CharacterVisual の ProceduralMotion を外すと回さない。Animator に任せる)。
    /// </summary>
    public class WeaponHolder : MonoBehaviour
    {
        const float mPopDuration = 0.12f;

        [SerializeField, Tooltip("武器を持たせる手。空なら CharacterVisual の HandAnchor"), FormerlySerializedAs("handAnchor")] Transform mHandAnchor;
        [SerializeField, FormerlySerializedAs("weaponDatabase")] WeaponTypeData[] mWeaponDatabase;
        [SerializeField, FormerlySerializedAs("slots")] SkillSlotController mSlots;
        [SerializeField, Tooltip("武器の仮モデルに使うマテリアル(色は武器種ごとに変える)"), FormerlySerializedAs("surfaceMaterial")] Material mSurfaceMaterial;

        readonly Dictionary<WeaponType, GameObject> mVisuals = new Dictionary<WeaponType, GameObject>();
        readonly Dictionary<WeaponType, TrailRenderer> mTrails = new Dictionary<WeaponType, TrailRenderer>();
        bool mTrailOn;
        CharacterVisual mVisual;
        WeaponType mMainWeapon;
        WeaponType? mTemporaryWeapon;
        WeaponType? mShownWeapon;
        float mPopTimer;

        void Start()
        {
            mVisual = CharacterVisual.Of(this);
            // 見た目(プレハブの Visual)を差し替えても武器が付くよう、手は見た目のものを優先する
            if (mVisual != null && mVisual.HandAnchor != null) mHandAnchor = mVisual.HandAnchor;
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
            WeaponVisual aWeapon;
            if (iData.Prefab != null)
            {
                var aObject = Instantiate(iData.Prefab, mHandAnchor, false);
                aObject.name = iData.DisplayName;
                aWeapon = aObject.GetComponent<WeaponVisual>();
                if (aWeapon == null) aWeapon = aObject.AddComponent<WeaponVisual>();
            }
            else
            {
                aWeapon = PrimitiveVisuals.BuildWeapon(iData, PrimitiveVisuals.RuntimeMaterials(mSurfaceMaterial));
                aWeapon.transform.SetParent(mHandAnchor, false);
            }

            // 振ったときの軌跡(武器の先から出す)。攻撃判定が出ている間だけ描く。プレハブに軌跡がなければ付ける
            var aTrail = aWeapon.Trail;
            if (aTrail == null)
            {
                float aWidth = Mathf.Clamp(Mathf.Max(iData.HeadScale.x, iData.HeadScale.y, iData.HeadScale.z) * 0.5f, 0.12f, 0.45f);
                aTrail = PrimitiveVisuals.AddTrail(aWeapon.Tip, aWidth, mSurfaceMaterial);
                aTrail.material.color = Color.Lerp(iData.Color, Color.white, 0.5f);
            }
            aTrail.emitting = false;
            mTrails[iData.Weapon] = aTrail;

            aWeapon.gameObject.SetActive(false);
            return aWeapon.gameObject;
        }

        /// <summary>武器の軌跡を描くか(攻撃判定が出ている間だけ true にする)</summary>
        public void SetTrail(bool iOn)
        {
            if (mTrailOn == iOn) return;
            mTrailOn = iOn;
            foreach (var aPair in mTrails)
            {
                bool aShown = iOn && mShownWeapon.HasValue && aPair.Key == mShownWeapon.Value;
                // 描きはじめに、前の位置から線が伸びないよう消しておく
                if (aShown && !aPair.Value.emitting) aPair.Value.Clear();
                aPair.Value.emitting = aShown;
            }
        }

        /// <summary>軌跡を描いているか(テスト用)</summary>
        public bool IsTrailOn => mTrailOn;

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
            if (!UsesProceduralMotion) return;
            mHandAnchor.localRotation = iOverhead
                ? Quaternion.Euler(Mathf.LerpUnclamped(-100f, 60f, (iNormalized + 1f) * 0.5f), 0f, 0f)
                : Quaternion.Euler(10f, iNormalized * 80f * iSide, 0f);
        }

        public void SetIdle()
        {
            if (!UsesProceduralMotion) return;
            var aIdle = Quaternion.Euler(35f, 0f, 0f);
            mHandAnchor.localRotation = Quaternion.Slerp(mHandAnchor.localRotation, aIdle, 1f - Mathf.Exp(-15f * Time.deltaTime));
        }

        /// <summary>コードで手を回すか(Animator で動かす見た目なら回さない)</summary>
        bool UsesProceduralMotion => mHandAnchor != null && (mVisual == null || mVisual.ProceduralMotion);

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
