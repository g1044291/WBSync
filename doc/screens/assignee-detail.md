---
type: Screen
title: 担当者詳細
description: 担当者名と個人休日を設定する画面。
tags: [ui, assignee, holiday]
resource: ../../src/WBSync/Components/Pages/AssigneeDetail.razor
---

# 担当者詳細

## レイアウト

```
┌────────────────────────────────────────┐
│ [←戻る]  担当者詳細                     │
├────────────────────────────────────────┤
│ 担当者名  [山田 太郎___________]  [保存] │
├────────────────────────────────────────┤
│ 個人休日                        [+ 追加] │
│ ┌──────────────────────────────────┐  │
│ │ 2025/06/10  有給休暇      [削除]  │  │
│ │ 2025/07/15  夏季休暇      [削除]  │  │
│ └──────────────────────────────────┘  │
└────────────────────────────────────────┘
```

## 要素

| 要素 | 説明 |
|------|------|
| 戻るボタン | [担当者一覧](assignee-list.md) へ戻る |
| 担当者名 | テキスト入力で編集可能。保存ボタンで確定 |
| 個人休日リスト | 登録済みの休日を日付・メモ付きで一覧表示 |
| 追加ボタン | 日付ピッカーとメモ入力で休日を追加 |
| 削除ボタン | 対象の休日を削除 |

## 関連

* 要件: [../requirements/assignee-management.md](../requirements/assignee-management.md), [../requirements/holiday-settings.md](../requirements/holiday-settings.md)
* テーブル: [assignees](../tables/assignees.md), [assignee_holidays](../tables/assignee_holidays.md)
