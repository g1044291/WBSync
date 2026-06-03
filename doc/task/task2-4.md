# 2-4 global_holidaysリポジトリ

## 作業詳細
- `IGlobalHolidayRepository` インターフェースと `GlobalHolidayRepository` 実装クラスの作成
- `GetAllAsync(): Task<List<GlobalHoliday>>` の実装
- `CreateAsync(GlobalHoliday holiday): Task<GlobalHoliday>` の実装
- `DeleteAsync(int id): Task` の実装
- `MauiProgram.cs` で DI コンテナに登録

## 完了条件
- 全体休日のCRUDが実行できる
- 同じ日付の重複登録で EF Core の例外が返される（UNIQUE制約違反）
