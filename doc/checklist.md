# 開発進捗チェックリスト

- 詳細はtask.mdに記載

## 大タスク1：開発環境・プロジェクト基盤構築
- [x] 1-1 [MAUIプロジェクト初期化](task/task1-1.md)
- [x] 1-2 [EF Core + SQLite設定](task/task1-2.md)
- [x] 1-3 [DBマイグレーション機構](task/task1-3.md)
- [x] 1-4 [共通Blazorコンポーネント](task/task1-4.md)

## 大タスク2：データアクセス層
- [x] 2-1 [projectsリポジトリ](task/task2-1.md)
- [x] 2-2 [assigneesリポジトリ](task/task2-2.md)
- [x] 2-3 [tasksリポジトリ](task/task2-3.md)
- [x] 2-4 [global_holidaysリポジトリ](task/task2-4.md)
- [x] 2-5 [assignee_holidaysリポジトリ](task/task2-5.md)

## 大タスク3：プロジェクト管理機能
- [x] 3-1 [プロジェクト一覧画面（S01）](task/task3-1.md)
- [x] 3-2 [プロジェクト作成モーダル（S07）](task/task3-2.md)
- [x] 3-3 [休日設定ボタンの配置](task/task3-3.md)

## 大タスク4：担当者・休日管理機能
- [x] 4-1 [全体休日設定画面（S04）](task/task4-1.md)
- [x] 4-2 [担当者一覧画面（S05）](task/task4-2.md)
- [x] 4-3 [担当者詳細画面（S06）](task/task4-3.md)

## 大タスク5：ガントチャート画面・タスクツリー
- [x] 5-1 [画面レイアウト構築](task/task5-1.md)
- [x] 5-2 [ヘッダー実装](task/task5-2.md)
- [x] 5-3 [タスクツリー表示](task/task5-3.md)
- [ ] 5-4 [タスクのD&D並び替え](task/task5-4.md)
- [x] 5-5 [右クリックメニュー](task/task5-5.md)

## 大タスク6：タスク編集機能
- [x] 6-1 [タスク編集モーダル（S03）UI](task/task6-1.md)
- [x] 6-2 [タスク作成処理](task/task6-2.md)
- [x] 6-3 [タスク更新処理](task/task6-3.md)
- [x] 6-4 [タスク削除処理](task/task6-4.md)

## 大タスク7：スケジュール計算エンジン
- [x] 7-1 [稼働日判定ロジック](task/task7-1.md)
- [x] 7-2 [終了日自動計算](task/task7-2.md)
- [x] 7-3 [FS依存による開始日設定](task/task7-3.md)
- [x] 7-4 [後続タスクへの再計算伝播](task/task7-4.md)
- [x] 7-5 [担当者の工数重複警告検知](task/task7-5.md)
- [ ] 7-6 [親タスク日付の動的計算](task/task7-6.md)

## 大タスク8：ガントチャート描画
- [x] 8-1 [時間軸レンダリング](task/task8-1.md)
- [ ] 8-2 [タスクバー描画](task/task8-2.md)
- [ ] 8-3 [スケール切り替え](task/task8-3.md)
- [ ] 8-4 [左右ペインのスクロール同期](task/task8-4.md)
- [x] 8-5 [全体休日列のハイライト](task/task8-5.md)
- [x] 8-6 [個人休日行のハイライト](task/task8-6.md)

## 大タスク9：結合・品質
- [ ] 9-1 [画面遷移の繋ぎ込み](task/task9-1.md)
- [ ] 9-2 [バリデーション整備](task/task9-2.md)
- [ ] 9-3 [エラーハンドリング](task/task9-3.md)
- [ ] 9-4 [動作確認・デバッグ](task/task9-4.md)
