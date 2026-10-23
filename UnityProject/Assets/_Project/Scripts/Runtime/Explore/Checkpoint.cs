using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 祭壇。触れると体力が回復し、倒れたときにここから再開する。
    /// 最後に触れた祭壇は保存され、次に探索を始めたときもここから始まる。
    /// </summary>
    [RequireComponent(typeof(SafePoint))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("id")] string mId;
        [SerializeField, FormerlySerializedAs("radius")] float mRadius = 2.5f;
        [SerializeField, Tooltip("光る部分(触れたら色を変える)"), FormerlySerializedAs("crystal")] Renderer mCrystal;

        bool mTouching;

        public string Id => mId;

        public void Setup(string iCheckpointId, Renderer iCrystalRenderer)
        {
            mId = iCheckpointId;
            mCrystal = iCrystalRenderer;
        }

        public Vector3 RespawnPosition => transform.position + transform.forward * 2f;

        void Update()
        {
            var aRegion = ExplorationRegion.Active;
            if (aRegion == null || aRegion.Player == null) return;

            bool aNear = (aRegion.Player.position - transform.position).sqrMagnitude <= mRadius * mRadius;
            if (aNear && !mTouching)
            {
                aRegion.RestorePlayer();
                if (ExplorationSave.Checkpoint != mId)
                {
                    ExplorationSave.Checkpoint = mId;
                    aRegion.Notify(GameText.Get("explore.altar"), new Color(0.6f, 0.9f, 1f));
                }
            }
            mTouching = aNear;

            if (mCrystal != null)
            {
                bool aCurrent = ExplorationSave.Checkpoint == mId;
                mCrystal.transform.Rotate(0f, (aCurrent ? 90f : 30f) * Time.deltaTime, 0f, Space.World);
            }
        }
    }
}
