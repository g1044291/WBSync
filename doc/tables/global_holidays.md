---
type: SQLite Table
title: global_holidays
description: アプリ全体の休日（祝日等）。土日は含まずアプリケーションロジックで固定休日として扱う。
tags: [db, holiday]
resource: ../../src/WBSync/Models/GlobalHoliday.cs
---

# global_holidays（全体休日）

| カラム名 | 型 | NULL | デフォルト | 説明 |
|----------|----|------|------------|------|
| id | INTEGER | NOT NULL | AUTOINCREMENT | PK |
| date | TEXT | NOT NULL | - | 休日の日付（YYYY-MM-DD）|
| name | TEXT | NULL | - | 休日名（例：元日） |

## 制約

* `date` UNIQUE（同じ日付の重複登録を禁止）

> 土曜・日曜はアプリケーションロジックで固定休日として扱い、このテーブルには登録しない。

## DDL

```sql
CREATE TABLE IF NOT EXISTS global_holidays (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    date TEXT    NOT NULL UNIQUE,
    name TEXT
);
```

## 関連

* 要件: [../requirements/holiday-settings.md](../requirements/holiday-settings.md)（CSVインポート仕様・稼働日判定の優先順序）
* 画面: [休日設定モーダル](../screens/holiday-settings-modal.md)
* DDL・ER図: [index.md](index.md)
