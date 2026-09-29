using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 探索マップに置かれたスキル。触れると手に入り、空いているプリセットに自動で入る。
    /// 取ったかどうかは保存され、次からは出てこない。
    /// </summary>
    public class SkillPickup : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] SkillData[] skills;
        [SerializeField, Tooltip("手に入れたときに出す一言(何ができるようになるか)")] string hint;
        [SerializeField] float radius = 1.6f;
        [SerializeField] Transform orb;

        float baseHeight;

        public string Id => id;

        public void Setup(string pickupId, SkillData[] pickupSkills, string pickupHint, Transform orbTransform)
        {
            id = pickupId;
            skills = pickupSkills;
            hint = pickupHint;
            orb = orbTransform;
        }

        public IReadOnlyList<SkillData> Skills => skills ?? new SkillData[0];
        public bool Collected => ExplorationSave.HasFlag(id);

        void OnEnable()
        {
            if (Collected)
            {
                gameObject.SetActive(false);
                return;
            }
            if (orb != null) baseHeight = orb.localPosition.y;
        }

        void Start()
        {
            // 武器種の色で光らせる(最初のスキルの武器種)
            var region = ExplorationRegion.Active;
            if (orb == null || region == null || region.Slots == null || skills == null || skills.Length == 0 || skills[0] == null) return;
            var data = region.Slots.GetWeaponData(skills[0].weapon);
            var renderer = orb.GetComponent<Renderer>();
            if (data != null && renderer != null)
            {
                if (region.SurfaceMaterial != null) renderer.sharedMaterial = region.SurfaceMaterial;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", data.color);
                block.SetColor("_Color", data.color);
                renderer.SetPropertyBlock(block);
            }
        }

        void Update()
        {
            if (orb != null)
            {
                orb.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
                var p = orb.localPosition;
                p.y = baseHeight + Mathf.Sin(Time.time * 2.5f) * 0.15f;
                orb.localPosition = p;
            }

            var region = ExplorationRegion.Active;
            if (region == null || region.Player == null) return;
            if ((region.Player.position + Vector3.up - transform.position).sqrMagnitude <= radius * radius) Collect(region);
        }

        public void Collect(ExplorationRegion region)
        {
            if (Collected) return;
            ExplorationSave.SetFlag(id);

            var names = new StringBuilder();
            foreach (var skill in Skills)
            {
                if (skill == null) continue;
                ExplorationSave.Unlock(skill);
                int preset = region.Slots != null ? region.Slots.EquipNewSkill(skill) : -1;
                var data = region.Slots != null ? region.Slots.GetWeaponData(skill.weapon) : null;
                if (names.Length > 0) names.Append('\n');
                names.Append($"<color=#{ColorUtility.ToHtmlStringRGB(data != null ? data.color : Color.white)}>{skill.displayName}</color>");
                names.Append($"<size=24>  {(data != null ? data.displayName : "")}・{SlotName(skill.slot)}");
                if (preset >= 0) names.Append($"  → プリセット{preset + 1}に入れた");
                names.Append("</size>");
            }
            string hintLine = string.IsNullOrEmpty(hint) ? "" : $"\n<size=26><color=#FFE08A>{hint}</color></size>";
            region.Notify($"スキルを手に入れた\n{names}{hintLine}\n<size=22>[P] 編成で組み替えられる</size>", Color.white);
            if (CombatFeedback.Instance != null) CombatFeedback.Instance.SpawnShockwave(transform.position, 2.5f, new Color(1f, 0.9f, 0.5f));
            gameObject.SetActive(false);
        }

        static string SlotName(SlotType slot) => slot switch
        {
            SlotType.AttackA => "攻撃A",
            SlotType.AttackB => "攻撃B",
            _ => "移動",
        };
    }
}
