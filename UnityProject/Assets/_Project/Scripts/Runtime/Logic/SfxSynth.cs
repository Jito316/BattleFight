using System;

namespace BattleFight
{
    /// <summary>効果音の種類</summary>
    public enum Sfx
    {
        Swing,
        HitSword,
        HitHammer,
        HitChain,
        HitStaff,
        HitGun,
        HitGauntlet,
        /// <summary>弱点に当てたときに重ねる高い音</summary>
        Weak,
        ArmorBreak,
        Defeat,
        Hurt,
        Dodge,
        Swap,
        /// <summary>銃を撃った音</summary>
        Shot,
        /// <summary>敵の攻撃の予備動作(だんだん高くなる音で、攻撃が来ることを知らせる)</summary>
        EnemyWindup,
        /// <summary>ガードで受け止めた音(金属がぶつかる重い音)</summary>
        Guard,
        /// <summary>パリィ・カウンター(高く澄んで長く響く音)</summary>
        Parry,
    }

    /// <summary>
    /// 効果音をコードで作る(音のファイルを使わない。ライセンスもダウンロード量もかからない)。
    /// ノイズ・音程が下がる正弦波・金属っぽい倍音を組み合わせ、最後に最大 0.9 にそろえる。
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 22050;
        const float mPeak = 0.9f;

        public static float[] Generate(Sfx iSfx)
        {
            var aRandom = new Random(1000 + (int)iSfx);
            float[] aSamples = iSfx switch
            {
                Sfx.Swing => Whoosh(aRandom, 0.18f, 0.35f, false),
                Sfx.Dodge => Whoosh(aRandom, 0.25f, 0.8f, true),
                Sfx.HitSword => Mix(Click(aRandom, 0.25f, 60f, 0.5f), Ring(0.25f, 16f, 0.5f, 2200f, 3300f, 4700f), Hiss(aRandom, 0.25f, 22f, 0.4f, 0.85f)),
                Sfx.HitHammer => Saturate(Mix(Drop(0.45f, 110f, 45f, 7f, 1f), Rumble(aRandom, 0.45f, 12f, 0.7f, 0.08f)), 2f),
                Sfx.HitChain => Mix(Click(aRandom, 0.3f, 70f, 0.4f), Ring(0.3f, 14f, 0.7f, 523f, 1187f, 1843f, 2711f)),
                Sfx.HitStaff => Mix(Zap(0.3f, 1400f, 300f, 9f, 0.8f), Hiss(aRandom, 0.3f, 14f, 0.25f, 0.9f)),
                Sfx.HitGun => Mix(Hiss(aRandom, 0.15f, 35f, 0.8f, 0.7f), Drop(0.15f, 300f, 160f, 30f, 0.5f)),
                Sfx.Shot => Mix(Rumble(aRandom, 0.2f, 25f, 0.9f, 0.25f), Drop(0.2f, 90f, 60f, 20f, 0.7f)),
                Sfx.HitGauntlet => Saturate(Mix(Drop(0.25f, 150f, 60f, 14f, 1f), Click(aRandom, 0.25f, 60f, 0.6f)), 1.5f),
                Sfx.Weak => Ring(0.35f, 8f, 0.6f, 1568f, 2349f),
                Sfx.ArmorBreak => Mix(Rumble(aRandom, 0.5f, 9f, 0.6f, 0.3f), Ring(0.5f, 7f, 0.5f, 731f, 1301f, 1977f, 2633f, 3511f), Drop(0.5f, 120f, 50f, 10f, 0.6f)),
                Sfx.Defeat => Saturate(Mix(Drop(0.6f, 80f, 30f, 5f, 1f), Rumble(aRandom, 0.6f, 5f, 0.8f, 0.1f)), 1.5f),
                Sfx.Hurt => Saturate(Drop(0.25f, 220f, 120f, 10f, 1f), 3f),
                Sfx.Swap => Mix(Ring(0.08f, 60f, 0.6f, 1800f), Ring(0.08f, 80f, 0.4f, 2400f)),
                Sfx.EnemyWindup => Rise(0.3f, 260f, 620f, 0.8f),
                Sfx.Guard => Mix(Click(aRandom, 0.3f, 50f, 0.6f), Ring(0.3f, 12f, 0.8f, 880f, 1410f, 2190f), Drop(0.3f, 180f, 90f, 18f, 0.6f)),
                Sfx.Parry => Mix(Ring(0.7f, 4.5f, 0.8f, 2637f, 3951f, 5274f), Click(aRandom, 0.7f, 80f, 0.5f), Whoosh(aRandom, 0.25f, 0.9f, false)),
                _ => new float[1],
            };
            return Normalize(aSamples);
        }

