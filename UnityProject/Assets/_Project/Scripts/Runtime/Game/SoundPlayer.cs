using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 効果音を鳴らす。音は SfxSynth がはじめて使うときに作る。同時に鳴らせる数は mVoiceCount まで。
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mInstance = null;
            PlayCount = 0;
            LastPlayed = null;
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

            var aSource = mVoices[mNextVoice];
            mNextVoice = (mNextVoice + 1) % mVoices.Count;
            aSource.clip = Clip(iSfx);
            aSource.volume = Mathf.Clamp01(iVolume) * mMasterVolume;
            aSource.pitch = iPitch * Random.Range(0.95f, 1.05f);
            aSource.Play();
            PlayCount++;
            LastPlayed = iSfx;
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
