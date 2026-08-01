---
type: SQLite Table
title: tasks
description: タスク。階層構造（親子関係）とFS依存関係（先行・後続）を持つ。
tags: [db, task]
resource: ../../src/WBSync/Models/WbsTask.cs
---

# tasks（タスク）

| カラム名 | 型 | NULL | デフォルト | 説明 |
|----------|----|------|------------|------|
| id | INTEGER | NOT NULL | AUTOINCREMENT | PK |
| project_id | INTEGER | NOT NULL | - | FK → [projects](projects.md).id |
| parent_id | INTEGER | NULL | - | FK → tasks.id。NULL = ルートタスク |
| predecessor_id | INTEGER | NULL | - | FK → tasks.id。依存タスク（FS）。NULL = 依存なし |
| assignee_id | INTEGER | NULL | - | FK → [assignees](assignees.md).id。親タスクは NULL |
| name | TEXT | NOT NULL | - | タスク名 |
| work_days | REAL | NULL | - | 工数（人日）。親タスクは NULL |
| start_date | TEXT | NULL | - | 開始日（YYYY-MM-DD）。親タスクは NULL（後述） |
| end_date | TEXT | NULL | - | 終了日（YYYY-MM-DD）。親タスクは NULL（後述） |
| status | TEXT | NOT NULL | '未着手' | ステータス（後述のCHECK制約参照） |
| progress | INTEGER | NOT NULL | 0 | 進捗率（0〜100） |
| notes | TEXT | NULL | - | 備考 |
| sort_order | INTEGER | NOT NULL | 0 | 同一階層内での表示順 |
| created_at | TEXT | NOT NULL | DATETIME('now') | 作成日時 |
| updated_at | TEXT | NOT NULL | DATETIME('now') | 更新日時 |

## 制約

* `project_id` → `projects.id` ON DELETE CASCADE
* `parent_id` → `tasks.id` ON DELETE CASCADE（親タスク削除時に子タスクも再帰的に削除）
* `predecessor_id` → `tasks.id` ON DELETE SET NULL（依存先タスク削除時は依存関係を解除）
* `assignee_id` → `assignees.id` ON DELETE SET NULL（担当者削除時はNULLに）
* `status` CHECK: `status IN ('未着手', '進行中', '完了', '保留')`
* `progress` CHECK: `progress >= 0 AND progress <= 100`

## DDL

```sql
CREATE TABLE IF NOT EXISTS tasks (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id     INTEGER NOT NULL  REFERENCES projects(id)  ON DELETE CASCADE,
    parent_id      INTEGER           REFERENCES tasks(id)     ON DELETE CASCADE,
    predecessor_id INTEGER           REFERENCES tasks(id)     ON DELETE SET NULL,
    assignee_id    INTEGER           REFERENCES assignees(id) ON DELETE SET NULL,
    name           TEXT    NOT NULL,
    work_days      REAL,
    start_date     TEXT,
    end_date       TEXT,
    status         TEXT    NOT NULL DEFAULT '未着手'
                           CHECK (status IN ('未着手', '進行中', '完了', '保留')),
    progress       INTEGER NOT NULL DEFAULT 0
                           CHECK (progress >= 0 AND progress <= 100),
    notes          TEXT,
    sort_order     INTEGER NOT NULL DEFAULT 0,
    created_at     TEXT    NOT NULL DEFAULT (DATETIME('now', 'localtime')),
    updated_at     TEXT    NOT NULL DEFAULT (DATETIME('now', 'localtime'))
);
```

## インデックス

| インデックス名 | カラム | 用途 |
|---------------|--------|------|
| idx_tasks_project_id | project_id | プロジェクト別タスク取得 |
| idx_tasks_parent_id | parent_id | 子タスク一覧取得 |
| idx_tasks_predecessor_id | predecessor_id | 後続タスク検索（再計算時） |

## 親タスクの日付について（DBに保存しない）

子タスクを持つタスク（親タスク）の `start_date` / `end_date` はDBに保存せず NULL とする。
表示時はアプリケーション側で子タスクの日付から動的に計算する。

| 項目 | 計算ロジック |
|------|-------------|
| 親タスクの開始日 | 直接の子タスクの `start_date` の最小値 |
| 親タスクの終了日 | 直接の子タスクの `end_date` の最大値 |

## 再計算の伝播

タスク保存時、保存前後で `start_date` / `end_date` に変化がない場合、後続タスクへの再計算処理自体を行わない。

変化がある場合、後続タスクの再計算は以下の手順でアプリケーションが実施する：

1. 変更されたタスクの `predecessor_id` を持つタスクを `idx_tasks_predecessor_id` で検索
2. 該当タスクの `start_date` を「前タスクの `end_date` の翌稼働日」に更新
3. 該当タスクに既存の `start_date` / `end_date` がある場合、その期間（日数）を維持したまま `end_date` をずらす（`work_days` からの再計算は行わない）。既存の日付がない場合のみ、新 `start_date` + `work_days`（稼働日カウント）で `end_date` を算出する
4. そのタスクを前タスクとして持つタスクに再帰的に同処理を適用

稼働日判定の優先順序は [../requirements/holiday-settings.md](../requirements/holiday-settings.md) を参照。

## 関連

* 親テーブル: [projects](projects.md), [assignees](assignees.md)
* 自己参照: `parent_id`（階層構造）, `predecessor_id`（FS依存関係）
* 要件: [../requirements/task-management.md](../requirements/task-management.md), [../requirements/schedule-calculation.md](../requirements/schedule-calculation.md)
* 画面: [ガントチャート](../screens/gantt-chart.md), [タスク編集モーダル](../screens/task-edit-modal.md)
* DDL・ER図・インデックス: [index.md](index.md)
