namespace BattleFight
{
    /// <summary>
    /// 途切れずに当て続けた数(コンボ)。最後に当ててから一定時間たつか、被弾すると途切れる。
    /// 途切れたときの数は LastFinished に残す(HUD の評価表示と、スタイルのボーナスに使う)。
    /// </summary>
    public class ComboCounter
    {
        public const float DefaultWindow = 2.5f;

        readonly float window;
        float lastHitTime = float.NegativeInfinity;

        public ComboCounter(float window = DefaultWindow)
        {
            this.window = window;
        }

        public int Count { get; private set; }
        public int Best { get; private set; }
        /// <summary>最後に途切れたコンボの数</summary>
        public int LastFinished { get; private set; }
        /// <summary>最後に途切れた時刻</summary>
        public float LastFinishedTime { get; private set; } = float.NegativeInfinity;

        /// <summary>残り時間の割合(1 = 当てた直後、0 = 途切れる)</summary>
        public float Remaining(float time) => Count > 0 ? UnityEngine.Mathf.Clamp01(1f - (time - lastHitTime) / window) : 0f;

        public void AddHit(float time, int hits = 1)
        {
            Tick(time);
            Count += hits;
            lastHitTime = time;
            if (Count > Best) Best = Count;
        }

        /// <summary>時間切れで途切れたら、その数を返す(途切れていなければ 0)</summary>
        public int Tick(float time)
        {
            if (Count > 0 && time - lastHitTime > window) return Finish(time);
            return 0;
        }

        /// <summary>被弾などで今すぐ途切れさせる。途切れた数を返す</summary>
        public int Break(float time) => Count > 0 ? Finish(time) : 0;

        int Finish(float time)
        {
            int finished = Count;
            LastFinished = finished;
            LastFinishedTime = time;
            Count = 0;
            return finished;
        }

        /// <summary>コンボの数に応じた評価(HUD 用)。少ないときは空</summary>
        public static string Rating(int count)
        {
            if (count >= 100) return "UNREAL";
            if (count >= 50) return "AMAZING";
            if (count >= 25) return "GREAT";
            if (count >= 10) return "NICE";
            return "";
        }
    }
}
