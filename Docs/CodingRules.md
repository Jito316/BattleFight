# BattleFight コーディングルール

> ステータス: 案 v0.1 / 最終更新: 2026-10-22

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

`[SerializeField] float mMoveSpeed;` は、Inspector では「M Move Speed」と出る(Unity は `m_` は省くが `m` は省かない)。必要なら `[InspectorName]` などで表示名を付ける(未決事項)。

## 5. ルールを守る仕組み

- リポジトリ直下の `.editorconfig` に名前のルールを書き、IDE(Visual Studio / Rider)で違反を警告にする
  - 引数の `i` / `o` / `io` は、`.editorconfig` では `ref`・`out` を見分けられないのでチェックしない。レビューで確かめる
- Claude Code で書くときは、このファイルに従う

## 6. 未決事項

- [ ] Inspector の表示名(「M Move Speed」のままでよいか)
- [x] `private` のプロパティは `m` + PascalCase(変数と同じ扱い)
- [ ] 1文字の名前(`i`・`s` など)は `aI`・`iS` になった。意味のある名前に付け直すか
- [ ] コメント・テストの名前の付け方など、名前以外のルール
