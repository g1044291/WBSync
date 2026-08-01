---
type: Requirement Set
title: 要件定義
description: WBSyncの機能要件・非機能要件の一覧。
tags: [requirements]
---

# 要件定義

## 概要

本システムは、工数（人日）をベースにスケジュールを自動生成するWBSツールである。
依存関係・休日・手動調整を組み合わせ、現実に即したプロジェクト計画を簡易に作成・管理する。

単一ユーザー利用を前提としつつ、複数担当者を含むプロジェクト単位のWBS管理を行う。

詳細は [overview.md](overview.md) を参照。

## 機能要件

| ドキュメント | 内容 |
|------|------|
| [project-management.md](project-management.md) | プロジェクト管理 |
| [task-management.md](task-management.md) | タスク管理（属性・階層構造・操作） |
| [schedule-calculation.md](schedule-calculation.md) | スケジュール計算・依存関係・再計算 |
| [assignee-management.md](assignee-management.md) | 担当者管理 |
| [holiday-settings.md](holiday-settings.md) | 休日設定 |

## 非機能要件

[non-functional.md](non-functional.md) を参照。
