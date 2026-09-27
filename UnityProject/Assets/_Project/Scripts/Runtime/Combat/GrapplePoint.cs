using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>ワイヤーで飛びつける地点</summary>
    public class GrapplePoint : MonoBehaviour
    {
        public static readonly List<GrapplePoint> All = new List<GrapplePoint>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
