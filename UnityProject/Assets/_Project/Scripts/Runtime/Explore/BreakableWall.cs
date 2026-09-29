using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// ひび割れた壁。決まった武器種(大槌)の技で叩くと崩れる。ほかの武器では壊れず、ヒントを出す。
    /// 壊したかどうかは保存される。
    /// </summary>
    public class BreakableWall : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] WeaponType breaker = WeaponType.Hammer;

        float lastHintTime = -99f;

        public string Id => id;

        public void Setup(string wallId, WeaponType breakerWeapon)
        {
            id = wallId;
            breaker = breakerWeapon;
        }

        public WeaponType Breaker => breaker;
        public bool Broken => ExplorationSave.HasFlag(id);

        void OnEnable()
        {
            if (Broken) gameObject.SetActive(false);
        }

        /// <summary>技が当たった。壊れたら true</summary>
        public bool TryBreak(SkillData skill)
        {
            if (skill == null || Broken) return false;
            var region = ExplorationRegion.Active;
            if (skill.weapon != breaker)
            {
                if (region != null && Time.unscaledTime - lastHintTime > 2f)
                {
                    lastHintTime = Time.unscaledTime;
                    string weaponName = region.Slots != null && region.Slots.GetWeaponData(breaker) != null
                        ? region.Slots.GetWeaponData(breaker).displayName
                        : breaker.ToString();
                    region.Notify($"びくともしない…\n<size=26>{weaponName}の技なら壊せそうだ</size>", new Color(1f, 0.8f, 0.6f));
                }
                return false;
            }

            ExplorationSave.SetFlag(id);
            if (CombatFeedback.Instance != null)
            {
                var bounds = GetComponent<Collider>() != null ? GetComponent<Collider>().bounds : new Bounds(transform.position, Vector3.one);
                CombatFeedback.Instance.SpawnShockwave(new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z),
                    Mathf.Max(bounds.extents.x, bounds.extents.z) + 1f, new Color(0.9f, 0.7f, 0.5f));
            }
            if (region != null) region.Notify("壁が崩れた", new Color(1f, 0.85f, 0.6f));
            gameObject.SetActive(false);
            return true;
        }
    }
}
