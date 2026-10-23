# BattleFight 数値・文言・操作の直し方(プランナー向け)

> ステータス: v1.0 / 最終更新: 2026-10-24

ゲームの数値・画面の文言・操作の割り当ては、コードではなくデータで決める。Unity の Inspector で直せる。
見た目(モデル・演出・音)の差し替えは [ArtistGuide.md](ArtistGuide.md) を見る。

## 1. どこに何があるか

| 直したいもの | 場所 | メモ |
|---|---|---|
| スキル(40 個) | `Assets/_Project/Data/Skills/Skill_*.asset` | 段ごとの発生・持続・硬直(60fps のフレーム)、威力・ひるみ・アーマー削り、前進、無敵、弾・ビームなど |
| 武器種(6 種) | `Assets/_Project/Data/Weapons/Weapon_*.asset` | 名前・色、マスタリーのフィニッシャー、シナジーのひるみボーナス |
| 敵(15 種) | `Assets/_Project/Data/Enemies/Enemy_*.asset` | 体力・アーマー・速さ・攻撃の一覧(予備動作・範囲・威力・逃げるしかない攻撃か)・弱点・型(写し身) |
| ステージ | `Assets/_Project/Data/Stages/Stage_*.asset` | ウェーブごとの敵、エンドレスの出現表、見た目(床・空・光・霧の色) |
| スタイルランク | `Assets/_Project/Data/StyleRankConfig.asset` | ランクの名前・必要な点数・攻撃力の強化・ボーナスの点数 |
| 編成画面に出すスキル | `Assets/_Project/Data/SkillDatabase.asset` | 自動で作り直す(手で直さない) |
| 画面の文言 | `Assets/_Project/Resources/GameText.asset` | 3 章 |
| 操作の割り当て | `Assets/_Project/Input/BattleFight.inputactions` | 4 章 |

- データを直したら保存(Ctrl+S)し、`Tools/check.ps1` か Unity の Test Runner でテストを回す。
- 名前に「Mirror」「Dummy」が付く敵は、写し身(第5幕)と訓練場の人形。

## 2. 数値を直す

- Inspector で値を書き換えて保存するだけでよい。コードを直す必要はない。
- `BattleFight/Update Data And Prefabs` を実行しても、**直した値は消えない**(ないデータを作るだけ)。
- たたき台の値に戻したいときは、戻したいデータを Project ウィンドウで選び、右クリック → **BattleFight → Reset To Defaults**(またはメニューの `BattleFight/Reset Selected Data To Defaults`)。選んだデータだけが戻る。
- `BattleFight/Reset All Data To Defaults` は、すべてのデータを戻す(2 回確かめる)。チームで直した値も消えるので、ふだんは使わない。
- 戻してしまったら、Git で戻す(コミットしていない変更は戻せないので、こまめにコミットする)。

### テストに注意

ゲームの手ごたえ(当たりの重さ・逃げ切れる大きさなど)は、PlayMode のテストで確かめている。
数値を大きく変えるとテストが落ちることがある。落ちたら、テストの名前とメッセージ(日本語)を見て、意図した変更ならプログラマーに相談してテストを直す。

## 3. 画面の文言

画面に出す文言は、文言の表 `Assets/_Project/Resources/GameText.asset` にまとまっている。
キー(`hud.shards` など)ごとに文言があり、コードはキーで引く。文言を直せば画面も変わる。

| 書き方 | 意味 |
|---|---|
| `{0}` `{1}` | コードが数や名前を入れる所。**消したり増やしたりしない**(テストで確かめる) |
| `{0:0.0}` | 数の書き方(小数 1 桁など) |
| `<color=#FFE066>…</color>` `<size=26>…</size>` | 色・大きさの指定 |
| `\n` | 改行(Inspector では Enter で改行してよい) |

- `guide.tip.1`、`guide.tip.2` … のように番号で続くもの(操作説明のコツ)は、番号を続けて足せば増える。
- `control.<Mouse>/leftButton` などは、操作説明に出すキーの呼び方。
- コードで新しいキーを使い始めると、`Update Data And Prefabs` で表に足される(すでにある文言には手を付けない)。
- 表にないキーは、画面に `[キー]` と出る。
- スキル・敵・ステージの名前と説明は、それぞれのデータ(1 章)で直す。

## 4. 操作の割り当て

操作の割り当ては `Assets/_Project/Input/BattleFight.inputactions`。ダブルクリックで Input System のウィンドウが開く。

- アクションの **名前は変えない**(ゲームは名前で読む。`InputBindingsTests` で確かめる)。名前は `Move`・`StickLook`・`MouseLook` と、`AttackA`・`AttackB`・`Movement`・`Jump`・`LockOn`・`Swap1`〜`Swap4`・`Finisher`・`Restart`・`OpenLoadout`・`Drive`・`Guard`・`LockOnLeft`・`LockOnRight`。
- 割り当て(キー・ボタン)は自由に足したり替えたりしてよい。キーボード・マウスの割り当ては「Keyboard&Mouse」、ゲームパッドは「Gamepad」の組に入れる。
- 操作説明・ボタンの横のキーの表示は、割り当てから作る。割り当てを替えると表示も変わる。キーの呼び方を変えたいときは、文言の表の `control.〜` を直す。
- 次のキーは割り当ての外で決まっている(変えるときはプログラマーに相談): 操作説明の開け閉め `0` / R3、ステージ選択へ `T`、次のステージ `N`、設定 `O`・`Esc`、ミュート `M`、探索をはじめから `Backspace`。文言の表に書いてある `[N]` `[T]` などの表示も、これに合わせて手で直す。

## 5. 確かめ方

| テスト | 確かめること |
|---|---|
| `GameTextTests`(EditMode) | コードが使うキーが表にすべてあるか、`{0}` などの数が合っているか |
| `InputBindingsTests`(EditMode) | 割り当てに、ゲームが読むアクションがすべてあるか |
| `SceneValidatorTests`(EditMode) | シーンの参照が切れていないか |
| PlayMode のテスト | 遊んだときの動き(数値を変えると落ちることがある) |

遊んで確かめるときは、ブラウザ版のコンソールからステージ・ウェーブを選んで始められる(README の「遊んで確かめる用のコマンド」)。
