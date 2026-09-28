using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BattleFight.Tests
{
    /// <summary>
    /// HUD の日本語フォントは使う文字だけに絞っている(Tools/subset_font.py)。
    /// コードやデータに新しい文字を足してフォントを作り直し忘れると、WebGL でその文字が表示されない。
    /// </summary>
    public class FontCoverageTests
    {
        const string FontPath = "Assets/_Project/Fonts/NotoSansJP-Bold-Subset.otf";
        static readonly Regex Escape = new Regex(@"\\u([0-9A-Fa-f]{4})");

        [Test]
        public void SubsetFont_ContainsEveryCharacterUsedInTheGame()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.NotNull(font, FontPath);

            var used = new HashSet<char>();
            foreach (var path in Directory.GetFiles("Assets/_Project", "*.*", SearchOption.AllDirectories))
            {
                if (!path.EndsWith(".cs") && !path.EndsWith(".asset")) continue;
                // テストのメッセージは画面に出ないので対象外(subset_font.py と同じ)
                if (path.Replace('\\', '/').Contains("/Tests/")) continue;
                string text = File.ReadAllText(path, Encoding.UTF8);
                foreach (char c in text) used.Add(c);
                foreach (Match m in Escape.Matches(text)) used.Add((char)System.Convert.ToInt32(m.Groups[1].Value, 16));
            }

            var missing = new StringBuilder();
            foreach (char c in used)
            {
                // 制御文字・サロゲート・ASCII 以外の記号でフォントにない字を集める
                if (c < 0x20 || char.IsSurrogate(c)) continue;
                if (!font.HasCharacter(c)) missing.Append(c);
            }
            Assert.IsEmpty(missing.ToString(),
                "フォントにない文字があります。`python Tools/subset_font.py` を実行してフォントを作り直してください: " + missing);
        }
    }
}
