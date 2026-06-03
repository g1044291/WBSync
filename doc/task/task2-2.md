# 2-2 assigneesリポジトリ

## 作業詳細
- `IAssigneeRepository` インターフェースと `AssigneeRepository` 実装クラスの作成
- `GetByProjectAsync(int projectId): Task<List<Assignee>>` の実装
- `CreateAsync(Assignee assignee): Task<Assignee>` の実装
- `UpdateAsync(Assignee assignee): Task<Assignee>` の実装
- `DeleteAsync(int id): Task` の実装
- `UpdateSortOrderAsync(int id, int sortOrder): Task` の実装
- `MauiProgram.cs` で DI コンテナに登録

## 完了条件
- 担当者のCRUDが全て実行できる
- 削除時にカスケードで `assignee_holidays` も削除される
- `sort_order` の更新が反映される
