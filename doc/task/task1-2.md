# 1-2 EF Core + SQLite設定

## 作業詳細
- `Microsoft.EntityFrameworkCore.Sqlite` の導入
- db_design.md を基に各エンティティクラスを `Models/` に定義
  - `Project`, `Assignee`, `WbsTask`（`Task` は `System.Threading.Tasks.Task` と衝突するため）, `GlobalHoliday`, `AssigneeHoliday`
- `Data/AppDbContext.cs` に `AppDbContext` を作成（DbSet・リレーション・制約の設定）
- DBファイルのパスをアプリのユーザーデータフォルダ（`FileSystem.AppDataDirectory`）に設定
- `MauiProgram.cs` で DbContext を DI コンテナに登録（`AddDbContext`）

## 完了条件
- アプリ起動時にユーザーデータフォルダ配下に `.db` ファイルが作成される
- DbContext 経由で簡単なクエリ（`context.Projects.ToList()` 等）が実行できる
