# BattleFight コーディングルール

> ステータス: v1.0 / 最終更新: 2026-10-23

C#(`UnityProject/Assets/_Project` 以下のすべて。ランタイム・エディタ拡張・テストを含む)に適用する。

## 1. 変数の名前

| 種類 | 書き方 | 例 |
|---|---|---|
| 外部に公開する変数(`public` / `internal` / `protected` のフィールドとプロパティ) | 先頭を大文字(PascalCase) | `public float MaxHealth;` `public bool IsDead { get; }` |
| それ以外のメンバー変数(`private` のフィールド。`[SerializeField]`・`static`・`readonly`・`const` も含む) | `m` + PascalCase | `float mHealth;` `[SerializeField] float mMoveSpeed;` `const int mMaxChain = 5;` `static int mTokensInUse;` |
| ローカル変数(`foreach` の変数・`out var` で宣言する変数・ループのカウンタも含む) | `a` + PascalCase | `float aDamage = 0f;` `for (int aIndex = 0; ...)` `foreach (var aEnemy in ...)` |
| 引数(読むだけ) | `i` + PascalCase | `void ApplyHit(HitInfo iHit)` |
| 引数(`out`。書くだけ) | `o` + PascalCase | `bool TryLoad(out Loadout oLoadout)` |
| 引数(`ref`。読んで書く) | `io` + PascalCase | `void Clamp(ref float ioValue)` |

- **例外は作らない**
  - ラムダの引数も引数として扱う: `aEnemies.Find(iEnemy => iEnemy.IsDead)`
  - Unity のコールバックの引数にも付ける: `void OnTriggerEnter(Collider iOther)`
  - 言語の都合で名前を変えられないものだけは、そのままにする: プロパティの `set` の `value`、捨てる変数 `_`
- 外部に公開する `static` / `const` は、公開する変数と同じく先頭を大文字: `public const float MinWindup = 0.45f;`
- 単語の区切りは大文字で始める。略語も1語として扱う: `mHudRoot`(`mHUDRoot` にしない)
- 1〜2文字の名前は使わない。中身が分かる名前にする
  - ループのカウンタ: 回している配列・リストの名前 + `Index`(`aPresetIndex`)。決まらなければ `aIndex`、入れ子は `aInnerIndex`
  - 型から付ける: `SkillData` → `iSkill`、`SerializedObject` → `iSerialized`、`float dt` → `aDeltaTime`
  - 例外: 単語そのものは使ってよい(`aUp`・`iOk`・`iOn`)。`Id`、`From` と組の `To` もよい

## 2. 変数以外の名前

変数のルールは変えず、今のコードの書き方に合わせる。

| 種類 | 書き方 | 例 |
|---|---|---|
| 型(クラス・構造体・列挙型・インターフェイス) | PascalCase(インターフェイスは `I` + PascalCase) | `EnemyController` `IDamageHandler` |
| メソッド(公開・非公開とも) | PascalCase | `ResolveHits()` `TickChase()` |
| イベント | PascalCase | `public event Action Died;` |
| 列挙型の値 | PascalCase | `SlotType.AttackA` |

## 3. 既存のデータを壊さない

フィールドの名前を変えると、Unity が保存しているデータ(シーン・`.asset`・ブラウザに保存した JSON)が読めなくなる。

- シリアライズされるフィールド(`public` のフィールド、`[SerializeField]`、`[Serializable]` なクラスのフィールド)の名前を変えるときは、`[FormerlySerializedAs("前の名前")]` を付けて前の値を引き継ぐ
- JSON で保存しているデータ(編成・探索の進み具合)も同じ。`JsonUtility` も `FormerlySerializedAs` を見る
- `FormerlySerializedAs` は、保存されたデータがすべて新しい名前で書き直されたあと(アセットを保存し直し、十分な期間がたったあと)に外してよい

## 4. Inspector での見え方

`[SerializeField] float mMoveSpeed;` は、Unity のままだと Inspector に「M Move Speed」と出る(Unity は `m_` は省くが `m` は省かない)。
`Scripts/Editor/PrefixlessInspector.cs` が、専用のエディタを持たないすべてのコンポーネントと ScriptableObject で、先頭の `m` を外して「Move Speed」と出す。

- 専用のエディタ(`[CustomEditor]`)を書くときは、ラベルに `PrefixlessInspector.Label(名前)` を使う
- `[Serializable]` なクラスの中の `m○○` は、Unity の表示(「M ○○」)のまま

## 5. ファイル・クラスの分け方

- 1ファイルに1クラス。ファイル名はクラス名と同じにする
  - そのクラスだけが使う小さな型(`[Serializable]` なデータ、`enum`、テスト用の差し替え)は、同じファイルに置いてよい
- 大きくなったクラスは `partial` で役割ごとに分ける。ファイル名は `クラス名.役割.cs`(`PrototypeSceneBuilder.Character.cs`)
- 置き場所(アセンブリ)
  - ゲーム本体: `Scripts/Runtime`(`BattleFight.Runtime`)。Unity に依存しない計算は `Scripts/Runtime/Logic` に置き、EditMode テストで確かめる
  - エディタ拡張: `Scripts/Editor`(`BattleFight.Editor`)
  - テスト: `Tests/EditMode`・`Tests/PlayMode`
- 外から持ってきたアセットは `ThirdParty/提供元/` に置き、ライセンスを `LICENSE.txt` に書く

## 6. フォーマット

- インデントはスペース4つ。タブは使わない
- 波かっこは改行してから書く(Allman)。中身が1文だけの `if` は、1行に収まるなら波かっこなしで同じ行に書いてよい
- 1行はだいたい 140 文字まで
- `var` は右辺から型が分かるとき(`new`・キャスト・`GetComponent<T>()` など)に使う。分かりにくいときは型を書く
- `using` はファイルの先頭。`System` を先に、ほかはアルファベット順
- 書き方を変える PR では、フォーマットだけの変更を中身の変更と混ぜない

## 7. ルールを守る仕組み

- リポジトリ直下の `.editorconfig` に名前のルールを書き、IDE(Visual Studio / Rider)で違反を警告にする
  - 引数の `i` / `o` / `io` は、`.editorconfig` では `ref`・`out` を見分けられないのでチェックしない。レビューで確かめる
- Claude Code で書くときは、このファイルに従う

## 8. 決めたこと・まだ決めていないこと

- [x] Inspector の表示名: 先頭の `m` を外して出す(4章)
- [x] `private` のプロパティは `m` + PascalCase(変数と同じ扱い)
- [x] 1〜2文字の名前は使わない(1章)
- [x] ファイル・クラスの分け方(5章)とフォーマット(6章)
- [ ] コメントの書き方(今は、日本語で、公開するものと分かりにくいところに `/// <summary>` を付けている)
- [ ] テストの名前の付け方(今は、英語で「対象_ふるまい_結果」、Assert のメッセージは日本語)
