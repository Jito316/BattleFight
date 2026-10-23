# BattleFight

武器を持ち替え続けて、流れるように戦う 3D アクション(Unity 6)。
攻撃A・攻撃B・移動の 3 つのスロットのスキルを、戦いながら組み替えて戦う。

- 遊べる版: https://battlefight-proto.web.app
- ゲームの仕様: [Docs/GameDesign.md](Docs/GameDesign.md)

## 1. 用意するもの

| もの | バージョン | メモ |
|---|---|---|
| Unity | **6000.3.25f1** | Unity Hub で入れる。モジュールは **WebGL Build Support**(公開用)と **Windows Build Support (IL2CPP)**(Windows 版を作るとき) |
| Git | 2.40 以降 | |
| Git LFS | 3.x | 画像・モデル・音・フォントは LFS で扱う |
| Python | 3.10 以降 | 日本語フォントを作り直すときだけ(`Tools/subset_font.py`) |
| Firebase CLI | 最新 | 公開するときだけ |

## 2. 始め方

```powershell
git clone https://github.com/Jito316/BattleFight.git
cd BattleFight
powershell -ExecutionPolicy Bypass -File Tools/setup-git.ps1
```

`Tools/setup-git.ps1` は、このリポジトリだけに次の設定をする(一度でよい)。

- Git LFS を有効にする
- シーン・プレハブ・アセットのマージに、Unity のマージツール(UnityYAMLMerge)を使う

そのあと Unity Hub で `UnityProject` フォルダを開き、`Assets/_Project/Scenes/Prototype_Arena.unity` を開いて再生する。

## 3. フォルダ

```
Docs/                 仕様・ルール・ガイド
Tools/                手元で使う道具(Git の設定・まとめて確かめる・フォントを作り直す)
UnityProject/
  Assets/_Project/
    Data/             スキル・武器・敵・ステージ・演出と音の差し替え表(ScriptableObject)
    Input/            操作の割り当て(BattleFight.inputactions)
    Prefabs/          プレイヤー・敵・武器・シーンに置く物
    Resources/        画面の文言の表(GameText.asset)
    Scenes/           シーン(Prototype_Arena)
    Scripts/Runtime/  ゲームのコード
    Scripts/Editor/   エディタの道具(データ・プレハブ・シーンを用意する、ビルドする)
    Tests/            EditMode・PlayMode のテスト
    UI/               UI Toolkit(UXML・USS)
```

## 4. 役割ごとの入口

| 役割 | 触る所 | 読むもの |
|---|---|---|
| プログラマー | `Scripts/`・`Tests/` | [Docs/CodingRules.md](Docs/CodingRules.md) |
| アーティスト | `Prefabs/`・`Data/EffectLibrary.asset` | [Docs/ArtistGuide.md](Docs/ArtistGuide.md) |
| サウンド | `Data/SoundLibrary.asset` | [Docs/ArtistGuide.md](Docs/ArtistGuide.md) の「音」 |
| プランナー | `Data/`(スキル・敵・ステージの数値)・`Resources/GameText.asset`(文言)・`Input/`(操作の割り当て) | [Docs/DesignerGuide.md](Docs/DesignerGuide.md) |

## 5. エディタのメニュー(BattleFight)

| メニュー | すること |
|---|---|
| Update Data And Prefabs | 足りないデータ・プレハブだけを作る。あるものには手を付けない。シーンもさわらない(なければ作る) |
| Validate Scene | 開いているシーンが壊れていないか確かめる(スクリプトが外れた物・切れた参照・つなぎ忘れ) |
| Rebuild Scene From Prefabs | シーンをプレハブから作り直す。**シーンで手を加えた所は消える**(確認が出る) |
| Reset Selected Data To Defaults | 選んだデータだけを、たたき台の値に戻す(Project ウィンドウの右クリック → BattleFight → Reset To Defaults でも同じ) |
| Reset All Data To Defaults | すべてのデータを、たたき台の値で上書きする。**手で直した数値はすべて消える**(2 回確認が出る) |
| Build WebGL / Build Windows | ビルドする(`UnityProject/Builds/`) |

シーンは手で直してよい。置く物(アリーナ・探索マップ・プレイヤー・カメラ・ゲーム進行・UI)はそれぞれプレハブなので、
中身を直すときはプレハブを開いて直す(同じシーンを何人もさわらずに済む)。

## 6. 確かめる

```powershell
powershell -ExecutionPolicy Bypass -File Tools/check.ps1
```

データとプレハブの更新(シーンの確かめも)→ EditMode テスト → PlayMode テスト → WebGL ビルドを順に回す。
結果は `TestResults/` に出る。どれかが失敗すると終了コード 1。

| オプション | すること |
|---|---|
| `-RebuildScene` | シーンもプレハブから作り直す |
| `-SkipWebGL` | ビルドを飛ばす(テストだけ) |
| `-SkipTests` | テストを飛ばす |

Unity エディタの Window → General → Test Runner からも回せる。

## 7. CI(GitHub Actions)

プルリクエストと main への push のたびに、`.github/workflows/unity.yml` がテストと WebGL ビルドを回す([GameCI](https://game.ci/))。
使う前に、リポジトリの Settings → Secrets and variables → Actions に次を入れる。

| 名前 | 中身 |
|---|---|
| `UNITY_LICENSE` | Unity のライセンスファイル(`.ulf`)の中身。Personal なら [GameCI の手順](https://game.ci/docs/github/activation) で作る |
| `UNITY_EMAIL` | Unity ID のメールアドレス |
| `UNITY_PASSWORD` | Unity ID のパスワード |

main には、テストが通ったプルリクエストだけをマージする(Settings → Branches で「Require status checks to pass」を付ける)。

## 8. 進め方

1. main から作業用のブランチを切る(`feature/〜`)
2. 直したら `Tools/check.ps1` で確かめる
3. プルリクエストを出す(テンプレートに沿って書く)。CI が通り、レビューを受けてからマージする

コードの書き方は [Docs/CodingRules.md](Docs/CodingRules.md)。コミットのメッセージは英語、プルリクエストの説明は日本語。

## 9. 公開する(Firebase Hosting)

```powershell
firebase login
firebase deploy --only hosting
```

`UnityProject/Builds/WebGL` を battlefight-proto.web.app に上げる。先に WebGL をビルドしておく(Firebase のプロジェクトに入れてもらう必要がある)。

## 10. 遊んで確かめる用のコマンド

ブラウザで遊んでいるときに、開発者ツールのコンソールから呼べる。

```js
unityInstance.SendMessage('GameDirector', 'PlaytestCommand', '1 4')   // 第1幕のウェーブ4から始める
unityInstance.SendMessage('GameDirector', 'PlaytestCommand', 'god')   // プレイヤーが倒れなくなる
unityInstance.SendMessage('GameDirector', 'PlaytestCommand', 'room 2') // 探索で 2 番目の部屋へ移る
unityInstance.SendMessage('GameDirector', 'PlaytestCommand', 'attack 0') // いちばん近い敵に 0 番目の攻撃をさせる
```
