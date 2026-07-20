# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

WBSync は工数（人日）ベースでスケジュールを自動生成するWBSデスクトップアプリ。  
**単一ユーザー、Windows専用、〜100タスク規模**を前提とした軽量ツール。

## ビルド・実行

```powershell
# Windowsターゲットでビルド
dotnet build src/WBSync/WBSync.csproj -f net10.0-windows10.0.19041.0

# デバッグ実行
dotnet run --project src/WBSync/WBSync.csproj -f net10.0-windows10.0.19041.0

# EF Core マイグレーション追加
dotnet ef migrations add <MigrationName> --project src/WBSync

# マイグレーション適用（手動）
dotnet ef database update --project src/WBSync
```

> アプリ起動時に `MigrateAsync()` を呼んでマイグレーションを自動適用する設計。

## ディレクトリ構成

```
src/WBSync/
├── Components/        # Razor コンポーネント（画面・共通部品）
├── Data/              # AppDbContext
├── Models/            # EF Core エンティティクラス、および画面用ViewModel（DB非対応の表示専用モデル）
├── Repositories/      # リポジトリパターンのデータアクセス層
├── Services/          # ビジネスロジック（スケジュール計算等）
├── Platforms/Windows/ # Windows固有エントリーポイント
└── MauiProgram.cs     # DIコンテナ設定・アプリ起動
```

**名前空間**: フォルダに対応した階層（`WBSync.Models`, `WBSync.Data`, `WBSync.Repositories`, `WBSync.Services`）。  
**エンティティのクラス名**: `System.Threading.Tasks.Task` との衝突を避けるため、タスクエンティティは `WbsTask` とする。  
**Models内のViewModel**: DBに対応しない画面専用モデル（例: `TaskNode`, `StatusMessage`）は `internal` にし、EF Coreエンティティ（`public`）と区別する。

## アーキテクチャの要点

- DBアクセスはすべてEF Core経由（生SQLなし）
- SQLite接続時は必ず `PRAGMA foreign_keys = ON;` を実行（EF Core の `OnConfiguring` またはマイグレーションで設定）
- D&D並び替えは SortableJS を JS Interop 経由で使用

### 親タスクの日付（DBに保存しない）

子タスクを持つ親タスクの `start_date` / `end_date` は **DBにNULL**。表示時に動的計算する：
- 親の開始日 = 子の `start_date` の最小値
- 親の終了日 = 子の `end_date` の最大値

### スケジュール再計算

前タスクの終了日変更 → 後続タスクの開始日を「翌稼働日」に更新 → `work_days` から終了日を再計算 → 再帰伝播。  
**後続タスクが手動で開始日を変更済みであっても、再計算で上書きする。**

### 稼働日判定の優先順序

1. 土曜・日曜 → 休日
2. `global_holidays.date` に存在 → 休日
3. 対象担当者の `assignee_holidays.date` に存在 → 休日
4. それ以外 → 稼働日

## コーディング規約

### 画面IDはコードに持ち込まない

設計書の画面ID（S01, S02 等）はコードに持ち込まない。CSSクラス名・C#クラス名・変数名・UIテキストに使わない。

### セクション分割に #region を使う

C# ファイル内でメソッド群をセクションに分ける場合は `// ===` や `// ---` などのコメント区切りではなく `#region` / `#endregion` を使う。

```csharp
#region 個人休日

/// <summary>個人休日追加モーダルを開く。</summary>
private void OpenAddHoliday() { ... }

#endregion
```

### Disabled制御用フラグの命名

非同期処理中にボタンを無効化するbool変数は `_disable<用途>` とする（例: `_disableCreate`, `_disableAdd`, `_disableImport`）。
同一ファイル内に複数ある場合は、操作で区別できるときは操作名（`_disableAdd` / `_disableEdit`）、
対象で区別できるときは対象名（`_disableSaveName` / `_disableSaveHoliday`）で修飾する。

### XMLドキュメントコメント

C# のクラス・メソッド・プロパティには必ず `///` コメントを付ける。

```csharp
/// <summary>担当者の個人休日一覧を取得する。</summary>
/// <param name="assigneeId">担当者ID。</param>
Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId);
```

## ツール使用

- シェルコマンドは `&&` / `||` / `;` で連結せず、1コマンドずつ個別のツール呼び出しで実行する
- `|`（パイプ）と `>`（リダイレクト）は単一コマンド内の使用はOK（例: `git log | head -10`）

## タスク管理

タスク・バックログは **GitHub Issues** で管理する。

## ブランチルール

| ブランチ | 用途 |
|----------|------|
| `main` | 本番ブランチ。直接pushしない。PRマージのみ。 |
| `feature/<description>` | 機能追加 |
| `fix/<description>` | バグ修正 |
| `chore/<description>` | リファクタリング・設定変更・ドキュメント更新 |

- `<description>` は英語のスネークケース（例: `feature/add_coefficient`）
- 作業はissueに対応するブランチを切り、PRで `main` にマージする

## コミット・PRルール

### 言語

- **コミットメッセージ・PRタイトル・PR本文はすべて日本語**で書く
- ブランチ名とプレフィックスは英語

### コミットメッセージ形式

issueに対応する作業は先頭にissue番号を付ける。

```
#42 タスク編集に予定工数欄を追加
```

issueに紐づかない作業（リファクタリング等）はプレフィックスを付ける。

```
fix: スケジュール再計算で終了日がずれるバグを修正
chore: 不要なusingを削除
```

プレフィックス一覧：`feat` / `fix` / `chore` / `docs` / `refactor` / `test`

### PRルール

- タイトル：日本語の説明のみ（PR番号はGitHubが自動採番するため不要）
- 本文：変更概要・動作確認内容を日本語で記載
- マージ前にCIが通っていること

**依存関係がある場合：** 依存元のissue番号を依存先のPRタイトルまたは本文に記載する。

```
# 例：このPRがissue #45に依存している場合
タイトル：#45 スケジュール再計算の修正
```

## 設計ドキュメント

| ドキュメント | 内容 |
|-------------|------|
| [doc/requirements.md](doc/requirements.md) | 機能要件・非機能要件 |
| [doc/db_design.md](doc/db_design.md) | テーブル定義・DDL・ER図 |
| [doc/ui_design.md](doc/ui_design.md) | 画面レイアウト・画面遷移・共通ルール |

**タスク完了時に必ず更新**: requirements.md（仕様変更時）、ui_design.md（画面変更時）
