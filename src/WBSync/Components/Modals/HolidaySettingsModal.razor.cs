using Microsoft.AspNetCore.Components;
using WBSync.Models;
using WBSync.Repositories;

namespace WBSync.Components.Modals;

public partial class HolidaySettingsModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter, EditorRequired] public IGlobalHolidayRepository HolidayRepo { get; set; } = null!;

    private List<GlobalHoliday> _holidays = [];
    private bool _isAdding;
    private DateOnly? _newDate;
    private string _newName = string.Empty;
    private bool _saving;
    private string? _error;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !_holidays.Any())
            _holidays = await HolidayRepo.GetAllAsync();
    }

    private async Task HandleClose()
    {
        _isAdding = false;
        _error = null;
        await OnClose.InvokeAsync();
    }

    private void StartAdding()
    {
        _newDate = null;
        _newName = string.Empty;
        _error = null;
        _isAdding = true;
    }

    private void CancelAdding()
    {
        _isAdding = false;
        _error = null;
    }

    private async Task AddHoliday()
    {
        _error = null;
        if (_newDate is null) { _error = "日付を入力してください"; return; }

        _saving = true;
        try
        {
            await HolidayRepo.CreateAsync(new GlobalHoliday
            {
                Date = _newDate.Value.ToString("yyyy-MM-dd"),
                Name = string.IsNullOrWhiteSpace(_newName) ? null : _newName.Trim()
            });
            _isAdding = false;
            _holidays = await HolidayRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _error = ex.InnerException?.Message.Contains("UNIQUE") == true
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
        _holidays = await HolidayRepo.GetAllAsync();
    }

    private static string FormatDate(string date)
        => DateOnly.TryParse(date, out var d) ? d.ToString("yyyy/MM/dd") : date;
}
