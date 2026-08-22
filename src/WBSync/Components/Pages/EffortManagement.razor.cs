using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WBSync.Helpers;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>工数管理画面のコードビハインド。</summary>
public partial class EffortManagement
{
    /// <summary>表示するプロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private string _projectName = string.Empty;
    private List<WbsTask> _allTasks = [];
    private List<TaskNode> _taskRoots = [];
    private List<WbsTask> _allTasksWbsOrdered = [];
    private List<Assignee> _allAssignees = [];
    private Dictionary<int, string> _assigneeNames = [];
    private List<WorkLog> _allWorkLogs = [];
    private Dictionary<int, EffortAggregate> _aggregates = [];
    private HashSet<DateOnly> _globalHolidays = [];
    private Dictionary<int, IReadOnlySet<DateOnly>> _assigneeHolidays = [];
    private StatusMessage? _pageStatus;

    #region ツリー表示状態

    private readonly HashSet<int> _collapsedTaskIds = [];
    private readonly HashSet<int> _expandedRowIds = [];

    #endregion

    #region フィルター・並び替え状態

    private Assignee? _filterAssignee;
    private DelayFilter _filterDelay = DelayFilter.All;
    private SortMode _sortMode = SortMode.WbsOrder;

    private static readonly List<DelayFilter> _delayFilterOptions = [DelayFilter.All, DelayFilter.Delayed, DelayFilter.NotDelayed];
    private static readonly List<SortMode> _sortModeOptions = [SortMode.WbsOrder, SortMode.AssigneeName, SortMode.DelayDays];

    #endregion

    #region タスク編集モーダル状態

    private bool _isTaskModalOpen;
    private WbsTask? _taskModalTask;

    #endregion

    #region 実績ログ編集状態

    private int? _editingLogId;
    private DateOnly? _editLogDate;
    private Assignee? _editLogAssignee;
    private string _editLogMinutesStr = string.Empty;
    private string _editLogComment = string.Empty;
    private bool _disableEditLog;
    private StatusMessage? _editLogStatus;

    private readonly Dictionary<int, NewLogFormState> _newLogForms = [];

    /// <summary>行展開パネルの実績ログ追加フォームの入力状態。タスクIDごとに独立させる。</summary>
    private sealed class NewLogFormState
    {
        public DateOnly? Date;
        public Assignee? Assignee;
        public string MinutesStr = string.Empty;
        public string Comment = string.Empty;
        public bool DisableAdd;
        public StatusMessage? Status;
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var projects = await ProjectRepo.GetAllAsync();
        var project = projects.FirstOrDefault(p => p.Id == ProjectId);
        if (project is null) { Nav.NavigateTo("/"); return; }
        _projectName = project.Name;

        await ReloadAllAsync();
    }

    #endregion

    #region データロード

    /// <summary>タスク・担当者・実績ログ・集計をすべて再読み込みする。</summary>
    private async Task ReloadAllAsync()
    {
        _allTasks = await TaskRepo.GetByProjectAsync(ProjectId);
        _taskRoots = TaskTreeHelper.BuildTree(_allTasks);
        _allTasksWbsOrdered = TaskTreeHelper.GetAllNodesInDisplayOrder(_taskRoots).Select(n => n.Task).ToList();

        _allAssignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
        _assigneeNames = _allAssignees.ToDictionary(a => a.Id, a => a.Name);

        await LoadHolidaysAsync();
        await ReloadWorkLogsAndAggregatesAsync();
    }

