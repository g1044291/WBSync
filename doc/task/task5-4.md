# 5-4 タスクのD&D並び替え

## 作業詳細
- SortableJS を JS Interop 経由で利用（`IJSRuntime.InvokeVoidAsync` でSortable初期化）
- Blazorコンポーネントの `OnAfterRenderAsync` でSortableJSをタスクリストに適用
- ドロップ完了イベントをJSからBlazorへコールバックして `sort_order` を再計算
- 異なる階層へのドロップを無効化（`group` オプションで制御）
- 並び替え後に `UpdateSortOrderAsync`（2-3）でDBを更新

## 完了条件
- 同一階層内でドラッグ＆ドロップによる順序変更が動作する
- 異なる階層へのドロップが無効（ドロップしても移動しない）
- 並び替え後の順序がDB・再読み込み後も保持される
