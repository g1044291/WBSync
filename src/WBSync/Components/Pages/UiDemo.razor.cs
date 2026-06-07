namespace WBSync.Components.Pages;

public partial class UiDemo
{
    private string _log = string.Empty;
    private DateOnly? _date = DateOnly.FromDateTime(DateTime.Today);
    private string? _selectedStatus;
    private bool _modalOpen;
    private bool _confirmOpen;
    private string _confirmResult = string.Empty;

    private readonly List<string> _statuses = ["未着手", "進行中", "完了", "保留"];

    private void GoBack() => Nav.NavigateTo("/");

    private void Log(string msg) => _log = msg;
    private void LogPrimary() => Log("primary clicked");
    private void LogSecondary() => Log("secondary clicked");
    private void LogDanger() => Log("danger clicked");
    private void OpenModal() => _modalOpen = true;
    private void CloseModal() => _modalOpen = false;
    private void OpenConfirm() => _confirmOpen = true;

    private void HandleConfirm()
    {
        _confirmOpen = false;
        _confirmResult = "→ 削除を確認しました";
    }

    private void HandleCancel()
    {
        _confirmOpen = false;
        _confirmResult = "→ キャンセルしました";
    }
}
