---
type: Requirement Set
title: 要件定義
description: WBSyncの機能要件・非機能要件の一覧。
tags: [requirements]
---

# 要件定義

## 概要

本システムは、工数（人日）をベースにスケジュールを自動生成するWBS作成機能と、日々の実績を記録して予定との差分・遅延を可視化する工数管理機能を併せ持つプロジェクト管理ツールである。
依存関係・休日・手動調整を組み合わせて現実に即したプロジェクト計画を簡易に作成し、作成後は実績記録・ダッシュボード・複数プロジェクト横断ビューを通じて予実の乖離を継続的に把握する。

単一ユーザー利用を前提としつつ、複数担当者を含むプロジェクト単位のWBS管理・工数管理を行う。

詳細は [overview.md](overview.md) を参照。

## 機能要件

| ドキュメント | 内容 |
|------|------|
| [project-management.md](project-management.md) | プロジェクト管理 |
| [task-management.md](task-management.md) | タスク管理（属性・階層構造・操作） |
| [schedule-calculation.md](schedule-calculation.md) | スケジュール計算・依存関係・再計算 |
| [assignee-management.md](assignee-management.md) | 担当者管理 |
| [holiday-settings.md](holiday-settings.md) | 休日設定 |
| [effort-management.md](effort-management.md) | 工数管理（実績記録・残工数・前倒し/遅れ・担当者別ダッシュボード） |

## 非機能要件

[non-functional.md](non-functional.md) を参照。
