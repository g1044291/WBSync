using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using WBSync.Models;
using WBSync.Repositories;
using WBSync.Services;

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
    private bool _importing;
    private string? _importMessage;
    private bool _importIsError;

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
        _importMessage = null;
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
        try
        {
            await HolidayRepo.DeleteAsync(holidayId);
            _holidays = await HolidayRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _error = $"削除に失敗しました: {ex.InnerException?.Message ?? ex.Message}";
        }
    }

    /// <summary>CSVファイルを選択し、全体休日を一括インポートする。</summary>
    private async Task ImportCsv()
    {
        _importMessage = null;
        _importIsError = false;

        FileResult? file;
        try
        {
            file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "休日CSVファイルを選択",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".csv" } }
                })
            });
        }
        catch (Exception ex)
        {
            _importMessage = $"ファイル選択に失敗しました: {ex.Message}";
            _importIsError = true;
            return;
        }

        if (file is null)
            return;

        _importing = true;
        try
        {
            var csvText = await File.ReadAllTextAsync(file.FullPath);
            var parsed = HolidayCsvParser.Parse(csvText);

            if (parsed.Dates.Count == 0)
            {
                _importMessage = parsed.InvalidLineCount > 0
                    ? $"インポートできる日付がありませんでした（{parsed.InvalidLineCount}行が不正な形式です）"
                    : "インポートできる日付がありませんでした";
                _importIsError = true;
                return;
            }

            var imported = await HolidayRepo.CreateManyAsync(parsed.Dates.Select(d => new GlobalHoliday { Date = d }));
            var skipped = parsed.Dates.Count - imported;

            var message = $"{imported}件登録しました";
            if (skipped > 0)
                message += $"（{skipped}件は重複のためスキップ）";
            if (parsed.InvalidLineCount > 0)
                message += $"（{parsed.InvalidLineCount}行は不正な形式のためスキップ）";
            _importMessage = message;

            _holidays = await HolidayRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _importMessage = $"インポートに失敗しました: {ex.InnerException?.Message ?? ex.Message}";
            _importIsError = true;
        }
        finally
        {
            _importing = false;
        }
    }

    /// <summary>日付文字列を表示用にフォーマットする。</summary>
    /// <param name="date">yyyy-MM-dd 形式の日付文字列。</param>
    /// <returns>yyyy/MM/dd 形式の文字列。</returns>
    private static string FormatDate(string date)
        => DateOnly.TryParse(date, out var d) ? d.ToString("yyyy/MM/dd") : date;
}
