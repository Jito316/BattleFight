using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// ひび割れた壁。決まった武器種(大槌)の技で叩くと崩れる。ほかの武器では壊れず、ヒントを出す。
    /// 壊したかどうかは保存される。
    /// </summary>
    public class BreakableWall : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("id")] string mId;
        [SerializeField, FormerlySerializedAs("breaker")] WeaponType mBreaker = WeaponType.Hammer;

        float mLastHintTime = -99f;

        public string Id => mId;

        public void Setup(string iWallId, WeaponType iBreakerWeapon)
        {
            mId = iWallId;
            mBreaker = iBreakerWeapon;
        }

        public WeaponType Breaker => mBreaker;
        public bool Broken => ExplorationSave.HasFlag(mId);

        void OnEnable()
        {
            if (Broken) gameObject.SetActive(false);
        }

        /// <summary>技が当たった。壊れたら true</summary>
        public bool TryBreak(SkillData iSkill)
        {
            if (iSkill == null || Broken) return false;
            var aRegion = ExplorationRegion.Active;
            if (iSkill.Weapon != mBreaker)
            {
                if (aRegion != null && Time.unscaledTime - mLastHintTime > 2f)
                {
                    mLastHintTime = Time.unscaledTime;
                    string aWeaponName = aRegion.Slots != null && aRegion.Slots.GetWeaponData(mBreaker) != null
                        ? aRegion.Slots.GetWeaponData(mBreaker).DisplayName
                        : mBreaker.ToString();
                    aRegion.Notify(GameText.Format("explore.wall.locked", aWeaponName), new Color(1f, 0.8f, 0.6f));
                }
                return false;
            }

            ExplorationSave.SetFlag(mId);
            if (CombatFeedback.Instance != null)
            {
                var aBounds = GetComponent<Collider>() != null ? GetComponent<Collider>().bounds : new Bounds(transform.position, Vector3.one);
                var aGround = new Vector3(aBounds.center.x, aBounds.min.y + 0.05f, aBounds.center.z);
                float aSize = Mathf.Max(aBounds.extents.x, aBounds.extents.z) + 1f;
                var aDustColor = new Color(0.9f, 0.7f, 0.5f);
                if (!CombatFeedback.Instance.PlayCue(EffectCue.WallBreak, aGround, transform.rotation, aSize, aDustColor))
                    CombatFeedback.Instance.SpawnShockwave(aGround, aSize, aDustColor);
            }
            if (aRegion != null) aRegion.Notify(GameText.Get("explore.wall.broken"), new Color(1f, 0.85f, 0.6f));
            gameObject.SetActive(false);
            return true;
        }
    }
}
