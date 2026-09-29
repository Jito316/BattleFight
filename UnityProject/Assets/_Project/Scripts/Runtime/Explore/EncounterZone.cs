using System;
using System.Collections.Generic;
using UnityEngine;

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
            public EnemyProfile profile;
            [Tooltip("このゾーンからの相対位置")] public Vector3 offset;
        }

        [SerializeField] string id;
        [SerializeField, Tooltip("このゾーンからの相対位置の範囲")] Vector3 size = new Vector3(20f, 6f, 20f);
        [SerializeField] List<Spawn> spawns = new List<Spawn>();
        [SerializeField, Tooltip("倒すと手に入るスキル(ボス)")] SkillData[] rewards;
        [SerializeField, Tooltip("倒すと探索クリア")] bool isFinal;
        [SerializeField, Tooltip("戦っている間だけ出す壁(ボス部屋の入口を閉じる)")] GameObject[] barriers;

        readonly List<EnemyController> alive = new List<EnemyController>();

        public string Id => id;

        public void Setup(string zoneId, Vector3 zoneSize, List<Spawn> zoneSpawns, SkillData[] zoneRewards, bool final, GameObject[] zoneBarriers)
        {
            id = zoneId;
            size = zoneSize;
            spawns = zoneSpawns;
            rewards = zoneRewards;
            isFinal = final;
            barriers = zoneBarriers;
        }

        public bool Cleared => ExplorationSave.HasFlag(id);
        public bool Active { get; private set; }
        public IReadOnlyList<SkillData> Rewards => rewards ?? new SkillData[0];
        public int AliveCount => alive.Count;

        void Start() => SetBarriers(false);

        void Update()
        {
            var region = ExplorationRegion.Active;
            if (region == null || region.Player == null || region.Director == null || Cleared) return;

            if (!Active)
            {
                if (new Bounds(transform.position, size).Contains(region.Player.position)) Begin(region);
                return;
            }

            alive.RemoveAll(e => e == null || e.IsDead);
            if (alive.Count == 0) Finish(region);
        }

        void Begin(ExplorationRegion region)
        {
            Active = true;
            foreach (var spawn in spawns)
            {
                if (spawn.profile == null) continue;
                var enemy = region.Director.SpawnAt(spawn.profile, transform.position + spawn.offset);
                if (enemy != null) alive.Add(enemy);
            }
            SetBarriers(true);
        }

        void Finish(ExplorationRegion region)
        {
            Active = false;
            ExplorationSave.SetFlag(id);
            SetBarriers(false);

            var got = new List<string>();
            foreach (var skill in Rewards)
            {
                if (skill == null || !ExplorationSave.Unlock(skill)) continue;
                region.Slots.EquipNewSkill(skill);
                got.Add(skill.displayName);
            }
            if (got.Count > 0)
            {
                region.Notify($"スキルを手に入れた\n<size=30>{string.Join("・", got)}</size>\n<size=22>[P] 編成で組み替えられる</size>", Color.white);
            }
            if (isFinal) region.Director.CompleteExploration();
        }

        void SetBarriers(bool on)
        {
            if (barriers == null) return;
            foreach (var barrier in barriers)
            {
                if (barrier != null) barrier.SetActive(on);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.4f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
