using System;
using UnityEngine;

namespace BattleFight
{
    /// <summary>編成画面などでゲームを止める。ヒットストップ(timeScale)より優先する。</summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }
        public static event Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsPaused = false;
            Changed = null;
        }

        public static void Set(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Changed?.Invoke(paused);
        }
    }
}
