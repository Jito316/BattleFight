using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 探索マップ全体。部屋の名前、最初に持っているスキル、祭壇・安全地点への復帰、画面に出すお知らせを管理する。
    /// 探索のステージを始めたときに ArenaDirector が Begin を呼ぶ。
    /// </summary>
    public class ExplorationRegion : MonoBehaviour
    {
        [Serializable]
        public class Room
        {
            public string name;
            public Vector3 center;
            public Vector3 size;

            public bool Contains(Vector3 localPosition) => new Bounds(center, size).Contains(localPosition);
        }

        [SerializeField] string regionName = "古城";
        [SerializeField, Tooltip("はじめて来たときの出現位置")] Transform spawnPoint;
        [SerializeField, Tooltip("最初から持っているスキル")] SkillData[] starterSkills;
        [SerializeField, Tooltip("探索モードの初期プリセット(持っているスキルだけで組む)")] SkillPreset[] starterPresets;
        [SerializeField, Tooltip("この高さより下に落ちたら、最後に触れた安全地点へ戻す")] float fallHeight = -8f;
        [SerializeField, Tooltip("落ちたときのダメージ")] float fallDamage = 15f;
        [SerializeField] List<Room> rooms = new List<Room>();

        public static ExplorationRegion Active { get; private set; }

        /// <summary>マップを生成するエディタ拡張から設定する</summary>
        public void Setup(string name, Transform spawn, SkillData[] starters, SkillPreset[] presets, float fall, List<Room> roomList)
        {
            regionName = name;
            spawnPoint = spawn;
            starterSkills = starters;
            starterPresets = presets;
            fallHeight = fall;
            rooms = roomList;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active = null;

        public string RegionName => regionName;
        public Transform Player { get; private set; }
        public SkillSlotController Slots { get; private set; }
        public ArenaDirector Director { get; private set; }
        public Material SurfaceMaterial { get; private set; }

        /// <summary>このマップで集められるスキルの数(最初から持っているものを含む)</summary>
        public int TotalSkills { get; private set; }

        public string Message { get; private set; }
        public Color MessageColor { get; private set; } = Color.white;
        public float MessageTime { get; private set; } = -99f;

        SafePoint lastSafePoint;
        Damageable playerDamageable;

        public void Begin(ArenaDirector director, Transform player, SkillSlotController slots, Material surface)
        {
            Active = this;
            Director = director;
            Player = player;
            Slots = slots;
            SurfaceMaterial = surface;
            playerDamageable = player.GetComponent<Damageable>();
            gameObject.SetActive(true);

            foreach (var skill in starterSkills) ExplorationSave.Unlock(skill);
            TotalSkills = CountSkills();
            ExplorationSave.TotalSkills = TotalSkills;
            slots.BeginCollection(starterPresets);

            // 最後に触れた祭壇から始める(まだなければ入口)
            var start = FindCheckpoint(ExplorationSave.Checkpoint);
            lastSafePoint = start != null ? start.GetComponent<SafePoint>() : null;
            Teleport(start != null ? start.RespawnPosition : spawnPoint.position);
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        int CountSkills()
        {
            var skills = new HashSet<SkillData>(starterSkills);
            foreach (var pickup in GetComponentsInChildren<SkillPickup>(true))
            {
                foreach (var skill in pickup.Skills) skills.Add(skill);
            }
            foreach (var zone in GetComponentsInChildren<EncounterZone>(true))
            {
                foreach (var skill in zone.Rewards) skills.Add(skill);
            }
            skills.Remove(null);
            return skills.Count;
        }

        Checkpoint FindCheckpoint(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var checkpoint in GetComponentsInChildren<Checkpoint>(true))
            {
                if (checkpoint.Id == id) return checkpoint;
            }
            return null;
        }

        public void Notify(string message, Color color)
        {
            Message = message;
            MessageColor = color;
            MessageTime = Time.unscaledTime;
        }

        /// <summary>プレイヤーが今いる部屋の名前(どこにもいなければ空)</summary>
        public string CurrentRoomName()
        {
            if (Player == null) return "";
            var local = transform.InverseTransformPoint(Player.position);
            foreach (var room in rooms)
            {
                if (room.Contains(local)) return room.name;
            }
            return "";
        }

        public void TouchSafePoint(SafePoint point) => lastSafePoint = point;

        void Update()
        {
            if (Player == null || GamePause.IsPaused || playerDamageable == null || playerDamageable.IsDead) return;
            if (Player.position.y >= fallHeight) return;

            // 谷に落ちた: 最後に触れた安全地点(なければ入口)へ戻し、少しダメージを受ける
            var respawn = lastSafePoint != null ? lastSafePoint.transform.position : spawnPoint.position;
            Teleport(respawn);
            playerDamageable.ApplyHit(new HitInfo { damage = fallDamage, direction = Vector3.forward });
            Notify("落ちてしまった…", new Color(1f, 0.6f, 0.5f));
        }

        void Teleport(Vector3 position)
        {
            // CharacterController は有効なままだと位置を上書きするので、一度止めて動かす
            var controller = Player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            Player.position = position + Vector3.up * 0.1f;
            if (controller != null) controller.enabled = true;
        }

        public void RestorePlayer()
        {
            if (playerDamageable != null && !playerDamageable.IsDead) playerDamageable.Heal(playerDamageable.MaxHealth);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            foreach (var room in rooms) Gizmos.DrawWireCube(room.center, room.size);
        }
    }
}
