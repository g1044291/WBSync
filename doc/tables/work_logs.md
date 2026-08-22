---
type: SQLite Table
title: work_logs
description: タスクへの日々の作業実績。工数管理画面・集計ダッシュボードの基盤。
tags: [db, task, assignee]
resource: ../../src/WBSync/Models/WorkLog.cs
---

# work_logs（作業実績）

| カラム名 | 型 | NULL | デフォルト | 説明 |
|----------|----|------|------------|------|
| id | INTEGER | NOT NULL | AUTOINCREMENT | PK |
| task_id | INTEGER | NOT NULL | - | FK → [tasks](tasks.md).id |
| assignee_id | INTEGER | NULL | - | FK → [assignees](assignees.md).id。作業時点の担当者が変わる場合を考慮 |
| date | TEXT | NOT NULL | - | 作業日（YYYY-MM-DD） |
| minutes | INTEGER | NOT NULL | - | 作業時間（分単位） |
| comment | TEXT | NULL | - | 備忘用の任意コメント |

## 制約

* `task_id` → `tasks.id` ON DELETE CASCADE（タスク削除時に作業実績も削除）
* `assignee_id` → `assignees.id` ON DELETE SET NULL（担当者削除後も実績履歴を保持）

## DDL

```sql
CREATE TABLE IF NOT EXISTS work_logs (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    task_id     INTEGER NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    assignee_id INTEGER REFERENCES assignees(id) ON DELETE SET NULL,
    date        TEXT    NOT NULL,
    minutes     INTEGER NOT NULL,
    comment     TEXT
);
```

## インデックス

| インデックス名 | カラム | 用途 |
|---------------|--------|------|
| idx_work_logs_task_id | task_id | タスク別作業実績取得 |
| idx_work_logs_assignee_id | assignee_id | 担当者別作業実績取得 |

## 関連

* 親テーブル: [tasks](tasks.md), [assignees](assignees.md)
* 要件: [../requirements/effort-management.md](../requirements/effort-management.md)
* 画面: [工数管理](../screens/effort-management.md), [ダッシュボード](../screens/dashboard.md), [担当者別稼働時間カレンダー](../screens/assignee-workload-calendar.md)
* DDL・ER図: [index.md](index.md)
