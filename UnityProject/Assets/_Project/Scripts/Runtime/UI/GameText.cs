using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 画面に出す文言を、文言の表(Resources/GameText.asset)から引く。コードには文言を書かず、キーで引く。
    /// 表にないキーは「[キー]」と出し、一度だけ警告する(表の直し忘れに気づけるように)。
    /// </summary>
    public static class GameText
    {
        /// <summary>Resources の中の、文言の表の名前</summary>
        public const string ResourceName = "GameText";

        static TextTable mTable;
        static bool mLoaded;
        static readonly HashSet<string> mReported = new HashSet<string>();
        static readonly List<string> mLines = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mTable = null;
            mLoaded = false;
            mReported.Clear();
        }

        /// <summary>使っている文言の表(テストで差し替えられる。null を入れると、次に使うときに Resources から読み直す)</summary>
        public static TextTable Table
        {
            get
            {
                if (!mLoaded)
                {
                    mTable = Resources.Load<TextTable>(ResourceName);
                    mLoaded = true;
                }
                return mTable;
            }
            set
            {
                mTable = value;
                mLoaded = value != null;
            }
        }

        public static bool Has(string iKey) => Table != null && Table.TryGet(iKey, out _);

        public static string Get(string iKey)
        {
            if (Table != null && Table.TryGet(iKey, out var aText)) return aText;
            if (mReported.Add(iKey)) Debug.LogWarning($"[GameText] 文言の表に「{iKey}」がない");
            return $"[{iKey}]";
        }

        /// <summary>{0} {1} … に iArgs を入れた文言</summary>
        public static string Format(string iKey, params object[] iArgs)
        {
            string aText = Get(iKey);
            try
            {
                return string.Format(aText, iArgs);
            }
            catch (System.FormatException)
            {
                // 表の { } の書き間違い。止めずに、そのまま出す
                if (mReported.Add(iKey + "#format")) Debug.LogWarning($"[GameText] 「{iKey}」の {{ }} の書き方が正しくない: {aText}");
                return aText;
            }
        }

        /// <summary>iPrefix.1, iPrefix.2 … と、表にある限り続く文言(コツの一覧など。表に足せば増える)</summary>
        public static IReadOnlyList<string> Lines(string iPrefix)
        {
            mLines.Clear();
            for (int aIndex = 1; Has($"{iPrefix}.{aIndex}"); aIndex++) mLines.Add(Get($"{iPrefix}.{aIndex}"));
            return mLines;
        }
    }
}