    /// <summary>全体休日・全担当者の個人休日を読み込む（実績ログ追加フォームの初期日付算出に使用）。</summary>
    private async Task LoadHolidaysAsync()
    {
        var holidays = await GlobalHolidayRepo.GetAllAsync();
        _globalHolidays = holidays
            .Select(h => DateOnly.TryParse(h.Date, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToHashSet();

        _assigneeHolidays = [];
        foreach (var assignee in _allAssignees)
        {
            var ah = await AssigneeHolidayRepo.GetByAssigneeAsync(assignee.Id);
            _assigneeHolidays[assignee.Id] = ah
                .Select(h => DateOnly.TryParse(h.Date, out var d) ? d : (DateOnly?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToHashSet();
        }
    }

    /// <summary>実績ログと集計のみを再読み込みする（タスク行自体は変わらない操作向けの軽量版）。</summary>
    private async Task ReloadWorkLogsAndAggregatesAsync()
    {
        _allWorkLogs = await WorkLogRepo.GetByProjectAsync(ProjectId);
        var actualByTaskId = _allWorkLogs
            .GroupBy(w => w.TaskId)
            .ToDictionary(g => g.Key, g => PersonDayHelper.ToPersonDays(g.Sum(w => w.Minutes)));

        var delayByTaskId = await ScheduleService.CalcDelayDaysAsync(ProjectId, _allTasks);

        _aggregates = EffortTreeHelper.BuildAggregates(_taskRoots, actualByTaskId, delayByTaskId);
    }

    #endregion

    #region 表示ヘルパー

    /// <summary>担当者IDから担当者名を取得する。</summary>
    /// <param name="assigneeId">担当者ID。未割り当ての場合は <see langword="null"/>。</param>
    /// <returns>担当者名。未割り当てまたは不明の場合は "-"。</returns>
    private string GetAssigneeName(int? assigneeId)
        => assigneeId.HasValue && _assigneeNames.TryGetValue(assigneeId.Value, out var name) ? name : "-";

    /// <summary>工数（人日）を表示用にフォーマットする。単位「人日」を付与する。</summary>
    /// <param name="value">工数（人日）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>「n人日」形式の文字列。算出不可の場合は "-"。</returns>
    private static string FormatWorkDays(double? value)
        => value.HasValue ? $"{PersonDayHelper.FormatWorkDays(value)}人日" : "-";

    /// <summary>実績ログの作業時間を分表示に時間換算の参考値を併記してフォーマットする。</summary>
    /// <param name="minutes">作業時間（分）。</param>
    /// <returns>「n分（h.hh）」形式の文字列（例: "480分（8.0h）"）。</returns>
    private static string FormatMinutesWithHours(int minutes)
        => $"{minutes}分（{(minutes / 60.0).ToString("0.0")}h）";

    /// <summary>日付文字列を表示用にフォーマットする。</summary>
    /// <param name="date">yyyy-MM-dd 形式の日付文字列。</param>
    /// <returns>M/d 形式の文字列。空の場合は "-"。</returns>
    private static string FormatDate(string? date)
    {
        if (string.IsNullOrEmpty(date)) return "-";
        return DateOnly.TryParse(date, out var d) ? d.ToString("M/d") : date;
    }

    /// <summary>yyyy-MM-dd 形式の日付文字列を <see cref="DateOnly"/> にパースする。</summary>
    /// <param name="date">日付文字列。</param>
    /// <returns>パース成功時は日付、失敗時は <see langword="null"/>。</returns>
    private static DateOnly? ParseDate(string? date) => DateOnly.TryParse(date, out var d) ? d : null;

    /// <summary>遅れ日数を表示用にフォーマットする。単位「日」を付与する。</summary>
    /// <param name="delayDays">遅れ日数。算出不可の場合は <see langword="null"/>。</param>
    /// <returns>プラスは "+n日"、マイナスは "n日"、算出不可は "-"。</returns>
    private static string FormatDelayDays(int? delayDays) => delayDays switch
    {
        null => "-",
        > 0 => $"+{delayDays}日",
        _ => $"{delayDays}日"
    };

    /// <summary>遅れフィルターの表示名を返す。</summary>
    /// <param name="filter">遅れフィルター。</param>
    private static string DisplayDelayFilter(DelayFilter filter) => filter switch
    {
        DelayFilter.Delayed => "遅れあり",
        DelayFilter.NotDelayed => "遅れなし",
        _ => "すべて"
    };

    /// <summary>並び替えキーの表示名を返す。</summary>
    /// <param name="sortMode">並び替えキー。</param>
    private static string DisplaySortMode(SortMode sortMode) => sortMode switch
    {
        SortMode.AssigneeName => "担当者順",
        SortMode.DelayDays => "遅れ順",
        _ => "WBS順"
    };

    #endregion

    #region ツリー折りたたみ

    /// <summary>タスクの子ノード折りたたみ状態を切り替える。</summary>
    /// <param name="node">対象ノード。</param>
    private void ToggleCollapse(TaskNode node)
    {
        if (!_collapsedTaskIds.Add(node.Task.Id))
            _collapsedTaskIds.Remove(node.Task.Id);
    }

    #endregion

    #region タスク編集モーダル

    /// <summary>タスク編集モーダルを開く。</summary>
    /// <param name="task">編集対象タスク。</param>
    private void OpenEditModal(WbsTask task)
    {
        _taskModalTask = task;
        _isTaskModalOpen = true;
    }

    /// <summary>タスク編集モーダルを閉じる。</summary>
    private void HandleTaskModalClose() => _isTaskModalOpen = false;

    /// <summary>タスク保存完了時に全データを再読み込みする。</summary>
    /// <param name="_">保存されたタスク（未使用）。</param>
    private async Task HandleTaskSaved(WbsTask _)
    {
        _isTaskModalOpen = false;
        await ReloadAllAsync();
    }

    /// <summary>タスク削除完了時に全データを再読み込みする。</summary>
    /// <param name="_">削除されたタスクID（未使用）。</param>
    private async Task HandleTaskDeleted(int _)
    {
        _isTaskModalOpen = false;
        await ReloadAllAsync();
    }

    #endregion

    #region 実績ログ行展開

    /// <summary>タスクの実績ログ行展開状態を切り替える。初回展開時は追加フォームを初期化する。</summary>
    /// <param name="task">対象タスク。</param>
    private void ToggleRowExpand(WbsTask task)
    {
        if (!_expandedRowIds.Add(task.Id))
        {
            _expandedRowIds.Remove(task.Id);
            return;
        }

        if (!_newLogForms.ContainsKey(task.Id))
        {
            _newLogForms[task.Id] = new NewLogFormState
            {
                Date = ComputeDefaultLogDate(task),
                Assignee = _allAssignees.FirstOrDefault(a => a.Id == task.AssigneeId)
            };
        }
    }

    /// <summary>
    /// 実績ログ追加フォームの初期日付を算出する。
    /// 既存の実績ログがある場合はその最大日付の翌稼働日、まだ実績がない場合はタスクの開始日を返す。
    /// </summary>
    /// <param name="task">対象タスク。</param>
    /// <returns>算出された初期日付。開始日も未設定の場合は <see langword="null"/>。</returns>
    private DateOnly? ComputeDefaultLogDate(WbsTask task)
    {
        var lastLogDate = _allWorkLogs
            .Where(w => w.TaskId == task.Id)
            .Select(w => ParseDate(w.Date))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .OrderByDescending(d => d)
            .FirstOrDefault();

        if (lastLogDate != default)
            return WorkdayHelper.GetNextWorkday(lastLogDate.AddDays(1), _globalHolidays, _assigneeHolidays, task.AssigneeId);

        return ParseDate(task.StartDate);
    }

    /// <summary>実績ログ追加フォームの分入力欄でEnterキーが押されたとき、「追加」ボタンと同じ処理を実行する。</summary>
    /// <param name="e">キーボードイベント引数。</param>
    /// <param name="task">対象タスク。</param>
    private async Task HandleMinutesKeyDown(KeyboardEventArgs e, WbsTask task)
    {
        if (e.Key == "Enter")
            await AddLog(task);
    }

    /// <summary>実績ログを追加する。</summary>
    /// <param name="task">対象タスク。</param>
    private async Task AddLog(WbsTask task)
    {
        if (!_newLogForms.TryGetValue(task.Id, out var form)) return;
        form.Status = null;

        if (form.Date is null) { form.Status = StatusMessage.Error("日付を入力してください"); return; }
        if (form.Assignee is null) { form.Status = StatusMessage.Error("担当者を選択してください"); return; }
        if (!int.TryParse(form.MinutesStr, out var minutes) || minutes <= 0)
        {
            form.Status = StatusMessage.Error("作業時間は1以上の整数（分）で入力してください");
            return;
        }

        form.DisableAdd = true;
        try
        {
            await WorkLogRepo.CreateAsync(new WorkLog
            {
                TaskId = task.Id,
                AssigneeId = form.Assignee?.Id,
                Date = form.Date.Value.ToString("yyyy-MM-dd"),
                Minutes = minutes,
                Comment = string.IsNullOrWhiteSpace(form.Comment) ? null : form.Comment
            });
            form.MinutesStr = string.Empty;
            form.Comment = string.Empty;
            await ReloadWorkLogsAndAggregatesAsync();
            form.Date = ComputeDefaultLogDate(task);
        }
        catch (Exception ex)
        {
            form.Status = StatusMessage.Error($"追加に失敗しました: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            form.DisableAdd = false;
        }
    }

    /// <summary>指定の実績ログをインライン編集モードにする。</summary>
    /// <param name="log">対象の実績ログ。</param>
    private void StartEditingLog(WorkLog log)
    {
        _editingLogId = log.Id;
        _editLogDate = ParseDate(log.Date);
        _editLogAssignee = _allAssignees.FirstOrDefault(a => a.Id == log.AssigneeId);
        _editLogMinutesStr = log.Minutes.ToString();
        _editLogComment = log.Comment ?? string.Empty;
        _editLogStatus = null;
    }

    /// <summary>実績ログのインライン編集をキャンセルする。</summary>
    private void CancelEditingLog()
    {
        _editingLogId = null;
        _editLogStatus = null;
    }

    /// <summary>実績ログを保存する。</summary>
    /// <param name="log">保存対象の実績ログ。</param>
    private async Task SaveLog(WorkLog log)
    {
        _editLogStatus = null;
        if (_editLogDate is null) { _editLogStatus = StatusMessage.Error("日付を入力してください"); return; }
        if (_editLogAssignee is null) { _editLogStatus = StatusMessage.Error("担当者を選択してください"); return; }
        if (!int.TryParse(_editLogMinutesStr, out var minutes) || minutes <= 0)
        {
            _editLogStatus = StatusMessage.Error("作業時間は1以上の整数（分）で入力してください");
            return;
        }

        _disableEditLog = true;
        try
        {
            log.Date = _editLogDate.Value.ToString("yyyy-MM-dd");
            log.AssigneeId = _editLogAssignee?.Id;
            log.Minutes = minutes;
            log.Comment = string.IsNullOrWhiteSpace(_editLogComment) ? null : _editLogComment;
            await WorkLogRepo.UpdateAsync(log);
            _editingLogId = null;
            await ReloadWorkLogsAndAggregatesAsync();
        }
        catch (Exception ex)
        {
            _editLogStatus = StatusMessage.Error($"保存に失敗しました: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            _disableEditLog = false;
        }
    }

    /// <summary>実績ログを削除する（確認ダイアログなし、即時削除）。</summary>
    /// <param name="log">削除対象の実績ログ。</param>
    private async Task DeleteLog(WorkLog log)
    {
        try
        {
            await WorkLogRepo.DeleteAsync(log.Id);
            await ReloadWorkLogsAndAggregatesAsync();
        }
        catch (Exception ex)
        {
            _pageStatus = StatusMessage.Error($"削除に失敗しました: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    #endregion

    #region ナビゲーション

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");

    #endregion
}
