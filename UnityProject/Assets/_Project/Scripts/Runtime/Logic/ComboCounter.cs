namespace BattleFight
{
    /// <summary>
    /// 途切れずに当て続けた数(コンボ)。最後に当ててから一定時間たつか、被弾すると途切れる。
    /// 途切れたときの数は LastFinished に残す(HUD の評価表示と、スタイルのボーナスに使う)。
    /// </summary>
    public class ComboCounter
    {
        public const float DefaultWindow = 2.5f;

        readonly float mWindow;
        float mLastHitTime = float.NegativeInfinity;

        public ComboCounter(float iWindow = DefaultWindow)
        {
            mWindow = iWindow;
        }

        public int Count { get; private set; }
        public int Best { get; private set; }
        /// <summary>最後に途切れたコンボの数</summary>
        public int LastFinished { get; private set; }
        /// <summary>最後に途切れた時刻</summary>
        public float LastFinishedTime { get; private set; } = float.NegativeInfinity;

        /// <summary>残り時間の割合(1 = 当てた直後、0 = 途切れる)</summary>
        public float Remaining(float iTime) => Count > 0 ? UnityEngine.Mathf.Clamp01(1f - (iTime - mLastHitTime) / mWindow) : 0f;

        public void AddHit(float iTime, int iHits = 1)
        {
            Tick(iTime);
            Count += iHits;
            mLastHitTime = iTime;
            if (Count > Best) Best = Count;
        }

        /// <summary>時間切れで途切れたら、その数を返す(途切れていなければ 0)</summary>
        public int Tick(float iTime)
        {
            if (Count > 0 && iTime - mLastHitTime > mWindow) return Finish(iTime);
            return 0;
        }

        /// <summary>被弾などで今すぐ途切れさせる。途切れた数を返す</summary>
        public int Break(float iTime) => Count > 0 ? Finish(iTime) : 0;

        int Finish(float iTime)
        {
            int aFinished = Count;
            LastFinished = aFinished;
            LastFinishedTime = iTime;
            Count = 0;
            return aFinished;
        }

        /// <summary>コンボの数に応じた評価(HUD 用)。少ないときは空</summary>
        public static string Rating(int iCount)
        {
            if (iCount >= 100) return "UNREAL";
            if (iCount >= 50) return "AMAZING";
            if (iCount >= 25) return "GREAT";
            if (iCount >= 10) return "NICE";
            return "";
        }
    }
}
