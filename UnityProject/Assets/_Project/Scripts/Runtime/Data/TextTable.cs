using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleFight
{
    /// <summary>
    /// 画面に出す文言の表(キー → 文言)。コードはキーだけを持ち、文言はここで直す。
    /// {0} {1} などは、コードが数や名前を入れる所(数えられる所は {0:0.0} のように書式も書ける)。
    /// 文言の中の &lt;color=#…&gt; &lt;size=…&gt; は、色・大きさの指定(UI Toolkit のリッチテキスト)。
    /// </summary>
    [CreateAssetMenu(menuName = "BattleFight/Text Table", fileName = "GameText")]
    public class TextTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string Key;
            [TextArea(1, 4)] public string Text;
        }

        [SerializeField] List<Entry> mEntries = new List<Entry>();

        readonly Dictionary<string, string> mLookup = new Dictionary<string, string>();
        bool mBuilt;

        public IReadOnlyList<Entry> Entries => mEntries;

        public bool TryGet(string iKey, out string oText)
        {
            if (!mBuilt) Rebuild();
            return mLookup.TryGetValue(iKey, out oText);
        }

        /// <summary>表にキーがなければ足す(あれば文言には手を付けない)。足したら true</summary>
        public bool EnsureEntry(string iKey, string iText)
        {
            if (!mBuilt) Rebuild();
            if (mLookup.ContainsKey(iKey)) return false;
            mEntries.Add(new Entry { Key = iKey, Text = iText });
            mLookup[iKey] = iText;
            return true;
        }

        /// <summary>キーの文言を決める(テストやツールから)</summary>
        public void Set(string iKey, string iText)
        {
            if (!EnsureEntry(iKey, iText))
            {
                foreach (var aEntry in mEntries)
                {
                    if (aEntry.Key == iKey) aEntry.Text = iText;
                }
            }
            mBuilt = false;
        }

        void OnValidate() => mBuilt = false;

        void Rebuild()
        {
            mLookup.Clear();
            foreach (var aEntry in mEntries)
            {
                if (aEntry != null && !string.IsNullOrEmpty(aEntry.Key)) mLookup[aEntry.Key] = aEntry.Text ?? "";
            }
            mBuilt = true;
        }
    }
}
