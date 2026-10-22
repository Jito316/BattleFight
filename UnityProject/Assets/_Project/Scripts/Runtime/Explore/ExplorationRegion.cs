using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
            [FormerlySerializedAs("name")] public string Name;
            [FormerlySerializedAs("center")] public Vector3 Center;
            [FormerlySerializedAs("size")] public Vector3 Size;

            public bool Contains(Vector3 iLocalPosition) => new Bounds(Center, Size).Contains(iLocalPosition);
        }

        [SerializeField, FormerlySerializedAs("regionName")] string mRegionName = "古城";
        [SerializeField, Tooltip("はじめて来たときの出現位置"), FormerlySerializedAs("spawnPoint")] Transform mSpawnPoint;
        [SerializeField, Tooltip("最初から持っているスキル"), FormerlySerializedAs("starterSkills")] SkillData[] mStarterSkills;
        [SerializeField, Tooltip("探索モードの初期プリセット(持っているスキルだけで組む)"), FormerlySerializedAs("starterPresets")] SkillPreset[] mStarterPresets;
        [SerializeField, Tooltip("この高さより下に落ちたら、最後に触れた安全地点へ戻す"), FormerlySerializedAs("fallHeight")] float mFallHeight = -8f;
        [SerializeField, Tooltip("落ちたときのダメージ"), FormerlySerializedAs("fallDamage")] float mFallDamage = 15f;
        [SerializeField, FormerlySerializedAs("rooms")] List<Room> mRooms = new List<Room>();

        public static ExplorationRegion Active { get; private set; }

        /// <summary>マップを生成するエディタ拡張から設定する</summary>
        public void Setup(string iName, Transform iSpawn, SkillData[] iStarters, SkillPreset[] iPresets, float iFall, List<Room> iRoomList)
        {
            mRegionName = iName;
            mSpawnPoint = iSpawn;
            mStarterSkills = iStarters;
            mStarterPresets = iPresets;
            mFallHeight = iFall;
            mRooms = iRoomList;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active = null;

        public string RegionName => mRegionName;
        public Transform Player { get; private set; }
        public SkillSlotController Slots { get; private set; }
        public ArenaDirector Director { get; private set; }
        public Material SurfaceMaterial { get; private set; }

        /// <summary>このマップで集められるスキルの数(最初から持っているものを含む)</summary>
        public int TotalSkills { get; private set; }

        public string Message { get; private set; }
        public Color MessageColor { get; private set; } = Color.white;
        public float MessageTime { get; private set; } = -99f;

        SafePoint mLastSafePoint;
        Damageable mPlayerDamageable;

        public void Begin(ArenaDirector iDirector, Transform iPlayer, SkillSlotController iSlots, Material iSurface)
        {
            Active = this;
            Director = iDirector;
            Player = iPlayer;
            Slots = iSlots;
            SurfaceMaterial = iSurface;
            mPlayerDamageable = iPlayer.GetComponent<Damageable>();
            gameObject.SetActive(true);

            foreach (var aSkill in mStarterSkills) ExplorationSave.Unlock(aSkill);
            TotalSkills = CountSkills();
            ExplorationSave.TotalSkills = TotalSkills;
            iSlots.BeginCollection(mStarterPresets);

            // 最後に触れた祭壇から始める(まだなければ入口)
            var aStart = FindCheckpoint(ExplorationSave.Checkpoint);
            mLastSafePoint = aStart != null ? aStart.GetComponent<SafePoint>() : null;
            Teleport(aStart != null ? aStart.RespawnPosition : mSpawnPoint.position);
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        int CountSkills()
        {
            var aSkills = new HashSet<SkillData>(mStarterSkills);
            foreach (var aPickup in GetComponentsInChildren<SkillPickup>(true))
            {
                foreach (var aSkill in aPickup.Skills) aSkills.Add(aSkill);
            }
            foreach (var aZone in GetComponentsInChildren<EncounterZone>(true))
            {
                foreach (var aSkill in aZone.Rewards) aSkills.Add(aSkill);
            }
            aSkills.Remove(null);
            return aSkills.Count;
        }

        Checkpoint FindCheckpoint(string iId)
        {
            if (string.IsNullOrEmpty(iId)) return null;
            foreach (var aCheckpoint in GetComponentsInChildren<Checkpoint>(true))
            {
                if (aCheckpoint.Id == iId) return aCheckpoint;
            }
            return null;
        }

        public void Notify(string iMessage, Color iColor)
        {
            Message = iMessage;
            MessageColor = iColor;
            MessageTime = Time.unscaledTime;
        }

        /// <summary>プレイヤーが今いる部屋の名前(どこにもいなければ空)</summary>
        public string CurrentRoomName()
        {
            if (Player == null) return "";
            var aLocal = transform.InverseTransformPoint(Player.position);
            foreach (var aRoom in mRooms)
            {
                if (aRoom.Contains(aLocal)) return aRoom.Name;
            }
            return "";
        }

        public void TouchSafePoint(SafePoint iPoint) => mLastSafePoint = iPoint;

        void Update()
        {
            if (Player == null || GamePause.IsPaused || mPlayerDamageable == null || mPlayerDamageable.IsDead) return;
            if (Player.position.y >= mFallHeight) return;

            // 谷に落ちた: 最後に触れた安全地点(なければ入口)へ戻し、少しダメージを受ける
            var aRespawn = mLastSafePoint != null ? mLastSafePoint.transform.position : mSpawnPoint.position;
            Teleport(aRespawn);
            mPlayerDamageable.ApplyHit(new HitInfo { Damage = mFallDamage, Direction = Vector3.forward });
            Notify("落ちてしまった…", new Color(1f, 0.6f, 0.5f));
        }

        void Teleport(Vector3 iPosition)
        {
            // CharacterController は有効なままだと位置を上書きするので、一度止めて動かす
            var aController = Player.GetComponent<CharacterController>();
            if (aController != null) aController.enabled = false;
            Player.position = iPosition + Vector3.up * 0.1f;
            if (aController != null) aController.enabled = true;
        }

        public void RestorePlayer()
        {
            if (mPlayerDamageable != null && !mPlayerDamageable.IsDead) mPlayerDamageable.Heal(mPlayerDamageable.MaxHealth);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            foreach (var aRoom in mRooms) Gizmos.DrawWireCube(aRoom.Center, aRoom.Size);
        }
    }
}
