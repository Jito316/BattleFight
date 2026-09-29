using UnityEngine;

namespace BattleFight
{
    /// <summary>谷などに落ちたときに戻ってくる地点。プレイヤーが近くを通ると、最後の安全地点として覚える。</summary>
    public class SafePoint : MonoBehaviour
    {
        [SerializeField] float radius = 4f;

        void Update()
        {
            var region = ExplorationRegion.Active;
            if (region == null || region.Player == null) return;
            if ((region.Player.position - transform.position).sqrMagnitude <= radius * radius) region.TouchSafePoint(this);
        }
    }
}
