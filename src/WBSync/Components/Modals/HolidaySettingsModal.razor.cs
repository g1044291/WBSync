using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using WBSync.Helpers;
using WBSync.Models;
using WBSync.Repositories.Interfaces;

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
    private bool _disableAdd;
    private StatusMessage? _addStatus;
    private bool _disableImport;
    private StatusMessage? _importStatus;

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
        _addStatus = null;
        _importStatus = null;
        await OnClose.InvokeAsync();
    }

    /// <summary>休日追加フォームを表示する。</summary>
    private void StartAdding()
    {
        _newDate = null;
        _newName = string.Empty;
        _addStatus = null;
        _isAdding = true;
    }

    /// <summary>休日追加フォームをキャンセルする。</summary>
    private void CancelAdding()
    {
        _isAdding = false;
        _addStatus = null;
    }

    /// <summary>全体休日を作成する。</summary>
    private async Task AddHoliday()
    {
        _addStatus = null;
        if (_newDate is null) { _addStatus = StatusMessage.Error("日付を入力してください"); return; }

        _disableAdd = true;
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
            _addStatus = StatusMessage.Error(
                ex.InnerException?.Message.Contains("UNIQUE") == true
                    ? "同じ日付がすでに登録されています"
                    : $"エラー: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            _disableAdd = false;
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
            _addStatus = StatusMessage.Error($"削除に失敗しました: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>CSVファイルを選択し、全体休日を一括インポートする。</summary>
    private async Task ImportCsv()
    {
        _importStatus = null;

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
            _importStatus = StatusMessage.Error($"ファイル選択に失敗しました: {ex.Message}");
            return;
        }

        if (file is null)
            return;

        _disableImport = true;
        try
        {
            var csvText = await File.ReadAllTextAsync(file.FullPath);
            var parsed = HolidayCsvHelper.Parse(csvText);

            if (parsed.Dates.Count == 0)
            {
                _importStatus = StatusMessage.Error(
                    parsed.InvalidLineCount > 0
                        ? $"インポートできる日付がありませんでした（{parsed.InvalidLineCount}行が不正な形式です）"
                        : "インポートできる日付がありませんでした");
                return;
            }

            var imported = await HolidayRepo.CreateManyAsync(parsed.Dates.Select(d => new GlobalHoliday { Date = d }));
            var skipped = parsed.Dates.Count - imported;

            var message = $"{imported}件登録しました";
            if (skipped > 0)
                message += $"（{skipped}件は重複のためスキップ）";
            if (parsed.InvalidLineCount > 0)
                message += $"（{parsed.InvalidLineCount}行は不正な形式のためスキップ）";
            _importStatus = new StatusMessage(message, false);

            _holidays = await HolidayRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _importStatus = StatusMessage.Error($"インポートに失敗しました: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            _disableImport = false;
        }
    }

    /// <summary>日付文字列を表示用にフォーマットする。</summary>
    /// <param name="date">yyyy-MM-dd 形式の日付文字列。</param>
    /// <returns>yyyy/MM/dd 形式の文字列。</returns>
    private static string FormatDate(string date)
        => DateOnly.TryParse(date, out var d) ? d.ToString("yyyy/MM/dd") : date;
}
