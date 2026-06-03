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

## 技術スタック

| 役割 | 採用技術 |
|------|----------|
| UIフレームワーク | .NET MAUI Blazor Hybrid (.NET 10) |
| UIコンポーネント | Razor コンポーネント（C#） |
| ORM | Entity Framework Core |
| データストア | SQLite3（EF Core経由） |
| D&D並び替え | SortableJS（JS Interop経由） |

## ディレクトリ構成（予定）

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

## アーキテクチャ

**データアクセス**: すべてEF Core経由（将来的なSQL Server/MySQL移行を考慮し、生SQLを書かない）。  
**パターン**: リポジトリパターン。`Repositories/` に各テーブル対応のリポジトリを実装し、`MauiProgram.cs` でDI登録。  
**状態管理**: NavigationManager とDIシングルトンサービスで画面間状態を管理（9-1タスク）。

### DBエンティティとリレーション

```
projects ──< assignees ──< assignee_holidays
projects ──< tasks ─┐
                     ├── (parent_id → tasks.id)  # 無制限階層
                     └── (predecessor_id → tasks.id)  # FS依存
assignees ──< tasks
global_holidays  # アプリ全体の祝日
```

**重要な制約**:
- SQLite接続時に必ず `PRAGMA foreign_keys = ON;` を実行（EF Core の `OnConfiguring` またはマイグレーションで設定）
- `tasks.status` は `CHECK (status IN ('未着手', '進行中', '完了', '保留'))` で制約あり
- `tasks.progress` は `CHECK (progress >= 0 AND progress <= 100)` で制約あり

### 親タスクの日付計算（DBに保存しない）

子タスクを持つ親タスクの `start_date` / `end_date` は **DBに保存せずNULL**。  
表示時にアプリ側で動的計算する：
- 親の開始日 = 直接の子の `start_date` の最小値
- 親の終了日 = 直接の子の `end_date` の最大値

### スケジュール再計算の伝播順序

1. 変更タスクの `predecessor_id` を持つ後続タスクを `idx_tasks_predecessor_id` で検索
2. 後続タスクの `start_date` を「前タスクの `end_date` の翌稼働日」に更新
3. `end_date` を「新 `start_date` + `work_days`（稼働日カウント）」で再計算
4. 再帰的に後続タスクへ伝播
5. 後続タスクが手動で開始日を変更済みでも**再計算で上書きする**

### 稼働日判定の優先順序

1. 土曜・日曜 → 休日（固定、DBに登録しない）
2. `global_holidays.date` に存在 → 休日
3. 対象担当者の `assignee_holidays.date` に存在 → 休日
4. それ以外 → 稼働日

## 画面一覧

| 画面ID | 画面名 | コンポーネント（予定） |
|--------|--------|----------------------|
| S01 | プロジェクト一覧 | `Components/Pages/ProjectList.razor` |
| S02 | ガントチャート | `Components/Pages/GanttChart.razor` |
| S03 | タスク編集モーダル | `Components/Modals/TaskEditModal.razor` |
| S04 | 休日設定 | `Components/Pages/HolidaySettings.razor` |
| S05 | 担当者一覧 | `Components/Pages/AssigneeList.razor` |
| S06 | 担当者詳細 | `Components/Pages/AssigneeDetail.razor` |
| S07 | プロジェクト作成モーダル | `Components/Modals/ProjectCreateModal.razor` |

## 開発進捗

進捗は [doc/checklist.md](doc/checklist.md) を参照。  
各タスクの詳細は [doc/task/](doc/task/) 以下のファイルに記載。  
全タスク未着手（1-1から順に実装する）。

## コーディング規約

画面ID（S01, S02 等）は設計書内の整理用であり、コードには持ち込まない。

- CSSクラス名に使わない（`.s01-header` → `.page-header` のように役割を表す名前にする）
- C#クラス名・変数名・フィールド名に使わない（`s01State` や `_s02Data` などは不可）
- ボタンラベル・画面テキスト等に表示しない

## 重要設計ドキュメント

- [doc/requirements.md](doc/requirements.md) — 機能要件・非機能要件
- [doc/db_design.md](doc/db_design.md) — テーブル定義・DDL・ER図
- [doc/ui_design.md](doc/ui_design.md) — 画面レイアウト・画面遷移・共通ルール
- [doc/task.md](doc/task.md) — 大タスク一覧と実装順序
