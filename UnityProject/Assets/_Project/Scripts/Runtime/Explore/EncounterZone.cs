using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleFight
{
    /// <summary>
    /// 部屋の敵。プレイヤーが範囲に入ると敵が現れ、全員倒すとその部屋は片付いたことになる(保存される)。
    /// ボスの部屋では、倒すと報酬のスキルを手に入れ、探索のクリアになる。
    /// </summary>
    public class EncounterZone : MonoBehaviour
    {
        [Serializable]
        public class Spawn
        {
            [FormerlySerializedAs("profile")] public EnemyProfile Profile;
            [Tooltip("このゾーンからの相対位置")] [FormerlySerializedAs("offset")] public Vector3 Offset;
        }

        [SerializeField, FormerlySerializedAs("id")] string mId;
        [SerializeField, Tooltip("このゾーンからの相対位置の範囲"), FormerlySerializedAs("size")] Vector3 mSize = new Vector3(20f, 6f, 20f);
        [SerializeField, FormerlySerializedAs("spawns")] List<Spawn> mSpawns = new List<Spawn>();
        [SerializeField, Tooltip("倒すと手に入るスキル(ボス)"), FormerlySerializedAs("rewards")] SkillData[] mRewards;
        [SerializeField, Tooltip("倒すと探索クリア"), FormerlySerializedAs("isFinal")] bool mIsFinal;
        [SerializeField, Tooltip("戦っている間だけ出す壁(ボス部屋の入口を閉じる)"), FormerlySerializedAs("barriers")] GameObject[] mBarriers;

        readonly List<EnemyController> mAlive = new List<EnemyController>();

        public string Id => mId;

        public void Setup(string iZoneId, Vector3 iZoneSize, List<Spawn> iZoneSpawns, SkillData[] iZoneRewards, bool iFinal, GameObject[] iZoneBarriers)
        {
            mId = iZoneId;
            mSize = iZoneSize;
            mSpawns = iZoneSpawns;
            mRewards = iZoneRewards;
            mIsFinal = iFinal;
            mBarriers = iZoneBarriers;
        }

        public bool Cleared => ExplorationSave.HasFlag(mId);
        public bool Active { get; private set; }
        public IReadOnlyList<SkillData> Rewards => mRewards ?? new SkillData[0];
        public int AliveCount => mAlive.Count;

        void Start() => SetBarriers(false);

        void Update()
        {
            var aRegion = ExplorationRegion.Active;
            if (aRegion == null || aRegion.Player == null || aRegion.Director == null || Cleared) return;

            if (!Active)
            {
                if (new Bounds(transform.position, mSize).Contains(aRegion.Player.position)) Begin(aRegion);
                return;
            }

            mAlive.RemoveAll(iE => iE == null || iE.IsDead);
            if (mAlive.Count == 0) Finish(aRegion);
        }

        void Begin(ExplorationRegion iRegion)
        {
            Active = true;
            foreach (var aSpawn in mSpawns)
            {
                if (aSpawn.Profile == null) continue;
                var aEnemy = iRegion.Director.SpawnAt(aSpawn.Profile, transform.position + aSpawn.Offset);
                if (aEnemy != null) mAlive.Add(aEnemy);
            }
            SetBarriers(true);
        }

        void Finish(ExplorationRegion iRegion)
        {
            Active = false;
            ExplorationSave.SetFlag(mId);
            SetBarriers(false);

            var aGot = new List<string>();
            foreach (var aSkill in Rewards)
            {
                if (aSkill == null || !ExplorationSave.Unlock(aSkill)) continue;
                iRegion.Slots.EquipNewSkill(aSkill);
                aGot.Add(aSkill.DisplayName);
            }
            if (aGot.Count > 0)
            {
                iRegion.Notify($"スキルを手に入れた\n<size=30>{string.Join("・", aGot)}</size>\n<size=22>[P] 編成で組み替えられる</size>", Color.white);
            }
            if (mIsFinal) iRegion.Director.CompleteExploration();
        }

        void SetBarriers(bool iOn)
        {
            if (mBarriers == null) return;
            foreach (var aBarrier in mBarriers)
            {
                if (aBarrier != null) aBarrier.SetActive(iOn);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.4f);
            Gizmos.DrawWireCube(transform.position, mSize);
        }
    }
}
