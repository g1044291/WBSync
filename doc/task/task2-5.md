# 2-5 assignee_holidaysリポジトリ

## 作業詳細
- `IAssigneeHolidayRepository` インターフェースと `AssigneeHolidayRepository` 実装クラスの作成
- `GetByAssigneeAsync(int assigneeId): Task<List<AssigneeHoliday>>` の実装
- `CreateAsync(AssigneeHoliday holiday): Task<AssigneeHoliday>` の実装
- `DeleteAsync(int id): Task` の実装
- `MauiProgram.cs` で DI コンテナに登録

## 完了条件
- 担当者個人休日のCRUDが実行できる
- 同一担当者・同じ日付の重複登録で EF Core の例外が返される（UNIQUE制約違反）
