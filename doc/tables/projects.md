---
type: SQLite Table
title: projects
description: プロジェクト。スケジュール計算の起点となる開始日を持つ。
tags: [db, project]
resource: ../../src/WBSync/Models/Project.cs
---

# projects（プロジェクト）

| カラム名 | 型 | NULL | デフォルト | 説明 |
|----------|----|------|------------|------|
| id | INTEGER | NOT NULL | AUTOINCREMENT | PK |
| name | TEXT | NOT NULL | - | プロジェクト名 |
| start_date | TEXT | NOT NULL | - | 開始日（YYYY-MM-DD） |
| created_at | TEXT | NOT NULL | DATETIME('now') | 作成日時 |
| updated_at | TEXT | NOT NULL | DATETIME('now') | 更新日時 |

## DDL

```sql
CREATE TABLE IF NOT EXISTS projects (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    name       TEXT    NOT NULL,
    start_date TEXT    NOT NULL,
    created_at TEXT    NOT NULL DEFAULT (DATETIME('now', 'localtime')),
    updated_at TEXT    NOT NULL DEFAULT (DATETIME('now', 'localtime'))
);
```

## 関連

* 子テーブル: [assignees](assignees.md)（`project_id` で参照、ON DELETE CASCADE）, [tasks](tasks.md)（`project_id` で参照、ON DELETE CASCADE）
* 要件: [../requirements/project-management.md](../requirements/project-management.md)
* 画面: [プロジェクト一覧](../screens/project-list.md), [プロジェクト作成モーダル](../screens/project-create-modal.md)
* DDL・ER図: [index.md](index.md)
