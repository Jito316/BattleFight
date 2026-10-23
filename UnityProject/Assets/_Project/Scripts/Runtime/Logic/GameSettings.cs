using System;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 遊ぶ人が変えられる設定(音量・画面の揺れ・画面の光・ズーム・振動・カメラの感度)。
    /// セーブスロットとは別に、この端末に 1 つだけ保存する。演出の強さは「1 = 作ったとおり、0 = 出さない」の倍率。
    /// </summary>
    public static class GameSettings
    {
        public const string SaveKey = "BattleFight.Settings.v1";

        [Serializable]
        class SavedData
        {
            public float Volume = 1f;
            public bool Muted;
            public float Shake = 1f;
            public float Flash = 1f;
            public float Zoom = 1f;
            public bool Vibration = true;
            public float CameraSensitivity = 1f;
            public bool HideGuide;
        }

        static SavedData mData;

        /// <summary>設定が変わった</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mData = null;
            Changed = null;
        }

        static SavedData Data
        {
            get
            {
                if (mData != null) return mData;
                mData = Load(SaveProfiles.Store.GetString(SaveKey, null)) ?? new SavedData();
                return mData;
            }
        }

        /// <summary>音量(0 〜 1)。ミュート中でも、戻したときの音量として覚えている</summary>
        public static float Volume { get => Data.Volume; set => Set(() => Data.Volume = Mathf.Clamp01(value)); }
        public static bool Muted { get => Data.Muted; set => Set(() => Data.Muted = value); }
        /// <summary>実際に鳴らす音量(ミュートなら 0)</summary>
        public static float EffectiveVolume => Muted ? 0f : Volume;
        /// <summary>画面の揺れ(カメラの揺れ・押し・沈み込み)の倍率</summary>
        public static float Shake { get => Data.Shake; set => Set(() => Data.Shake = Mathf.Clamp01(value)); }
        /// <summary>画面の光(インパクトフレーム・画面の縁の光)の倍率</summary>
        public static float Flash { get => Data.Flash; set => Set(() => Data.Flash = Mathf.Clamp01(value)); }
        /// <summary>ズーム(重い一撃・倒した瞬間の寄り)の倍率</summary>
        public static float Zoom { get => Data.Zoom; set => Set(() => Data.Zoom = Mathf.Clamp01(value)); }
        public static bool Vibration { get => Data.Vibration; set => Set(() => Data.Vibration = value); }
        /// <summary>カメラの感度(0.5 〜 2)</summary>
        public static float CameraSensitivity
        {
            get => Data.CameraSensitivity;
            set => Set(() => Data.CameraSensitivity = Mathf.Clamp(value, MinCameraSensitivity, MaxCameraSensitivity));
        }

        public const float MinCameraSensitivity = 0.5f;
        public const float MaxCameraSensitivity = 2f;

        /// <summary>すべて作ったときの値に戻す</summary>
        /// <summary>操作説明を出すか(一度閉じたら、次に遊ぶときも閉じたまま)</summary>
        public static bool ShowGuide { get => !Data.HideGuide; set => Set(() => Data.HideGuide = !value); }

        public static void ResetToDefaults() => Set(() => mData = new SavedData());

        /// <summary>保存された文字列から読み直す(テスト用)</summary>
        public static void Reload() => mData = null;

        static void Set(Action iChange)
        {
            iChange();
            SaveProfiles.Store.SetString(SaveKey, JsonUtility.ToJson(Data));
            SaveProfiles.Store.Save();
            Changed?.Invoke();
        }

        static SavedData Load(string iJson)
        {
            if (string.IsNullOrEmpty(iJson)) return null;
            try
            {
                return JsonUtility.FromJson<SavedData>(iJson);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
