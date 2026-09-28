"""
HUD 用の日本語フォントを、ゲームで使う文字だけに絞る(WebGL のダウンロード量を減らすため)。

使い方(リポジトリのルートで):
    python -m pip install fonttools
    python Tools/subset_font.py

・UnityProject/Assets 以下の .cs と .asset(Unity の YAML は \\uXXXX で書かれる)から文字を集める
・ひらがな・カタカナ全部と、よく使う記号も入れておく(あとで文字を足しても表示が崩れにくいように)
・スキル名や説明に新しい漢字を足したら、このスクリプトを実行し直すこと
"""
import pathlib
import re
import sys

from fontTools import subset

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE_FONT = ROOT / "Tools" / "Fonts" / "NotoSansJP-Bold.otf"
OUTPUT_FONT = ROOT / "UnityProject" / "Assets" / "_Project" / "Fonts" / "NotoSansJP-Bold-Subset.otf"
SCAN_ROOTS = [ROOT / "UnityProject" / "Assets" / "_Project"]

ESCAPE = re.compile(r"\\u([0-9A-Fa-f]{4})")


def base_characters() -> set[str]:
    chars = set(chr(c) for c in range(0x20, 0x7F))              # ASCII
    chars |= set(chr(c) for c in range(0x3041, 0x3097))          # ひらがな
    chars |= set(chr(c) for c in range(0x30A1, 0x30FB))          # カタカナ
    chars |= set(chr(c) for c in range(0xFF01, 0xFF5F))          # 全角英数・記号
    chars |= set("ー・、。「」『』【】（）〜…※◎○●◇◆□■△▲▽▼☆★→←↑↓⇒⇄×÷±＋－％　")
    return chars


def collect_characters() -> set[str]:
    chars = base_characters()
    for scan_root in SCAN_ROOTS:
        for path in scan_root.rglob("*"):
            if path.suffix not in (".cs", ".asset"):
                continue
            text = path.read_text(encoding="utf-8", errors="ignore")
            chars |= set(text)
            chars |= {chr(int(code, 16)) for code in ESCAPE.findall(text)}
    # 改行などの制御文字はいらない
    return {c for c in chars if ord(c) >= 0x20}


def main() -> int:
    if not SOURCE_FONT.exists():
        print(f"元のフォントがありません: {SOURCE_FONT}", file=sys.stderr)
        return 1

    chars = collect_characters()
    options = subset.Options()
    options.layout_features = ["*"]
    options.name_IDs = ["*"]
    options.notdef_outline = True
    options.desubroutinize = False

    font = subset.load_font(str(SOURCE_FONT), options)
    subsetter = subset.Subsetter(options)
    subsetter.populate(text="".join(sorted(chars)))
    subsetter.subset(font)
    OUTPUT_FONT.parent.mkdir(parents=True, exist_ok=True)
    subset.save_font(font, str(OUTPUT_FONT), options)

    before = SOURCE_FONT.stat().st_size / 1024
    after = OUTPUT_FONT.stat().st_size / 1024
    print(f"{len(chars)} 文字 / {before:,.0f} KB -> {after:,.0f} KB : {OUTPUT_FONT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
