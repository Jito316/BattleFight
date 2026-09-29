using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 祭壇。触れると体力が回復し、倒れたときにここから再開する。
    /// 最後に触れた祭壇は保存され、次に探索を始めたときもここから始まる。
    /// </summary>
    [RequireComponent(typeof(SafePoint))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] float radius = 2.5f;
        [SerializeField, Tooltip("光る部分(触れたら色を変える)")] Renderer crystal;

        bool touching;

        public string Id => id;

        public void Setup(string checkpointId, Renderer crystalRenderer)
        {
            id = checkpointId;
            crystal = crystalRenderer;
        }

        public Vector3 RespawnPosition => transform.position + transform.forward * 2f;

        void Update()
        {
            var region = ExplorationRegion.Active;
            if (region == null || region.Player == null) return;

            bool near = (region.Player.position - transform.position).sqrMagnitude <= radius * radius;
            if (near && !touching)
            {
                region.RestorePlayer();
                if (ExplorationSave.Checkpoint != id)
                {
                    ExplorationSave.Checkpoint = id;
                    region.Notify("祭壇に触れた\n<size=26>体力が回復した。倒れたらここから再開する</size>", new Color(0.6f, 0.9f, 1f));
                }
            }
            touching = near;

            if (crystal != null)
            {
                bool current = ExplorationSave.Checkpoint == id;
                crystal.transform.Rotate(0f, (current ? 90f : 30f) * Time.deltaTime, 0f, Space.World);
            }
        }
    }
}
