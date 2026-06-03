# 1-3 DBマイグレーション機構

## 作業詳細
- `dotnet ef migrations add InitialCreate` で初期マイグレーションを作成
- アプリ起動時（`MauiProgram.cs` 内）に `context.Database.MigrateAsync()` を実行して自動マイグレーションを適用
- 将来のスキーマ変更はマイグレーションを追加して対応する運用ルールを確立

## 完了条件
- 初回起動で全テーブル・インデックスが作成される
- 2回目以降の起動でエラーが発生しない
- SQLite Browser 等のツールで db_design.md と一致するスキーマが確認できる
