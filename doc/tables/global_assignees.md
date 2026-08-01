---
type: SQLite Table
title: global_assignees
description: アプリ全体で共有する担当者マスタ。
tags: [db, assignee]
resource: ../../src/WBSync/Models/GlobalAssignee.cs
---

# global_assignees（グローバル担当者マスタ）

| カラム名 | 型 | NULL | デフォルト | 説明 |
|----------|----|------|------------|------|
| id | INTEGER | NOT NULL | AUTOINCREMENT | PK |
| name | TEXT | NOT NULL | - | 担当者名 |

## 制約

* `name` UNIQUE（重複登録を禁止）

## DDL

```sql
CREATE TABLE IF NOT EXISTS global_assignees (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT    NOT NULL UNIQUE
);
```

## インデックス

| インデックス名 | カラム | 用途 |
|---------------|--------|------|
| idx_global_assignees_name | name | 名前重複チェック（UNIQUE） |

## 関連

* 参照元: [assignees](assignees.md)（`global_assignee_id` で参照。プロジェクト内担当者がグローバルマスタと紐付く場合に使用。NULL = プロジェクト専用担当者）
* DDL・ER図: [index.md](index.md)
