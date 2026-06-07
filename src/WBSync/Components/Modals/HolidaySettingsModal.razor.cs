using Microsoft.AspNetCore.Components;
using WBSync.Models;
using WBSync.Repositories;

namespace WBSync.Components.Modals;

/// <summary>全体休日設定モーダルのコードビハインド。</summary>
public partial class HolidaySettingsModal
{
    /// <summary>モーダルの開閉状態。</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>モーダルを閉じるときに呼び出されるコールバック。</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>全体休日リポジトリ。</summary>
    [Parameter, EditorRequired] public IGlobalHolidayRepository HolidayRepo { get; set; } = null!;

    private List<GlobalHoliday> _holidays = [];
    private bool _isAdding;
    private DateOnly? _newDate;
    private string _newName = string.Empty;
    private bool _saving;
    private string? _error;

    /// <summary>モーダルが開かれたときに休日一覧を読み込む。</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen && !_holidays.Any())
            _holidays = await HolidayRepo.GetAllAsync();
    }

    /// <summary>モーダルを閉じる。</summary>
    private async Task HandleClose()
    {
        _isAdding = false;
        _error = null;
        await OnClose.InvokeAsync();
    }

    /// <summary>休日追加フォームを表示する。</summary>
    private void StartAdding()
    {
        _newDate = null;
        _newName = string.Empty;
        _error = null;
        _isAdding = true;
    }

    /// <summary>休日追加フォームをキャンセルする。</summary>
    private void CancelAdding()
    {
        _isAdding = false;
        _error = null;
    }

    /// <summary>全体休日を作成する。</summary>
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

    /// <summary>全体休日を削除する。</summary>
    /// <param name="holidayId">削除する休日ID。</param>
    private async Task DeleteHoliday(int holidayId)
    {
        await HolidayRepo.DeleteAsync(holidayId);
        _holidays = await HolidayRepo.GetAllAsync();
    }

    /// <summary>日付文字列を表示用にフォーマットする。</summary>
    /// <param name="date">yyyy-MM-dd 形式の日付文字列。</param>
    /// <returns>yyyy/MM/dd 形式の文字列。</returns>
    private static string FormatDate(string date)
        => DateOnly.TryParse(date, out var d) ? d.ToString("yyyy/MM/dd") : date;
}
