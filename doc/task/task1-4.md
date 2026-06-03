# 1-4 共通Blazorコンポーネント

## 作業詳細
- `Button`：variant（primary / secondary / danger）対応
- `Modal`：タイトル・コンテンツ・フッタースロット（`RenderFragment`）構成、背景クリックで閉じる
- `ConfirmDialog`：メッセージ・キャンセル・確認ボタン構成
- `Dropdown`：選択肢リストと選択値の表示（`TValue` ジェネリック対応）
- `DatePicker`：日付入力（`<input type="date">` ベース）
- `Badge`：ステータス表示用（未着手 / 進行中 / 完了 / 保留の色分け）

## 完了条件
- 各コンポーネントが単独でレンダリングされる
- Modal の開閉、Dropdown の選択、DatePicker の日付入力が動作する
- 未保存変更がある状態で Modal を閉じようとすると確認ダイアログが表示される
