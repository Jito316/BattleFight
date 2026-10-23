using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BattleFight
{
    /// <summary>
    /// 音の差し替え表。音の種類(Sfx)ごとに音のファイルを決めると、コードで作った音の代わりにそれを鳴らす。
    /// ファイルが空の種類は、コードで作った音(SfxSynth)のまま。
    /// </summary>
    [CreateAssetMenu(menuName = "BattleFight/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public Sfx Sound;
            [Tooltip("鳴らす音のファイル。2 つ以上なら、鳴らすたびにどれか 1 つを選ぶ。空ならコードで作った音")]
            public AudioClip[] Clips = Array.Empty<AudioClip>();
            [Tooltip("音量の倍率(ゲームの中の音量・設定の音量に掛ける)")]
            [Range(0f, 2f)] public float Volume = 1f;
            [Tooltip("鳴らすたびに音の高さを揺らす幅(0.05 なら ±5%)。同じ音の繰り返しを目立たなくする")]
            [Range(0f, 0.5f)] public float PitchVariation = 0.05f;
        }

        [SerializeField, Tooltip("鳴らす先の AudioMixer のグループ(効果音のグループなど)。空なら直接鳴らす")]
        AudioMixerGroup mOutput;
        [SerializeField] List<Entry> mEntries = new List<Entry>();

        readonly Dictionary<Sfx, Entry> mLookup = new Dictionary<Sfx, Entry>();
        bool mBuilt;

        public AudioMixerGroup Output => mOutput;
        public IReadOnlyList<Entry> Entries => mEntries;

        /// <summary>iSound の差し替え(音のファイルが入っていなければ null)</summary>
        public Entry Find(Sfx iSound)
        {
            if (!mBuilt) Rebuild();
            if (!mLookup.TryGetValue(iSound, out var aEntry) || aEntry.Clips == null || aEntry.Clips.Length == 0) return null;
            foreach (var aClip in aEntry.Clips)
            {
                if (aClip == null) return null;
            }
            return aEntry;
        }

        /// <summary>表に音の種類がなければ足す(ファイルは空のまま)。足したら true</summary>
        public bool EnsureEntry(Sfx iSound)
        {
            foreach (var aEntry in mEntries)
            {
                if (aEntry.Sound == iSound) return false;
            }
            mEntries.Add(new Entry { Sound = iSound });
            mBuilt = false;
            return true;
        }

        /// <summary>iSound の音のファイルを決める(テストやツールから)</summary>
        public void Set(Sfx iSound, params AudioClip[] iClips)
        {
            EnsureEntry(iSound);
            foreach (var aEntry in mEntries)
            {
                if (aEntry.Sound == iSound) aEntry.Clips = iClips;
            }
            mBuilt = false;
        }

        void OnValidate() => mBuilt = false;

        void Rebuild()
        {
            mLookup.Clear();
            foreach (var aEntry in mEntries)
            {
                if (aEntry != null) mLookup[aEntry.Sound] = aEntry;
            }
            mBuilt = true;
        }
    }
}
