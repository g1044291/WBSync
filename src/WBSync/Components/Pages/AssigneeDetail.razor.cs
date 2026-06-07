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
        finally
        {
            _nameSaving = false;
        }
    }

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
    /// <param name="holidayId">削除する休日ID。</param>
    private async Task DeleteHoliday(int holidayId)
    {
        await HolidayRepo.DeleteAsync(holidayId);
        _holidays = await HolidayRepo.GetByAssigneeAsync(AssigneeId);
    }

    /// <summary>日付文字列を表示用にフォーマットする。</summary>
    /// <param name="date">yyyy-MM-dd 形式の日付文字列。</param>
    /// <returns>yyyy/MM/dd 形式の文字列。</returns>
    private static string FormatDate(string date)
        => DateOnly.TryParse(date, out var d) ? d.ToString("yyyy/MM/dd") : date;

    /// <summary>担当者一覧画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/assignees/{ProjectId}");
}