        static int Length(float iSeconds) => Math.Max(1, (int)(iSeconds * SampleRate));

        static float Time(int iIndex) => iIndex / (float)SampleRate;

        /// <summary>風を切る音: 明るさが上がって下がるノイズ。iReverse なら後ろへ向かって大きくなる</summary>
        static float[] Whoosh(Random iRandom, float iSeconds, float iBrightness, bool iReverse)
        {
            var aOut = new float[Length(iSeconds)];
            float aLow = 0f;
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++)
            {
                float aProgress = aIndex / (float)aOut.Length;
                if (iReverse) aProgress = 1f - aProgress;
                float aEnvelope = (float)Math.Sin(Math.PI * Math.Min(1f, aProgress * 1.6f)) * (1f - aProgress * 0.5f);
                float aCutoff = 0.05f + iBrightness * 0.3f * (float)Math.Sin(Math.PI * aProgress);
                aLow += (Noise(iRandom) - aLow) * aCutoff;
                aOut[aIndex] = aLow * aEnvelope;
            }
            return aOut;
        }

        /// <summary>音程が下がっていく正弦波(打撃の芯)</summary>
        static float[] Drop(float iSeconds, float iFrom, float iTo, float iDecay, float iGain)
        {
            var aOut = new float[Length(iSeconds)];
            double aPhase = 0;
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++)
            {
                float aTime = Time(aIndex);
                float aFrequency = iTo + (iFrom - iTo) * (float)Math.Exp(-aTime * 18f);
                aPhase += 2 * Math.PI * aFrequency / SampleRate;
                aOut[aIndex] = (float)Math.Sin(aPhase) * (float)Math.Exp(-aTime * iDecay) * iGain;
            }
            return aOut;
        }

        /// <summary>だんだん高く・大きくなる音(予備動作)。最後は少し短く切る</summary>
        static float[] Rise(float iSeconds, float iFrom, float iTo, float iGain)
        {
            var aOut = new float[Length(iSeconds)];
            double aPhase = 0;
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++)
            {
                float aProgress = aIndex / (float)aOut.Length;
                float aFrequency = iFrom + (iTo - iFrom) * aProgress * aProgress;
                aPhase += 2 * Math.PI * aFrequency / SampleRate;
                float aEnvelope = aProgress * Math.Min(1f, (1f - aProgress) * 12f);
                // 少し角ばった音にして、ほかの効果音に埋もれないようにする
                aOut[aIndex] = (float)Math.Tanh(Math.Sin(aPhase) * 2.5) * aEnvelope * iGain;
            }
            return aOut;
        }

        /// <summary>金属のように響く倍音</summary>
        static float[] Ring(float iSeconds, float iDecay, float iGain, params float[] iFrequencies)
        {
            var aOut = new float[Length(iSeconds)];
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++)
            {
                float aTime = Time(aIndex);
                float aSum = 0f;
                for (int aPartial = 0; aPartial < iFrequencies.Length; aPartial++)
                {
                    aSum += (float)Math.Sin(2 * Math.PI * iFrequencies[aPartial] * aTime) * (float)Math.Exp(-aTime * iDecay * (1f + aPartial * 0.3f));
                }
                aOut[aIndex] = aSum / iFrequencies.Length * iGain;
            }
            return aOut;
        }

        /// <summary>周波数が急に下がる電子音(魔法)</summary>
        static float[] Zap(float iSeconds, float iFrom, float iTo, float iDecay, float iGain)
        {
            var aOut = new float[Length(iSeconds)];
            double aPhase = 0;
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++)
            {
                float aTime = Time(aIndex);
                float aFrequency = iTo + (iFrom - iTo) * (float)Math.Exp(-aTime * 14f);
                aPhase += 2 * Math.PI * aFrequency / SampleRate;
                float aModulator = (float)Math.Sin(aPhase * 2.01) * 2f;
                aOut[aIndex] = (float)Math.Sin(aPhase + aModulator) * (float)Math.Exp(-aTime * iDecay) * iGain;
            }
            return aOut;
        }

        /// <summary>ごく短いノイズ(当たった瞬間のカチッ)</summary>
        static float[] Click(Random iRandom, float iSeconds, float iDecay, float iGain)
        {
            var aOut = new float[Length(iSeconds)];
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++) aOut[aIndex] = Noise(iRandom) * (float)Math.Exp(-Time(aIndex) * iDecay) * iGain;
            return aOut;
        }

        /// <summary>高い成分だけのノイズ(シャッという擦れ)</summary>
        static float[] Hiss(Random iRandom, float iSeconds, float iDecay, float iGain, float iHighPass)
        {
            var aOut = new float[Length(iSeconds)];
            float aLow = 0f;
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++)
            {
                float aNoise = Noise(iRandom);
                aLow += (aNoise - aLow) * (1f - iHighPass);
                aOut[aIndex] = (aNoise - aLow) * (float)Math.Exp(-Time(aIndex) * iDecay) * iGain;
            }
            return aOut;
        }

        /// <summary>低い成分だけのノイズ(ドッという重さ・土煙)</summary>
        static float[] Rumble(Random iRandom, float iSeconds, float iDecay, float iGain, float iLowPass)
        {
            var aOut = new float[Length(iSeconds)];
            float aLow = 0f;
            for (int aIndex = 0; aIndex < aOut.Length; aIndex++)
            {
                aLow += (Noise(iRandom) - aLow) * iLowPass;
                aOut[aIndex] = aLow * (float)Math.Exp(-Time(aIndex) * iDecay) * iGain * 3f;
            }
            return aOut;
        }

        static float[] Mix(params float[][] iLayers)
        {
            int aLength = 0;
            foreach (var aLayer in iLayers) aLength = Math.Max(aLength, aLayer.Length);
            var aOut = new float[aLength];
            foreach (var aLayer in iLayers)
            {
                for (int aIndex = 0; aIndex < aLayer.Length; aIndex++) aOut[aIndex] += aLayer[aIndex];
            }
            return aOut;
        }

        /// <summary>少し歪ませて、重く・太くする</summary>
        static float[] Saturate(float[] iSamples, float iDrive)
        {
            for (int aIndex = 0; aIndex < iSamples.Length; aIndex++) iSamples[aIndex] = (float)Math.Tanh(iSamples[aIndex] * iDrive);
            return iSamples;
        }

        static float[] Normalize(float[] iSamples)
        {
            float aMax = 0f;
            foreach (var aSample in iSamples) aMax = Math.Max(aMax, Math.Abs(aSample));
            if (aMax < 1e-6f) return iSamples;
            float aScale = mPeak / aMax;
            for (int aIndex = 0; aIndex < iSamples.Length; aIndex++) iSamples[aIndex] *= aScale;
            return iSamples;
        }

        static float Noise(Random iRandom) => (float)(iRandom.NextDouble() * 2.0 - 1.0);

        /// <summary>武器で当てたときの音</summary>
        public static Sfx HitFor(WeaponType iWeapon) => iWeapon switch
        {
            WeaponType.Hammer => Sfx.HitHammer,
            WeaponType.Chain => Sfx.HitChain,
            WeaponType.Staff => Sfx.HitStaff,
            WeaponType.Gun => Sfx.HitGun,
            WeaponType.Gauntlet => Sfx.HitGauntlet,
            _ => Sfx.HitSword,
        };
    }
}
