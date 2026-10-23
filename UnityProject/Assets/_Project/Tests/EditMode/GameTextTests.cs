using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using BattleFight.EditorTools;

namespace BattleFight.Tests
{
    /// <summary>文言の表: コードが使うキーがすべてあるか・{0} などの数が合っているか</summary>
    public class GameTextTests
    {
        const string mTablePath = "Assets/_Project/Resources/GameText.asset";
        const string mRuntimeFolder = "Assets/_Project/Scripts/Runtime";
        /// <summary>コードの中の、文言のキーらしい文字列("hud.shards" など)</summary>
        static readonly Regex mKeyLiteral = new Regex("\"((?:common|guide|hud|result|announce|elite|explore|loadout|settings|select|control)\\.[A-Za-z0-9_.<>/]+)\"");
        static readonly Regex mPlaceholder = new Regex("\\{(\\d+)(?::[^}]*)?\\}");

        static TextTable LoadTable()
        {
            var aTable = AssetDatabase.LoadAssetAtPath<TextTable>(mTablePath);
            Assert.NotNull(aTable, "文言の表(Resources/GameText.asset)がない");
            return aTable;
        }

        [Test]
        public void EveryKeyTheCodeUses_IsInTheTable()
        {
            var aTable = LoadTable();
            var aMissing = new List<string>();
            foreach (var aPath in Directory.GetFiles(mRuntimeFolder, "*.cs", SearchOption.AllDirectories))
            {
                foreach (Match aMatch in mKeyLiteral.Matches(File.ReadAllText(aPath)))
                {
                    string aKey = aMatch.Groups[1].Value;
                    // 番号で続く文言の頭(GameText.Lines("guide.tip") → guide.tip.1, guide.tip.2 …)
                    if (aTable.TryGet(aKey + ".1", out _)) continue;
                    if (!aTable.TryGet(aKey, out _)) aMissing.Add($"{Path.GetFileName(aPath)}: {aKey}");
                }
            }
            // コードで組み立てるキー
            foreach (var aDevice in new[] { "keyboard", "gamepad" })
            {
                foreach (var aPrefix in new[] { "guide.title", "guide.close", "guide.hint", "guide.swap.note" })
                {
                    if (!aTable.TryGet($"{aPrefix}.{aDevice}", out _)) aMissing.Add($"{aPrefix}.{aDevice}");
                }
            }
            foreach (SkillBehavior aBehavior in Enum.GetValues(typeof(SkillBehavior)))
            {
                if (!aTable.TryGet($"loadout.behavior.{aBehavior}", out _)) aMissing.Add($"loadout.behavior.{aBehavior}");
            }
            Assert.IsEmpty(aMissing, "文言の表にないキー:\n" + string.Join("\n", aMissing));
        }

        [Test]
        public void Table_HasEveryDefaultKey()
        {
            var aTable = LoadTable();
            var aMissing = GameTextDefaults.Entries.Where(iEntry => !aTable.TryGet(iEntry.Key, out _)).Select(iEntry => iEntry.Key).ToArray();
            Assert.IsEmpty(aMissing, "文言の表にないキー(Update Data And Prefabs で足される): " + string.Join(", ", aMissing));
        }

        [Test]
        public void EditedTexts_KeepTheSamePlaceholders()
        {
            // {0} などを消したり増やしたりすると、数や名前が出なくなる(増やすと表示が崩れる)
            var aTable = LoadTable();
            var aBroken = new List<string>();
            foreach (var (aKey, aDefault) in GameTextDefaults.Entries)
            {
                if (!aTable.TryGet(aKey, out var aText)) continue;
                var aExpected = new SortedSet<string>(mPlaceholder.Matches(aDefault).Cast<Match>().Select(iMatch => iMatch.Groups[1].Value));
                var aActual = new SortedSet<string>(mPlaceholder.Matches(aText).Cast<Match>().Select(iMatch => iMatch.Groups[1].Value));
                if (!aExpected.SetEquals(aActual)) aBroken.Add($"{aKey}: {{{string.Join("}{", aExpected)}}} がいるのに「{aText}」");
            }
            Assert.IsEmpty(aBroken, "{0} などの数が合わない文言:\n" + string.Join("\n", aBroken));
        }

        [Test]
        public void ControlNames_ComeFromTheTable()
        {
            GameText.Table = LoadTable();
            Assert.AreEqual("左クリック", InputLabels.ControlName("<Mouse>/leftButton"));
            Assert.AreEqual("J", InputLabels.ControlName("<Keyboard>/j"), "表にないキーは Input System の呼び方になる");
            var aDefault = PlayerInputBindings.CreateDefault();
            Assert.AreEqual("左クリック・J", InputLabels.For(aDefault, false, nameof(PlayerAction.AttackA)));
            Assert.AreEqual("□", InputLabels.For(aDefault, true, nameof(PlayerAction.AttackA)));
            Assert.AreEqual("W・S・A・D", InputLabels.For(aDefault, false, PlayerInputBindings.MoveAction));
            UnityEngine.Object.DestroyImmediate(aDefault);
            GameText.Table = null;
        }
    }
}
