# 2-1 projectsリポジトリ

## 作業詳細
- `IProjectRepository` インターフェースと `ProjectRepository` 実装クラスの作成
- `GetAllAsync(): Task<List<Project>>` の実装
- `CreateAsync(Project project): Task<Project>` の実装
- `MauiProgram.cs` で DI コンテナにサービス登録（`AddScoped`）

## 完了条件
- プロジェクトの一覧取得・作成がアプリから実行できる
- 作成したデータがDBに永続化され、再起動後も取得できる
