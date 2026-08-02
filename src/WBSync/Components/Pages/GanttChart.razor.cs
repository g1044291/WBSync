using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WBSync.Helpers;
using WBSync.Models;
using WBSync.Services;

namespace WBSync.Components.Pages;

/// <summary>ガントチャート画面のコードビハインド。</summary>
public partial class GanttChart : IAsyncDisposable
{
    /// <summary>表示するプロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private string _projectName = string.Empty;
    private string _projectStartDate = string.Empty;
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);
    private List<TaskNode> _taskRoots = [];
    private List<WbsTask> _allTasks = [];
    private List<WbsTask> _allTasksWbsOrdered = [];
    private List<Assignee> _allAssignees = [];
    private Dictionary<int, string> _assigneeNames = [];
    private HashSet<DateOnly> _globalHolidays = [];
    private Dictionary<int, HashSet<DateOnly>> _assigneeHolidays = [];
    private HashSet<int> _warningTaskIds = [];
    private StatusMessage? _pageStatus;
    private int _hoveredRowIndex = -1;
    private bool _isDragging;
    private DotNetObjectReference<GanttChart>? _dotNetRef;
    private List<(int PredecessorId, int SuccessorId)> _dependencyPairs = [];
    private bool _linesDirty;
    private bool _showDependencyLines = true;

    #region モーダル状態

    private bool _isTaskModalOpen;
    private WbsTask? _taskModalTask;
    private int? _taskModalParentId;

    #endregion

    #region 右クリックメニュー状態

    private bool _ctxVisible;
    private double _ctxX;
    private double _ctxY;
    private TaskNode? _ctxNode;
    private bool _showCtxDeleteConfirm;

    private string _ctxDeleteMessage =>
        $"「{_ctxNode?.Task.Name}」およびその配下のタスクをすべて削除します。この操作は元に戻せません。";

    #endregion

    #region Chart scale & columns

    private ChartScale _scale = ChartScale.Day;
    private DateOnly _chartStart;
    private DateOnly _chartEnd;
    private List<ChartColumn> _chartColumns = [];

    #endregion

    #region Lifecycle

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var projects = await ProjectRepo.GetAllAsync();
        var project = projects.FirstOrDefault(p => p.Id == ProjectId);
        if (project is null) { Nav.NavigateTo("/"); return; }
        _projectName = project.Name;
        _projectStartDate = project.StartDate;

        var tasks = await TaskRepo.GetByProjectAsync(ProjectId);
        var assignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
        _allAssignees = assignees;
        _assigneeNames = assignees.ToDictionary(a => a.Id, a => a.Name);
        _allTasks = tasks;
        _taskRoots = TaskTreeHelper.BuildTree(tasks);
        _allTasksWbsOrdered = TaskTreeHelper.GetAllNodesInDisplayOrder(_taskRoots).Select(n => n.Task).ToList();

        var holidays = await GlobalHolidayRepo.GetAllAsync();
        _globalHolidays = holidays
            .Select(h => DateOnly.TryParse(h.Date, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToHashSet();

        await LoadAssigneeHolidaysAsync();
        _warningTaskIds = await ScheduleService.GetOverlappingTaskIdsAsync(ProjectId);
        BuildDependencyPairs();

        CalculateChartPeriod();
        _chartColumns = GanttChartLayoutHelper.BuildColumns(_scale, _chartStart, _chartEnd, _globalHolidays);
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initSplitter", "gantt-splitter", "task-pane", 320, 800);
            await JS.InvokeVoidAsync("initScrollSync", "task-pane-rows", "chart-pane");
            await JS.InvokeVoidAsync("initColNameResize", "col-name-resize", "task-pane", 120);
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("initSortable", "task-pane-rows", _dotNetRef);
            await JS.InvokeVoidAsync("initLeaderLineSync", "chart-pane", "task-pane-rows");
            await UpdateLeaderLinesAsync();
        }
        else if (_linesDirty)
        {
            _linesDirty = false;
            await UpdateLeaderLinesAsync();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        await JS.InvokeVoidAsync("disposeLeaderLines");
    }

    #endregion

    #region Scale management

    /// <summary>チャートのスケールを切り替える。</summary>
    /// <param name="scale">切り替え先のスケール。</param>
    private void SetScale(ChartScale scale)
    {
        _scale = scale;
        _chartColumns = GanttChartLayoutHelper.BuildColumns(_scale, _chartStart, _chartEnd, _globalHolidays);
        _linesDirty = true;
    }

    #endregion

    #region Chart period

    /// <summary>チャートの表示期間をタスクの開始日・終了日から算出する。</summary>
    private void CalculateChartPeriod()
    {
        _chartStart = DateOnly.TryParse(_projectStartDate, out var s)
            ? s
            : DateOnly.FromDateTime(DateTime.Today);

        var endDates = TaskTreeHelper.GetAllLeafNodes(_taskRoots)
            .Select(n => n.Task.EndDate)
            .Where(d => !string.IsNullOrEmpty(d))
            .Select(d => DateOnly.TryParse(d, out var date) ? date : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        var lastEndDate = endDates.Count > 0 ? endDates.Max() : _chartStart;
        _chartEnd = lastEndDate.AddDays(30);
    }

    #endregion

    #region 個人休日ロード

    /// <summary>全担当者の個人休日を読み込んで <see cref="_assigneeHolidays"/> に格納する。</summary>
    private async Task LoadAssigneeHolidaysAsync()
    {
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

    #endregion

    #region 表示ヘルパー

    /// <summary>ヘッダーセルに適用する CSS クラス名を返す。</summary>
    /// <param name="col">対象チャート列。</param>
    private string GetHeaderCellClass(ChartColumn col)
    {
        if (IsToday(col)) return "col-today";
        if (col.IsWeekend || col.IsHoliday) return "col-weekend";
        return string.Empty;
    }

    /// <summary>チャートセルに適用する CSS クラス名を返す。</summary>
    /// <param name="node">対象タスクノード。</param>
    /// <param name="col">対象チャート列。</param>
    /// <returns>CSS クラス名。該当なしの場合は空文字。</returns>
    private string GetCellClass(TaskNode node, ChartColumn col)
    {
        if (IsToday(col)) return "col-today";
        if (col.IsWeekend || col.IsHoliday) return "col-weekend";
        if (_scale == ChartScale.Day
            && !node.HasChildren
            && node.Task.AssigneeId.HasValue
            && _assigneeHolidays.TryGetValue(node.Task.AssigneeId.Value, out var ah)
            && ah.Contains(col.Date))
            return "col-personal-holiday";
        return string.Empty;
    }

    /// <summary>指定列が今日を含むかどうかを返す。</summary>
    /// <param name="col">対象チャート列。</param>
    private bool IsToday(ChartColumn col) => _scale switch
    {
        ChartScale.Day => col.Date == _today,
        ChartScale.Week => col.Date <= _today && _today < col.Date.AddDays(7),
        ChartScale.Month => col.Date.Year == _today.Year && col.Date.Month == _today.Month,
        _ => false
    };

    /// <summary>担当者IDから担当者名を取得する。</summary>
    /// <param name="assigneeId">担当者ID。未割り当ての場合は <see langword="null"/>。</param>
    /// <returns>担当者名。未割り当てまたは不明の場合は "-"。</returns>
    private string GetAssigneeName(int? assigneeId)
        => assigneeId.HasValue && _assigneeNames.TryGetValue(assigneeId.Value, out var name) ? name : "-";

    /// <summary>日付文字列をガントチャート表示用にフォーマットする。</summary>
    /// <param name="date">yyyy-MM-dd 形式の日付文字列。</param>
    /// <returns>M/d 形式の文字列。空の場合は "-"。</returns>
    private static string FormatDate(string? date)
    {
        if (string.IsNullOrEmpty(date)) return "-";
        return DateOnly.TryParse(date, out var d) ? d.ToString("M/d") : date;
    }

    /// <summary>子タスクの見積工数合計が親タスクの見積工数を超えている場合の警告ツールチップ文言を返す。</summary>
    /// <param name="node">対象タスクノード（<see cref="TaskNode.HasWorkDaysOverflow"/> が <see langword="true"/> であること）。</param>
    /// <returns>具体的な数値差を含む警告メッセージ。</returns>
    private static string GetWorkDaysOverflowTooltip(TaskNode node)
        => $"子タスクの見積工数合計（{node.DirectChildrenWorkDaysSum:0.##}人日）が" +
           $"親タスクの見積工数（{node.Task.WorkDays:0.##}人日）を" +
           $"{node.WorkDaysOverflowAmount:0.##}人日超過しています";

    #endregion

    #region D&D 並び替え

    /// <summary>SortableJS からドロップ完了時に呼び出され、兄弟タスクの表示順を DB に保存する。</summary>
    /// <param name="taskIds">新しい順序の兄弟タスクIDリスト。</param>
    [JSInvokable]
    public async Task OnSortOrderChanged(int[] taskIds)
    {
        for (var i = 0; i < taskIds.Length; i++)
            await TaskRepo.UpdateSortOrderAsync(taskIds[i], i);

        _allTasks = await TaskRepo.GetByProjectAsync(ProjectId);
        _taskRoots = TaskTreeHelper.BuildTree(_allTasks);
        BuildDependencyPairs();
        _linesDirty = true;
        await InvokeAsync(StateHasChanged);
    }

    #endregion

    #region ナビゲーション・操作

    /// <summary>プロジェクト一覧に戻る。</summary>
    private void GoBack() => Nav.NavigateTo("/");

    /// <summary>担当者設定画面に遷移する。</summary>
    private void GoToAssignees() => Nav.NavigateTo($"/assignees/{ProjectId}");

    /// <summary>ルートへのタスク追加モーダルを開く。</summary>
    private void AddTask()
    {
        _taskModalTask = null;
        _taskModalParentId = null;
        _isTaskModalOpen = true;
    }

    /// <summary>タスク行クリック時にタスク編集モーダルを開く。</summary>
    /// <param name="node">クリックされたタスクノード。</param>
    private void OnTaskRowClick(TaskNode node)
    {
        _taskModalTask = node.Task;
        _taskModalParentId = null;
        _isTaskModalOpen = true;
    }

    #endregion

    #region 右クリックメニュー

    /// <summary>右クリックメニューを表示する。</summary>
    /// <param name="e">マウスイベント。</param>
    /// <param name="node">右クリックされたタスクノード。</param>
    private void ShowContextMenu(MouseEventArgs e, TaskNode node)
    {
        _ctxX = e.ClientX;
        _ctxY = e.ClientY;
        _ctxNode = node;
        _ctxVisible = true;
    }

    /// <summary>右クリックメニューを閉じる。</summary>
    private void CloseContextMenu() => _ctxVisible = false;

    /// <summary>選択中タスクの子タスク追加モーダルを開く。</summary>
    private void AddChildTask()
    {
        CloseContextMenu();
        _taskModalTask = null;
        _taskModalParentId = _ctxNode!.Task.Id;
        _isTaskModalOpen = true;
    }

    /// <summary>選択中タスクと同階層へのタスク追加モーダルを開く。</summary>
    private void AddSiblingTask()
    {
        CloseContextMenu();
        _taskModalTask = null;
        _taskModalParentId = _ctxNode!.Task.ParentId;
        _isTaskModalOpen = true;
    }

    /// <summary>右クリックメニューからタスク削除の確認ダイアログを表示する。</summary>
    private void ShowCtxDeleteConfirm()
    {
        _ctxVisible = false;
        _showCtxDeleteConfirm = true;
    }

    /// <summary>タスク削除の確認ダイアログで「削除」が押されたときの処理。</summary>
    private async Task HandleCtxDeleteConfirm()
    {
        if (_ctxNode is null) return;
        _showCtxDeleteConfirm = false;
        try
        {
            await TaskRepo.DeleteAsync(_ctxNode.Task.Id);
            _ctxNode = null;
            await ReloadTasksAsync();
        }
        catch (Exception ex)
        {
            _ctxNode = null;
            _pageStatus = StatusMessage.Error($"削除に失敗しました: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>タスク編集モーダルを閉じる。</summary>
    private void HandleTaskModalClose() => _isTaskModalOpen = false;

    /// <summary>タスク保存完了時にリストを更新する。</summary>
    /// <param name="_">保存されたタスク（未使用）。</param>
    private async Task HandleTaskSaved(WbsTask _)
    {
        _isTaskModalOpen = false;
        await ReloadTasksAsync();
    }

    /// <summary>タスク削除完了時にリストを更新する。</summary>
    /// <param name="_">削除されたタスクID（未使用）。</param>
    private async Task HandleTaskDeleted(int _)
    {
        _isTaskModalOpen = false;
        await ReloadTasksAsync();
    }

    /// <summary>タスク一覧・チャート列を再読み込みする。</summary>
    private async Task ReloadTasksAsync()
    {
        _allTasks = await TaskRepo.GetByProjectAsync(ProjectId);
        _taskRoots = TaskTreeHelper.BuildTree(_allTasks);
        _allTasksWbsOrdered = TaskTreeHelper.GetAllNodesInDisplayOrder(_taskRoots).Select(n => n.Task).ToList();
        _warningTaskIds = await ScheduleService.GetOverlappingTaskIdsAsync(ProjectId);
        BuildDependencyPairs();
        CalculateChartPeriod();
        _chartColumns = GanttChartLayoutHelper.BuildColumns(_scale, _chartStart, _chartEnd, _globalHolidays);
        _linesDirty = true;
    }

    #endregion

    #region 行ホバー

    /// <summary>ホバー行インデックスを更新する。ドラッグ中は無視する。</summary>
    /// <param name="index">ホバー中の行インデックス。-1 でホバーなし。</param>
    private void SetHoveredRow(int index)
    {
        if (_isDragging) return;
        _hoveredRowIndex = index;
    }

    /// <summary>SortableJS からドラッグ開始・終了を受け取り再レンダリング競合を防ぐ。</summary>
    /// <param name="dragging">ドラッグ中なら <see langword="true"/>。</param>
    [JSInvokable]
    public void SetDragging(bool dragging) => _isDragging = dragging;

    #endregion

    #region タスクバー描画

    /// <summary>タスクバーの CSS インラインスタイル（left・width）を返す。描画不可の場合は <see langword="null"/>。</summary>
    /// <param name="node">対象タスクノード。</param>
    private string? GetBarStyle(TaskNode node)
    {
        var startStr = node.DisplayStartDate;
        var endStr = node.DisplayEndDate;
        if (string.IsNullOrEmpty(startStr) || string.IsNullOrEmpty(endStr)) return null;
        if (!DateOnly.TryParse(startStr, out var start)) return null;
        if (!DateOnly.TryParse(endStr, out var end)) return null;
        if (start > end) return null;

        var left = GanttChartLayoutHelper.GetPixelOffset(_scale, _chartStart, start);
        var width = GanttChartLayoutHelper.GetPixelOffset(_scale, _chartStart, end.AddDays(1)) - left;
        if (width <= 0) return null;

        return $"left:{left:F1}px;width:{width:F1}px";
    }

    #endregion

    #region ツリー操作

    /// <summary>ノードの展開・折り畳みを切り替える。</summary>
    /// <param name="node">対象ノード。</param>
    private void ToggleExpand(TaskNode node)
    {
        node.IsExpanded = !node.IsExpanded;
        _linesDirty = true;
    }

    #endregion

    #region 先行・後続タスク矢印

    /// <summary><see cref="_allTasks"/> から先行・後続タスクIDのペア一覧を再構築する。</summary>
    private void BuildDependencyPairs()
        => _dependencyPairs = _allTasks
            .Where(t => t.PredecessorId.HasValue)
            .Select(t => (PredecessorId: t.PredecessorId!.Value, SuccessorId: t.Id))
            .ToList();

    /// <summary>JS側のLeaderLineを現在の先行・後続ペアで再構築する。表示OFF時は空配列を渡して矢印を消す。</summary>
    private async Task UpdateLeaderLinesAsync()
        => await JS.InvokeVoidAsync("updateLeaderLines", "chart-pane",
            _showDependencyLines
                ? _dependencyPairs.Select(p => new[] { p.PredecessorId, p.SuccessorId })
                : []);

    /// <summary>先行・後続タスクの矢印表示のON/OFFを切り替える。</summary>
    private async Task ToggleDependencyLines()
    {
        _showDependencyLines = !_showDependencyLines;
        await UpdateLeaderLinesAsync();
    }

    #endregion
}
