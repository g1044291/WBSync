---
type: Requirement
title: 休日設定
description: 全体休日・担当者毎の休日、CSVインポートに関する機能要件。
tags: [requirements, holiday]
---

# 休日設定

* **全体の休日**：土曜日・日曜日を固定の休日とし、それ以外（祝日など）は手動登録またはCSVインポートで登録する
* **担当者毎の休日**：個人の有給・不在日などをプロジェクト内で手動登録する
* スケジュール計算時は全体の休日と担当者毎の休日の両方を考慮する
* **全体休日のCSVインポート**：1列目に日付（`yyyy/MM/dd` または `yyyy-MM-dd`）を記載したCSVファイルから一括登録できる。既存データおよびCSV内の重複日付はスキップする

## 稼働日判定の優先順序

1. 土曜・日曜 → 休日
2. `global_holidays.date` に存在 → 休日
3. 対象担当者の `assignee_holidays.date` に存在 → 休日
4. それ以外 → 稼働日

## 関連

* テーブル: [global_holidays](../tables/global_holidays.md), [assignee_holidays](../tables/assignee_holidays.md)
* 画面: [休日設定モーダル](../screens/holiday-settings-modal.md), [担当者詳細](../screens/assignee-detail.md)
