---
paths:
  - "**/*.cs"
  - "**/*.razor"
---

# C#コーディング規約

## セクション分割に #region を使う

C# ファイル内でメソッド群をセクションに分ける場合は `// ===` や `// ---` などのコメント区切りではなく `#region` / `#endregion` を使う。

```csharp
#region 個人休日

/// <summary>個人休日追加モーダルを開く。</summary>
private void OpenAddHoliday() { ... }

#endregion
```

## Disabled制御用フラグの命名

非同期処理中にボタンを無効化するbool変数は `_disable<用途>` とする（例: `_disableCreate`, `_disableAdd`, `_disableImport`）。
同一ファイル内に複数ある場合は、操作で区別できるときは操作名（`_disableAdd` / `_disableEdit`）、
対象で区別できるときは対象名（`_disableSaveName` / `_disableSaveHoliday`）で修飾する。

## XMLドキュメントコメント

C# のクラス・メソッド・プロパティには必ず `///` コメントを付ける。
引数がある場合は `<param>`、戻り値がある場合は `<returns>` を必ず書く。補足説明が必要な場合は `<remarks>` を追加する。

```csharp
/// <summary>担当者の個人休日一覧を取得する。</summary>
/// <param name="assigneeId">担当者ID。</param>
/// <returns>個人休日の一覧。</returns>
Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId);
```

インターフェースを実装するメソッドでも `<inheritdoc/>` は使わず、`summary` / `param` / `returns` / `remarks` を実装側にも書く（可読性のため、コメントを読むためだけにインターフェース定義へ飛ぶ必要をなくす）。
