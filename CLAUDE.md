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
├── Models/            # EF Core エンティティクラス
├── Repositories/      # リポジトリパターンのデータアクセス層
├── Services/          # ビジネスロジック（スケジュール計算等）
├── Platforms/Windows/ # Windows固有エントリーポイント
└── MauiProgram.cs     # DIコンテナ設定・アプリ起動
```

**名前空間**: フォルダに対応した階層（`WBSync.Models`, `WBSync.Data`, `WBSync.Repositories`, `WBSync.Services`）。  
**エンティティのクラス名**: `System.Threading.Tasks.Task` との衝突を避けるため、タスクエンティティは `WbsTask` とする。

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

### XMLドキュメントコメント

C# のクラス・メソッド・プロパティには必ず `///` コメントを付ける。

```csharp
/// <summary>担当者の個人休日一覧を取得する。</summary>
/// <param name="assigneeId">担当者ID。</param>
Task<List<AssigneeHoliday>> GetByAssigneeAsync(int assigneeId);
```

## 設計ドキュメント

| ドキュメント | 内容 |
|-------------|------|
| [doc/requirements.md](doc/requirements.md) | 機能要件・非機能要件 |
| [doc/db_design.md](doc/db_design.md) | テーブル定義・DDL・ER図 |
| [doc/ui_design.md](doc/ui_design.md) | 画面レイアウト・画面遷移・共通ルール |
| [doc/task.md](doc/task.md) | 追加機能メモ |

**タスク完了時に必ず更新**: requirements.md（仕様変更時）、ui_design.md（画面変更時）
