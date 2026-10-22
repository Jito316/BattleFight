using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>探索マップの上に出す文字(「ひび割れた壁」「祭壇」など)。HUD が近いものだけ描く。</summary>
    public class WorldLabel : MonoBehaviour
    {
        public static readonly List<WorldLabel> All = new List<WorldLabel>();

        [FormerlySerializedAs("text")] public string Text;
        [FormerlySerializedAs("color")] public Color Color = Color.white;
        [Tooltip("この位置からの高さ")] [FormerlySerializedAs("height")] public float Height = 2f;
        [Tooltip("プレイヤーがこの距離より近いときだけ出す")] [FormerlySerializedAs("showDistance")] public float ShowDistance = 14f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        public Vector3 Position => transform.position + Vector3.up * Height;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static WorldLabel Attach(GameObject iTarget, string iText, Color iColor, float iHeight, float iShowDistance = 14f)
        {
            var aLabel = iTarget.GetComponent<WorldLabel>();
            if (aLabel == null) aLabel = iTarget.AddComponent<WorldLabel>();
            aLabel.Text = iText;
            aLabel.Color = iColor;
            aLabel.Height = iHeight;
            aLabel.ShowDistance = iShowDistance;
            return aLabel;
        }
    }
}
