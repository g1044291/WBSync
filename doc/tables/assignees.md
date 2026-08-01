---
type: SQLite Table
title: assignees
description: 担当者。プロジェクト内で管理し、任意でグローバル担当者マスタと紐付く。
tags: [db, assignee]
resource: ../../src/WBSync/Models/Assignee.cs
---

# assignees（担当者）

| カラム名 | 型 | NULL | デフォルト | 説明 |
|----------|----|------|------------|------|
| id | INTEGER | NOT NULL | AUTOINCREMENT | PK |
| project_id | INTEGER | NOT NULL | - | FK → [projects](projects.md).id |
| global_assignee_id | INTEGER | NULL | - | FK → [global_assignees](global_assignees.md).id。NULL = プロジェクト専用 |
| name | TEXT | NOT NULL | - | 担当者名 |
| sort_order | INTEGER | NOT NULL | 0 | 担当者一覧の表示順 |
| created_at | TEXT | NOT NULL | DATETIME('now') | 作成日時 |
| updated_at | TEXT | NOT NULL | DATETIME('now') | 更新日時 |

## 制約

* `project_id` → `projects.id` ON DELETE CASCADE（プロジェクト削除時に担当者も削除）
* `global_assignee_id` → `global_assignees.id` ON DELETE SET NULL（グローバルマスタ削除時は連携解除）

## DDL

```sql
CREATE TABLE IF NOT EXISTS assignees (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id         INTEGER NOT NULL REFERENCES projects(id)         ON DELETE CASCADE,
    global_assignee_id INTEGER          REFERENCES global_assignees(id) ON DELETE SET NULL,
    name               TEXT    NOT NULL,
    sort_order         INTEGER NOT NULL DEFAULT 0,
    created_at         TEXT    NOT NULL DEFAULT (DATETIME('now', 'localtime')),
    updated_at         TEXT    NOT NULL DEFAULT (DATETIME('now', 'localtime'))
);
```

## インデックス

| インデックス名 | カラム | 用途 |
|---------------|--------|------|
| idx_assignees_project_id | project_id | プロジェクト別担当者取得 |

## 関連

* 親テーブル: [projects](projects.md), [global_assignees](global_assignees.md)
* 参照元: [tasks](tasks.md)（`assignee_id` で参照）, [assignee_holidays](assignee_holidays.md)（`assignee_id` で参照）
* 要件: [../requirements/assignee-management.md](../requirements/assignee-management.md)
* 画面: [担当者一覧](../screens/assignee-list.md), [担当者詳細](../screens/assignee-detail.md)
* DDL・ER図: [index.md](index.md)
