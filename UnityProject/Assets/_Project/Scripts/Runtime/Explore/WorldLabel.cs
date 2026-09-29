using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>探索マップの上に出す文字(「ひび割れた壁」「祭壇」など)。HUD が近いものだけ描く。</summary>
    public class WorldLabel : MonoBehaviour
    {
        public static readonly List<WorldLabel> All = new List<WorldLabel>();

        public string text;
        public Color color = Color.white;
        [Tooltip("この位置からの高さ")] public float height = 2f;
        [Tooltip("プレイヤーがこの距離より近いときだけ出す")] public float showDistance = 14f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        public Vector3 Position => transform.position + Vector3.up * height;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static WorldLabel Attach(GameObject target, string text, Color color, float height, float showDistance = 14f)
        {
            var label = target.GetComponent<WorldLabel>();
            if (label == null) label = target.AddComponent<WorldLabel>();
            label.text = text;
            label.color = color;
            label.height = height;
            label.showDistance = showDistance;
            return label;
        }
    }
}
