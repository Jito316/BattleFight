using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 効果音を鳴らす。音の差し替え表(SoundLibrary)に音のファイルがあればそれを、なければ SfxSynth が作った音を鳴らす
    /// (作った音は、はじめて使うときに作る)。同時に鳴らせる数は mVoiceCount まで。
    /// 同じ音が短い間に重なりすぎないよう、同じ音は mMinInterval 秒あけて鳴らす(範囲攻撃で一度に当たったときなど)。
    /// </summary>
    public class SoundPlayer : MonoBehaviour
    {
        const int mVoiceCount = 12;
        const float mMinInterval = 0.03f;
        const float mMasterVolume = 0.6f;

        static SoundPlayer mInstance;
        static readonly Dictionary<Sfx, AudioClip> mClips = new Dictionary<Sfx, AudioClip>();

        readonly List<AudioSource> mVoices = new List<AudioSource>();
        readonly Dictionary<Sfx, float> mLastPlayed = new Dictionary<Sfx, float>();
        int mNextVoice;

        /// <summary>鳴らした回数(テスト用)</summary>
        public static int PlayCount { get; private set; }
        /// <summary>最後に鳴らした音(テスト用)</summary>
        public static Sfx? LastPlayed { get; private set; }
        /// <summary>最後に鳴らした音のファイル(テスト用)</summary>
        public static AudioClip LastClip { get; private set; }

        /// <summary>音の差し替え表(シーンの CombatFeedback が入れる。空なら作った音だけ)</summary>
        public static SoundLibrary Library
        {
            get => mLibrary;
            set
            {
                mLibrary = value;
                if (mInstance != null) mInstance.ApplyOutput();
            }
        }
        static SoundLibrary mLibrary;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mInstance = null;
            mLibrary = null;
            PlayCount = 0;
            LastPlayed = null;
            LastClip = null;
        }

        static SoundPlayer Instance
        {
            get
            {
                if (mInstance == null)
                {
                    var aObject = new GameObject("SoundPlayer");
                    mInstance = aObject.AddComponent<SoundPlayer>();
                }
                return mInstance;
            }
        }

        /// <summary>効果音を鳴らす。iPitch は 1 がそのまま(少しずつ揺らして同じ音の繰り返しを目立たなくする)</summary>
        public static void Play(Sfx iSfx, float iVolume = 1f, float iPitch = 1f)
        {
            if (!Application.isPlaying) return;
            Instance.PlayVoice(iSfx, iVolume, iPitch);
        }

        void Awake()
        {
            for (int aIndex = 0; aIndex < mVoiceCount; aIndex++)
            {
                var aSource = gameObject.AddComponent<AudioSource>();
                aSource.playOnAwake = false;
                aSource.spatialBlend = 0f;
                mVoices.Add(aSource);
            }
            ApplyOutput();
        }

        /// <summary>鳴らす先(差し替え表の AudioMixer のグループ。なければ直接)</summary>
        void ApplyOutput()
        {
            var aOutput = mLibrary != null ? mLibrary.Output : null;
            foreach (var aVoice in mVoices) aVoice.outputAudioMixerGroup = aOutput;
        }

        void OnDestroy()
        {
            if (mInstance == this) mInstance = null;
        }

        void PlayVoice(Sfx iSfx, float iVolume, float iPitch)
        {
            float aNow = Time.unscaledTime;
            if (mLastPlayed.TryGetValue(iSfx, out float aLast) && aNow - aLast < mMinInterval) return;
            mLastPlayed[iSfx] = aNow;

            PlayCount++;
            LastPlayed = iSfx;
            var aEntry = mLibrary != null ? mLibrary.Find(iSfx) : null;
            var aClip = aEntry != null ? aEntry.Clips[Random.Range(0, aEntry.Clips.Length)] : Clip(iSfx);
            LastClip = aClip;
            // 設定の音量(ミュートなら鳴らさない)
            float aVolume = Mathf.Clamp01(iVolume) * mMasterVolume * GameSettings.EffectiveVolume * (aEntry != null ? aEntry.Volume : 1f);
            if (aVolume <= 0f) return;
            var aSource = mVoices[mNextVoice];
            mNextVoice = (mNextVoice + 1) % mVoices.Count;
            aSource.clip = aClip;
            aSource.volume = aVolume;
            float aVariation = aEntry != null ? aEntry.PitchVariation : 0.05f;
            aSource.pitch = iPitch * Random.Range(1f - aVariation, 1f + aVariation);
            aSource.Play();
        }

        static AudioClip Clip(Sfx iSfx)
        {
            if (mClips.TryGetValue(iSfx, out var aClip) && aClip != null) return aClip;
            var aSamples = SfxSynth.Generate(iSfx);
            aClip = AudioClip.Create($"Sfx_{iSfx}", aSamples.Length, 1, SfxSynth.SampleRate, false);
            aClip.SetData(aSamples, 0);
            mClips[iSfx] = aClip;
            return aClip;
        }
    }
}
