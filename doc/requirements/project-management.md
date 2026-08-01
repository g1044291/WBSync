---
type: Requirement
title: プロジェクト管理
description: プロジェクト一覧・作成に関する機能要件。
tags: [requirements, project]
---

# プロジェクト管理

* プロジェクト一覧画面を持ち、一覧からプロジェクトを選択するとそのWBS画面へ遷移する
* プロジェクトの作成が可能
* プロジェクトの削除・複製機能は不要
* プロジェクト間の横断機能は不要
* プロジェクトが持つ属性は以下の通り
  * プロジェクト名
  * 開始日（スケジュール計算の起点となる日付）

## 関連

* テーブル: [projects](../tables/projects.md)
* 画面: [プロジェクト一覧](../screens/project-list.md), [プロジェクト作成モーダル](../screens/project-create-modal.md)
