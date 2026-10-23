using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 探索マップに置かれたスキル。触れると手に入り、空いているプリセットに自動で入る。
    /// 取ったかどうかは保存され、次からは出てこない。
    /// </summary>
    public class SkillPickup : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("id")] string mId;
        [SerializeField, FormerlySerializedAs("skills")] SkillData[] mSkills;
        [SerializeField, Tooltip("手に入れたときに出す一言(何ができるようになるか)"), FormerlySerializedAs("hint")] string mHint;
        [SerializeField, FormerlySerializedAs("radius")] float mRadius = 1.6f;
        [SerializeField, FormerlySerializedAs("orb")] Transform mOrb;

        float mBaseHeight;

        public string Id => mId;

        public void Setup(string iPickupId, SkillData[] iPickupSkills, string iPickupHint, Transform iOrbTransform)
        {
            mId = iPickupId;
            mSkills = iPickupSkills;
            mHint = iPickupHint;
            mOrb = iOrbTransform;
        }

        public IReadOnlyList<SkillData> Skills => mSkills ?? new SkillData[0];
        public bool Collected => ExplorationSave.HasFlag(mId);

        void OnEnable()
        {
            if (Collected)
            {
                gameObject.SetActive(false);
                return;
            }
            if (mOrb != null) mBaseHeight = mOrb.localPosition.y;
        }

        void Start()
        {
            // 武器種の色で光らせる(最初のスキルの武器種)
            var aRegion = ExplorationRegion.Active;
            if (mOrb == null || aRegion == null || aRegion.Slots == null || mSkills == null || mSkills.Length == 0 || mSkills[0] == null) return;
            var aData = aRegion.Slots.GetWeaponData(mSkills[0].Weapon);
            var aRenderer = mOrb.GetComponent<Renderer>();
            if (aData != null && aRenderer != null)
            {
                if (aRegion.SurfaceMaterial != null) aRenderer.sharedMaterial = aRegion.SurfaceMaterial;
                var aBlock = new MaterialPropertyBlock();
                aBlock.SetColor("_BaseColor", aData.Color);
                aBlock.SetColor("_Color", aData.Color);
                aRenderer.SetPropertyBlock(aBlock);
            }
        }

        void Update()
        {
            if (mOrb != null)
            {
                mOrb.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
                var aPoint = mOrb.localPosition;
                aPoint.y = mBaseHeight + Mathf.Sin(Time.time * 2.5f) * 0.15f;
                mOrb.localPosition = aPoint;
            }

            var aRegion = ExplorationRegion.Active;
            if (aRegion == null || aRegion.Player == null) return;
            if ((aRegion.Player.position + Vector3.up - transform.position).sqrMagnitude <= mRadius * mRadius) Collect(aRegion);
        }

        public void Collect(ExplorationRegion iRegion)
        {
            if (Collected) return;
            ExplorationSave.SetFlag(mId);

            var aNames = new StringBuilder();
            foreach (var aSkill in Skills)
            {
                if (aSkill == null) continue;
                ExplorationSave.Unlock(aSkill);
                int aPreset = iRegion.Slots != null ? iRegion.Slots.EquipNewSkill(aSkill) : -1;
                var aData = iRegion.Slots != null ? iRegion.Slots.GetWeaponData(aSkill.Weapon) : null;
                if (aNames.Length > 0) aNames.Append('\n');
                aNames.Append($"<color=#{ColorUtility.ToHtmlStringRGB(aData != null ? aData.Color : Color.white)}>{aSkill.DisplayName}</color>");
                aNames.Append(GameText.Format("explore.pickup.line", aData != null ? aData.DisplayName : "", SlotName(aSkill.Slot)));
                if (aPreset >= 0) aNames.Append(GameText.Format("explore.pickup.preset", aPreset + 1));
                aNames.Append("</size>");
            }
            string aHintLine = string.IsNullOrEmpty(mHint) ? "" : $"\n<size=26><color=#FFE08A>{mHint}</color></size>";
            iRegion.Notify(GameText.Format("explore.pickup", aNames, aHintLine), Color.white);
            var aGlowColor = new Color(1f, 0.9f, 0.5f);
            if (CombatFeedback.Instance != null && !CombatFeedback.Instance.PlayCue(EffectCue.SkillPickup, transform.position, 2.5f, aGlowColor))
                CombatFeedback.Instance.SpawnShockwave(transform.position, 2.5f, aGlowColor);
            gameObject.SetActive(false);
        }

        static string SlotName(SlotType iSlot) => iSlot switch
        {
            SlotType.AttackA => GameText.Get("common.slot.attack_a"),
            SlotType.AttackB => GameText.Get("common.slot.attack_b"),
            _ => GameText.Get("common.slot.movement"),
        };
    }
}
