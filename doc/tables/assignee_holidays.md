---
type: SQLite Table
title: assignee_holidays
description: 担当者個人の休日（有給・不在日など）。プロジェクトをまたいで共有しない。
tags: [db, holiday, assignee]
resource: ../../src/WBSync/Models/AssigneeHoliday.cs
---

# assignee_holidays（担当者個人休日）

| カラム名 | 型 | NULL | デフォルト | 説明 |
|----------|----|------|------------|------|
| id | INTEGER | NOT NULL | AUTOINCREMENT | PK |
| assignee_id | INTEGER | NOT NULL | - | FK → [assignees](assignees.md).id |
| date | TEXT | NOT NULL | - | 休日の日付（YYYY-MM-DD） |
| memo | TEXT | NULL | - | メモ（例：有給休暇） |

## 制約

* `assignee_id` → `assignees.id` ON DELETE CASCADE（担当者削除時に個人休日も削除）
* UNIQUE(`assignee_id`, `date`)（同一担当者の同日重複を禁止）

## DDL

```sql
CREATE TABLE IF NOT EXISTS assignee_holidays (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    assignee_id INTEGER NOT NULL REFERENCES assignees(id) ON DELETE CASCADE,
    date        TEXT    NOT NULL,
    memo        TEXT,
    UNIQUE (assignee_id, date)
);
```

## インデックス

| インデックス名 | カラム | 用途 |
|---------------|--------|------|
| idx_assignee_holidays_assignee_id | assignee_id | 担当者別休日取得 |

## 関連

* 親テーブル: [assignees](assignees.md)
* 要件: [../requirements/holiday-settings.md](../requirements/holiday-settings.md), [../requirements/assignee-management.md](../requirements/assignee-management.md)
* 画面: [担当者詳細](../screens/assignee-detail.md)
* DDL・ER図: [index.md](index.md)
