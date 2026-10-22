using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>谷などに落ちたときに戻ってくる地点。プレイヤーが近くを通ると、最後の安全地点として覚える。</summary>
    public class SafePoint : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("radius")] float mRadius = 4f;

        void Update()
        {
            var aRegion = ExplorationRegion.Active;
            if (aRegion == null || aRegion.Player == null) return;
            if ((aRegion.Player.position - transform.position).sqrMagnitude <= mRadius * mRadius) aRegion.TouchSafePoint(this);
        }
    }
}
