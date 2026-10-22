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
        const string mFontPath = "Assets/_Project/Fonts/NotoSansJP-Bold-Subset.otf";
        static readonly Regex mEscape = new Regex(@"\\u([0-9A-Fa-f]{4})");

        [Test]
        public void SubsetFont_ContainsEveryCharacterUsedInTheGame()
        {
            var aFont = AssetDatabase.LoadAssetAtPath<Font>(mFontPath);
            Assert.NotNull(aFont, mFontPath);

            var aUsed = new HashSet<char>();
            foreach (var aPath in Directory.GetFiles("Assets/_Project", "*.*", SearchOption.AllDirectories))
            {
                if (!aPath.EndsWith(".cs") && !aPath.EndsWith(".asset") && !aPath.EndsWith(".uxml")) continue;
                // テストのメッセージは画面に出ないので対象外(subset_font.py と同じ)
                if (aPath.Replace('\\', '/').Contains("/Tests/")) continue;
                string aText = File.ReadAllText(aPath, Encoding.UTF8);
                foreach (char aC in aText) aUsed.Add(aC);
                foreach (Match aM in mEscape.Matches(aText)) aUsed.Add((char)System.Convert.ToInt32(aM.Groups[1].Value, 16));
            }

            var aMissing = new StringBuilder();
            foreach (char aC in aUsed)
            {
                // 制御文字・サロゲート・ASCII 以外の記号でフォントにない字を集める
                if (aC < 0x20 || char.IsSurrogate(aC)) continue;
                if (!aFont.HasCharacter(aC)) aMissing.Append(aC);
            }
            Assert.IsEmpty(aMissing.ToString(),
                "フォントにない文字があります。`python Tools/subset_font.py` を実行してフォントを作り直してください: " + aMissing);
        }
    }
}
