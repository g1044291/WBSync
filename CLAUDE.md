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

子タスクを持つ親タスクの `start_date` / `end_date` はDBに保存せずNULL。表示時に子タスクから動的計算する。詳細: `doc/tables/tasks.md`

### スケジュール再計算

**後続タスクが手動で開始日を変更済みであっても、再計算で上書きする。**  
詳細: `doc/requirements/schedule-calculation.md`, `doc/tables/tasks.md`

### 稼働日判定の優先順序

稼働日判定の優先順序は `doc/requirements/holiday-settings.md` を参照。

## コーディング規約

C#コーディング規約は `.claude/rules/csharp-style.md` を参照。

## ツール使用

- シェルコマンドは `&&` / `||` / `;` で連結せず、1コマンドずつ個別のツール呼び出しで実行する
- `|`（パイプ）と `>`（リダイレクト）は単一コマンド内の使用はOK（例: `git log | head -10`）

## タスク管理

タスク・バックログは **GitHub Issues** で管理する。

## ブランチ・コミット・PR

ブランチ作成・コミット・PR作成時は [CONTRIBUTING.md](CONTRIBUTING.md) を参照。

## 設計ドキュメント

[doc/index.md](doc/index.md) を起点とする。[Open Knowledge Format](https://cloud.google.com/blog/ja/products/data-analytics/how-the-open-knowledge-format-can-improve-data-sharing/) を参考に、YAMLフロントマター付きMarkdownファイルの集合として構成する（1ファイル1トピック、相互リンクで関連付け）。

| ディレクトリ | 内容 |
|-------------|------|
| [doc/requirements/](doc/requirements/index.md) | 機能要件・非機能要件 |
| [doc/tables/](doc/tables/index.md) | テーブル定義・DDL・ER図 |
| [doc/screens/](doc/screens/index.md) | 画面レイアウト・画面遷移・共通ルール |

**タスク完了時に必ず更新**: `doc/requirements/`（仕様変更時）、`doc/screens/`（画面変更時）、`doc/tables/`（テーブル変更時）。新しいテーブル・画面・要件項目を追加する場合は、既存ファイルに倣いYAMLフロントマター（`type` / `title` / `description` / `tags`、必要に応じ実装ファイルへの`resource`）を付与し、関連ファイルへの相互リンクを追加すること。
