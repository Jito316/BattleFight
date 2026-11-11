# BattleFight

Unity 6(6000.3.25f1)の 3D アクション。攻撃A・攻撃B・移動の 3 スロットのスキルを戦いながら組み替える。WebGL 版を Firebase Hosting(battlefight-proto.web.app)で公開。
このファイルは索引。中身は下の各文書にある。必要な所だけ開いて読む。

## 索引

| 知りたいこと | 文書(節) |
|---|---|
| 始め方・フォルダ・エディタのメニュー・CI・公開・遊んで確かめる用のコマンド | [README.md](README.md) 2〜10 章 |
| ゲームの仕様(スロット・入れ替え・スタイル・武器・ステージ・探索・操作・用語) | [Docs/GameDesign.md](Docs/GameDesign.md) 1〜11 章 |
| コードの書き方(名前・データを壊さない・ファイルの分け方・フォーマット) | [Docs/CodingRules.md](Docs/CodingRules.md) |
| 見た目・アニメーション・演出・音の差し替え(プレハブ・CharacterVisual・EffectLibrary・SoundLibrary) | [Docs/ArtistGuide.md](Docs/ArtistGuide.md) |
| 数値・文言(GameText)・操作の割り当て(.inputactions)・データの戻し方 | [Docs/DesignerGuide.md](Docs/DesignerGuide.md) |

## いつも守ること

- 返事は日本語。コミットのメッセージは英語、プルリクエストの説明は日本語。
- コードは [CodingRules](Docs/CodingRules.md) に合わせる(`m`・`a`・`i` の接頭辞、1 ファイル 1 クラス、名前を変えたら `[FormerlySerializedAs]`)。
- チームで作れる形を保つ: 見た目・演出・音・キー・画面の文言は、コードに書かず差し替え口(プレハブ・差し替え表・.inputactions・GameText)を通す。ビルダーは足りない物を作るだけで、あるプレハブ・データは上書きしない。シーンはコードから作り直さない。
- 新しい挙動にはテストを足す。確かめは `powershell -ExecutionPolicy Bypass -File Tools/check.ps1`(結果は `TestResults/`)。
- コミットの前に `UnityProject/Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` と `UnityProject/ProjectSettings/URPProjectSettings.asset` の変更を戻す(Unity が勝手に書き換える)。
- 日本語の文言を足したら `python Tools/subset_font.py` でフォントを作り直す。
- コミット・PR・マージ・公開(`firebase deploy --only hosting`)は、頼まれたときだけ。

## 読まないもの(大きい・自動で作られる)

- `UnityProject/Assets/_Project/Scenes/Prototype_Arena.unity`(約 500KB)は開かない。確かめは `BattleFight/Validate Scene`(`SceneValidator`)か grep。
- `UnityProject/Assets/_Project/UI/NotoSansJP-Bold.asset`(約 600KB)・`Tools/Fonts/*.otf` はフォントを作り直したときに変わるだけ。差分を見ない。
- Unity のログ(`TestResults/*.log`)は長い。`Tools/check.ps1` の最後の数行(通過数)を見て、落ちたときだけ `TestResults/*.xml` の失敗した test-case を読む。
- `UnityProject/Library`・`Temp`・`Logs`・`Builds` は Unity が作る物(Git の外)。
