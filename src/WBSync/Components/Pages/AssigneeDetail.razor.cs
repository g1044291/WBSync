using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Pages;

public partial class AssigneeDetail
{
    [Parameter] public int ProjectId { get; set; }
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

    private void OpenAddHoliday()
    {
        _newHolidayDate = null;
        _newHolidayMemo = string.Empty;
        _holidayError = null;
        _addHolidayOpen = true;
    }

    private void CloseAddHoliday()
    {
        _addHolidayOpen = false;
        _holidayError = null;
    }

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

    private async Task DeleteHoliday(int holidayId)
    {
        await HolidayRepo.DeleteAsync(holidayId);
        _holidays = await HolidayRepo.GetByAssigneeAsync(AssigneeId);
    }

    private static string FormatDate(string date)
        => DateOnly.TryParse(date, out var d) ? d.ToString("yyyy/MM/dd") : date;

    private void GoBack() => Nav.NavigateTo($"/assignees/{ProjectId}");
}
