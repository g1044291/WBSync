---
type: Requirement
title: 概要・コンセプト
description: WBSyncのシステム概要と設計コンセプト。
tags: [requirements, overview]
---

# 概要・コンセプト

## 概要

本システムは、工数（人日）をベースにスケジュールを自動生成するWBSツールである。
依存関係・休日・手動調整を組み合わせ、現実に即したプロジェクト計画を簡易に作成・管理する。

単一ユーザー利用を前提としつつ、複数担当者を含むプロジェクト単位のWBS管理を行う。

## コンセプト

* 工数を入力するだけでスケジュール生成
* モダンで直感的なUI（Excelライクには拘らない）
* 軽量（〜100タスク想定）
* プロジェクト単位で管理
* 全体の休日及び担当者毎の休日を設定可能
* ポータビリティを優先し、SQLite3を利用

## 関連

* [non-functional.md](non-functional.md) — 動作環境・技術スタック
* [../tables/index.md](../tables/index.md) — DBテーブル定義
* [../screens/index.md](../screens/index.md) — 画面設計
