---
type: Screen
title: プロジェクト作成モーダル
description: プロジェクトの新規作成を行うポップアップ。
tags: [ui, project]
resource: ../../src/WBSync/Components/Modals/ProjectCreateModal.razor
---

# プロジェクト作成モーダル

## レイアウト

```
┌────────────────────────────────────────┐
│ プロジェクトを作成                  [×] │
├────────────────────────────────────────┤
│ プロジェクト名  [________________________] │
│ 開始日         [____/____/____]        │
│                                        │
│              [キャンセル]  [作成]      │
└────────────────────────────────────────┘
```

## 要素

| 項目 | 入力形式 | 備考 |
|------|----------|------|
| プロジェクト名 | テキスト入力 | 必須 |
| 開始日 | 日付ピッカー | 必須。スケジュール計算の起点 |

## 関連

* 要件: [../requirements/project-management.md](../requirements/project-management.md)
* テーブル: [projects](../tables/projects.md)
* 呼び出し元: [プロジェクト一覧](project-list.md)
