using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 演出の差し替え表。場面(EffectCue)ごとにプレハブを決めると、いつもの演出の代わりにそれを出す。
    /// プレハブが空の場面は、いつもの演出(コードで作る仮の演出)のまま。
    /// </summary>
    [CreateAssetMenu(menuName = "BattleFight/Effect Library", fileName = "EffectLibrary")]
    public class EffectLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public EffectCue Cue;
            [Tooltip("出す演出のプレハブ(パーティクルなど)。空ならいつもの演出")]
            public GameObject Prefab;
            [Tooltip("場面の大きさ(衝撃波の半径・着地の強さなど)に合わせて拡大するか。外すと Scale のまま")]
            public bool ScaleWithSize = true;
            [Tooltip("大きさの倍率")]
            public float Scale = 1f;
            [Tooltip("場面の色(武器の色・敵の予告の色など)を、パーティクルの開始色に入れるか")]
            public bool UseColor = true;
            [Tooltip("出しておく秒数。0 ならパーティクルの長さから決める(パーティクルがなければ 2 秒)")]
            [Min(0f)] public float Lifetime;
        }

        [SerializeField] List<Entry> mEntries = new List<Entry>();

        readonly Dictionary<EffectCue, Entry> mLookup = new Dictionary<EffectCue, Entry>();
        bool mBuilt;

        public IReadOnlyList<Entry> Entries => mEntries;

        /// <summary>iCue の差し替え(プレハブが入っていなければ null)</summary>
        public Entry Find(EffectCue iCue)
        {
            if (!mBuilt) Rebuild();
            return mLookup.TryGetValue(iCue, out var aEntry) && aEntry.Prefab != null ? aEntry : null;
        }

        /// <summary>表に場面がなければ足す(プレハブは空のまま)。足したら true</summary>
        public bool EnsureEntry(EffectCue iCue)
        {
            foreach (var aEntry in mEntries)
            {
                if (aEntry.Cue == iCue) return false;
            }
            mEntries.Add(new Entry { Cue = iCue });
            mBuilt = false;
            return true;
        }

        /// <summary>iCue のプレハブを決める(テストやツールから)</summary>
        public void Set(EffectCue iCue, GameObject iPrefab)
        {
            EnsureEntry(iCue);
            foreach (var aEntry in mEntries)
            {
                if (aEntry.Cue == iCue) aEntry.Prefab = iPrefab;
            }
            mBuilt = false;
        }

        void OnValidate() => mBuilt = false;

        void Rebuild()
        {
            mLookup.Clear();
            foreach (var aEntry in mEntries)
            {
                if (aEntry != null) mLookup[aEntry.Cue] = aEntry;
            }
            mBuilt = true;
        }
    }
}
