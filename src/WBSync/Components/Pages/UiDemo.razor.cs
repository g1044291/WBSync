namespace WBSync.Components.Pages;

/// <summary>UI コンポーネント確認画面のコードビハインド。</summary>
public partial class UiDemo
{
    private string _log = string.Empty;
    private DateOnly? _date = DateOnly.FromDateTime(DateTime.Today);
    private string? _selectedStatus;
    private bool _modalOpen;
    private bool _confirmOpen;
    private string _confirmResult = string.Empty;

    private readonly List<string> _statuses = ["未着手", "進行中", "完了", "保留"];

    /// <summary>プロジェクト一覧に戻る。</summary>
    private void GoBack() => Nav.NavigateTo("/");

    /// <summary>ログメッセージを更新する。</summary>
    /// <param name="msg">表示するメッセージ。</param>
    private void Log(string msg) => _log = msg;

    /// <summary>Primary ボタンクリックをログに記録する。</summary>
    private void LogPrimary() => Log("primary clicked");

    /// <summary>Secondary ボタンクリックをログに記録する。</summary>
    private void LogSecondary() => Log("secondary clicked");

    /// <summary>Danger ボタンクリックをログに記録する。</summary>
    private void LogDanger() => Log("danger clicked");

    /// <summary>モーダルを開く。</summary>
    private void OpenModal() => _modalOpen = true;

    /// <summary>モーダルを閉じる。</summary>
    private void CloseModal() => _modalOpen = false;

    /// <summary>確認ダイアログを開く。</summary>
    private void OpenConfirm() => _confirmOpen = true;

    /// <summary>確認ダイアログで「確認」が押されたときの処理。</summary>
    private void HandleConfirm()
    {
        _confirmOpen = false;
        _confirmResult = "→ 削除を確認しました";
    }

    /// <summary>確認ダイアログで「キャンセル」が押されたときの処理。</summary>
    private void HandleCancel()
    {
        _confirmOpen = false;
        _confirmResult = "→ キャンセルしました";
    }
}
