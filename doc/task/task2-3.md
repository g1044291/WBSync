# 2-3 tasksリポジトリ

## 作業詳細
- `ITaskRepository` インターフェースと `TaskRepository` 実装クラスの作成
- `GetByProjectAsync(int projectId): Task<List<TaskEntity>>` の実装（フラット一覧。ツリー構築はUI側）
- `CreateAsync(TaskEntity task): Task<TaskEntity>` の実装
- `UpdateAsync(TaskEntity task): Task<TaskEntity>` の実装
- `DeleteAsync(int id): Task` の実装（子タスクのカスケード削除はDB側に委任）
- `UpdateSortOrderAsync(int id, int sortOrder): Task` の実装
- `MauiProgram.cs` で DI コンテナに登録

## 完了条件
- タスクのCRUDが全て実行できる
- 削除時に子タスクがカスケード削除される
- 削除タスクを参照していた `PredecessorId` が NULL になる
- `sort_order` の更新が反映される
