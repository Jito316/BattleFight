using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace BattleFight
{
    /// <summary>
    /// 操作の割り当て(.inputactions)から、画面に出すキーの名前を作る(「左クリック・J」など)。
    /// 割り当てを変えると、操作説明やボタンの表示もそれに合わせて変わる。
    /// キーの呼び方は文言の表の「control.〈操作の場所〉」(例: control.&lt;Mouse&gt;/leftButton → 左クリック)。表になければ Input System の呼び方。
    /// </summary>
    public static class InputLabels
    {
        /// <summary>区切り(操作説明では、この区切りごとに枠を分けて出す)</summary>
        public const string Separator = "・";

        static readonly StringBuilder mBuilder = new StringBuilder();
        static readonly List<string> mSeen = new List<string>();

        /// <summary>iActions の各アクションに、キーボード・マウス(iGamepad なら ゲームパッド)で割り当てたキーの名前を「・」でつなぐ</summary>
        public static string For(InputActionAsset iAsset, bool iGamepad, params string[] iActions)
        {
            mBuilder.Clear();
            mSeen.Clear();
            if (iAsset == null) return "";
            string aScheme = iGamepad ? PlayerInputBindings.GamepadScheme : PlayerInputBindings.KeyboardMouseScheme;
            foreach (var aName in iActions)
            {
                var aAction = iAsset.FindAction($"{PlayerInputBindings.MapName}/{aName}");
                if (aAction == null) continue;
                foreach (var aBinding in aAction.bindings)
                {
                    if (aBinding.isComposite || !InScheme(aBinding, aScheme)) continue;
                    string aLabel = ControlName(aBinding.effectivePath);
                    if (aLabel.Length == 0 || mSeen.Contains(aLabel)) continue;
                    mSeen.Add(aLabel);
                    if (mBuilder.Length > 0) mBuilder.Append(Separator);
                    mBuilder.Append(aLabel);
                }
            }
            return mBuilder.ToString();
        }

        /// <summary>いちばん初めに割り当てたキーの名前だけ(「[F] フィニッシャー」のような短い表示)</summary>
        public static string First(InputActionAsset iAsset, bool iGamepad, string iAction)
        {
            string aAll = For(iAsset, iGamepad, iAction);
            int aCut = aAll.IndexOf(Separator, System.StringComparison.Ordinal);
            return aCut >= 0 ? aAll.Substring(0, aCut) : aAll;
        }

        /// <summary>操作の場所の呼び方(文言の表 → Input System の呼び方の順)</summary>
        public static string ControlName(string iPath)
        {
            if (string.IsNullOrEmpty(iPath)) return "";
            string aKey = "control." + iPath;
            if (GameText.Has(aKey)) return GameText.Get(aKey);
            return InputControlPath.ToHumanReadableString(iPath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        /// <summary>割り当てがこの操作の組(キーボード・マウス / ゲームパッド)のものか。組がないものは、場所から決める</summary>
        static bool InScheme(InputBinding iBinding, string iScheme)
        {
            if (!string.IsNullOrEmpty(iBinding.groups)) return iBinding.groups.Contains(iScheme);
            bool aGamepad = iBinding.effectivePath != null && iBinding.effectivePath.StartsWith("<Gamepad>");
            return aGamepad == (iScheme == PlayerInputBindings.GamepadScheme);
        }
    }
}
