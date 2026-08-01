---
type: Requirement
title: 非機能要件
description: 動作環境・技術スタック・データ管理に関する非機能要件。
tags: [requirements, non-functional]
---

# 非機能要件

## 動作環境

* **形態**：デスクトップアプリケーション（.NET MAUI Blazor Hybrid）
* **対象OS**：Windows（主要ターゲット）
* **ランタイム**：.NET 10

## 技術スタック

| 役割 | 採用技術 |
|------|----------|
| UIフレームワーク | .NET MAUI Blazor Hybrid |
| UIコンポーネント | Razor コンポーネント（C#） |
| ORM | Entity Framework Core |
| データストア | SQLite3（EF Core経由） |

## データ管理

* **ORM**：Entity Framework Core（マイグレーション管理含む）
* **データストア**：SQLite3
* 将来的な SQL Server / MySQL への移行を考慮し、DBアクセスはすべてEF Core経由に統一する
* エクスポート・インポート・バックアップ機能は不要
* データはローカルのSQLiteファイルで完結する

## 関連

* [../tables/index.md](../tables/index.md) — DBテーブル定義
* [../screens/index.md](../screens/index.md) — UI技術スタック
