using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>担当者詳細画面のコードビハインド。</summary>
public partial class AssigneeDetail
{
    /// <summary>プロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    /// <summary>担当者ID。</summary>
    [Parameter] public int AssigneeId { get; set; }

    private Assignee? _assignee;
    private GlobalAssignee? _linkedGlobal;
    private string _editName = string.Empty;
    private bool _nameSaving;
    private string? _nameError;
    private bool _nameSaved;

    private List<AssigneeHoliday> _holidays = [];
    private bool _addHolidayOpen;
    private DateOnly? _newHolidayDate;
    private string _newHolidayMemo = string.Empty;
    private bool _saving;
    private string? _holidayError;

    private bool _unlinkConfirmOpen;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
        _assignee = assignees.FirstOrDefault(a => a.Id == AssigneeId);
        if (_assignee is null)
        {
            Nav.NavigateTo($"/assignees/{ProjectId}");
            return;
        }
        _editName = _assignee.Name;

        if (_assignee.GlobalAssigneeId.HasValue)
        {
            var allGlobals = await GlobalAssigneeRepo.GetAllAsync();
            _linkedGlobal = allGlobals.FirstOrDefault(g => g.Id == _assignee.GlobalAssigneeId);
        }

        _holidays = await HolidayRepo.GetByAssigneeAsync(AssigneeId);
    }

    /// <summary>担当者名を保存する。</summary>
    private async Task SaveName()
    {
        _nameError = null;
        _nameSaved = false;
        if (string.IsNullOrWhiteSpace(_editName))
        {
            _nameError = "担当者名を入力してください";
            return;
        }
        _nameSaving = true;
        try
        {
            _assignee!.Name = _editName.Trim();
            await AssigneeRepo.UpdateAsync(_assignee);
            _nameSaved = true;
        }
        catch (Exception ex)
        {
            _nameError = $"エラーが発生しました: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            _nameSaving = false;
        }
    }

    // ---- グローバルマスタ連携解除 ----

    /// <summary>グローバルマスタ連携解除確認ダイアログを開く。</summary>
    private void OpenUnlinkConfirm() => _unlinkConfirmOpen = true;

    /// <summary>グローバルマスタ連携解除確認ダイアログを閉じる。</summary>
    private void CloseUnlinkConfirm() => _unlinkConfirmOpen = false;

    /// <summary>グローバルマスタとの連携を解除してプロジェクト専用担当者にする。</summary>
    private async Task UnlinkGlobal()
    {
        if (_assignee is null) return;
        try
        {
            _assignee.GlobalAssigneeId = null;
            await AssigneeRepo.UpdateAsync(_assignee);
            _linkedGlobal = null;
            _unlinkConfirmOpen = false;
        }
        catch (Exception ex)
        {
            _nameError = $"連携解除に失敗しました: {ex.InnerException?.Message ?? ex.Message}";
            _unlinkConfirmOpen = false;
        }
    }

    // ---- 個人休日 ----

    /// <summary>個人休日追加モーダルを開く。</summary>
    private void OpenAddHoliday()
    {
        _newHolidayDate = null;
        _newHolidayMemo = string.Empty;
        _holidayError = null;
        _addHolidayOpen = true;
    }

    /// <summary>個人休日追加モーダルを閉じる。</summary>
    private void CloseAddHoliday()
    {
        _addHolidayOpen = false;
        _holidayError = null;
    }

    /// <summary>新しい個人休日を作成する。</summary>
    private async Task AddHoliday()
    {
        _holidayError = null;
        if (_newHolidayDate is null)
        {
            _holidayError = "日付を入力してください";
            return;
        }
        _saving = true;
        try
        {
            await HolidayRepo.CreateAsync(new AssigneeHoliday
            {
                AssigneeId = AssigneeId,
                Date = _newHolidayDate.Value.ToString("yyyy-MM-dd"),
                Memo = string.IsNullOrWhiteSpace(_newHolidayMemo) ? null : _newHolidayMemo.Trim()
            });
            _addHolidayOpen = false;
            _holidays = await HolidayRepo.GetByAssigneeAsync(AssigneeId);
        }
        catch (Exception ex)
        {
            _holidayError = ex.InnerException?.Message.Contains("UNIQUE") == true
                ? "同じ日付がすでに登録されています"
                : $"エラー: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>個人休日を削除する。</summary>
    private async Task DeleteHoliday(int holidayId)
    {
        try
        {
            await HolidayRepo.DeleteAsync(holidayId);
            _holidays = await HolidayRepo.GetByAssigneeAsync(AssigneeId);
        }
        catch (Exception ex)
        {
            _holidayError = $"削除に失敗しました: {ex.InnerException?.Message ?? ex.Message}";
        }
    }

    /// <summary>日付文字列を表示用にフォーマットする。</summary>
    private static string FormatDate(string date)
        => DateOnly.TryParse(date, out var d) ? d.ToString("yyyy/MM/dd") : date;

    /// <summary>担当者一覧画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/assignees/{ProjectId}");
}
