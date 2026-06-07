using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WBSync.Models;
using WBSync.Services;

namespace WBSync.Components.Pages;

/// <summary>ガントチャート画面のコードビハインド。</summary>
public partial class GanttChart
{
    /// <summary>表示するプロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private string _projectName = string.Empty;
    private string _projectStartDate = string.Empty;
    private List<TaskNode> _taskRoots = [];
    private List<WbsTask> _allTasks = [];
    private List<Assignee> _allAssignees = [];
    private Dictionary<int, string> _assigneeNames = [];
    private HashSet<DateOnly> _globalHolidays = [];
    private Dictionary<int, HashSet<DateOnly>> _assigneeHolidays = [];
    private HashSet<int> _warningTaskIds = [];

    // ===== モーダル状態 =====

    private bool _isTaskModalOpen;
    private WbsTask? _taskModalTask;
    private int? _taskModalParentId;

    // ===== 右クリックメニュー状態 =====

    private bool _ctxVisible;
    private double _ctxX;
    private double _ctxY;
    private TaskNode? _ctxNode;
    private bool _showCtxDeleteConfirm;

    private string _ctxDeleteMessage =>
        $"「{_ctxNode?.Task.Name}」およびその配下のタスクをすべて削除します。この操作は元に戻せません。";

    // ===== Chart scale & columns =====

    /// <summary>チャートの時間軸スケール。</summary>
    private enum ChartScale { Day, Week, Month }
    private ChartScale _scale = ChartScale.Day;

    private DateOnly _chartStart;
    private DateOnly _chartEnd;

    /// <summary>チャートの列定義。</summary>
    private record ChartColumn(string Label, DateOnly Date, bool IsWeekend, bool IsHoliday = false);
    private List<ChartColumn> _chartColumns = [];

    // ===== Lifecycle =====

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
        _taskRoots = BuildTree(tasks);

        var holidays = await GlobalHolidayRepo.GetAllAsync();
        _globalHolidays = holidays
            .Select(h => DateOnly.TryParse(h.Date, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToHashSet();

        await LoadAssigneeHolidaysAsync();
        _warningTaskIds = await ScheduleService.GetOverlappingTaskIdsAsync(ProjectId);

        CalculateChartPeriod();
        BuildChartColumns();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initSplitter", "gantt-splitter", "task-pane", 320, 800);
            await JS.InvokeVoidAsync("initScrollSync", "task-pane-rows", "chart-pane");
        }
    }

    // ===== Scale management =====

    /// <summary>チャートのスケールを切り替える。</summary>
    /// <param name="scale">切り替え先のスケール。</param>
    private void SetScale(ChartScale scale)
    {
        _scale = scale;
        BuildChartColumns();
    }

    // ===== Chart period & columns =====

    /// <summary>チャートの表示期間をタスクの開始日・終了日から算出する。</summary>
    private void CalculateChartPeriod()
    {
        _chartStart = DateOnly.TryParse(_projectStartDate, out var s)
            ? s
            : DateOnly.FromDateTime(DateTime.Today);

        var endDates = GetAllLeafNodes(_taskRoots)
            .Select(n => n.Task.EndDate)
            .Where(d => !string.IsNullOrEmpty(d))
            .Select(d => DateOnly.TryParse(d, out var date) ? date : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        var lastEndDate = endDates.Count > 0 ? endDates.Max() : _chartStart;
        _chartEnd = lastEndDate.AddDays(30);
    }

    /// <summary>現在のスケールに応じてチャート列一覧を構築する。</summary>
    private void BuildChartColumns()
    {
        _chartColumns = _scale switch
        {
            ChartScale.Day => BuildDayColumns(),
            ChartScale.Week => BuildWeekColumns(),
            ChartScale.Month => BuildMonthColumns(),
            _ => []
        };
    }

    /// <summary>日スケールの列一覧を構築する。</summary>
    private List<ChartColumn> BuildDayColumns()
    {
        var cols = new List<ChartColumn>();
        for (var d = _chartStart; d <= _chartEnd; d = d.AddDays(1))
        {
            var isWeekend = d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
            var isHoliday = _globalHolidays.Contains(d);
            cols.Add(new ChartColumn(d.ToString("M/d"), d, isWeekend, isHoliday));
        }
        return cols;
    }

    /// <summary>週スケールの列一覧を構築する。</summary>
    private List<ChartColumn> BuildWeekColumns()
    {
        var cols = new List<ChartColumn>();
        var weekStart = _chartStart;
        while (weekStart.DayOfWeek != DayOfWeek.Monday)
            weekStart = weekStart.AddDays(-1);

        for (var d = weekStart; d <= _chartEnd; d = d.AddDays(7))
        {
            var weekEnd = d.AddDays(6);
            cols.Add(new ChartColumn($"{d:M/d}〜{weekEnd:M/d}", d, false));
        }
        return cols;
    }

    /// <summary>月スケールの列一覧を構築する。</summary>
    private List<ChartColumn> BuildMonthColumns()
    {
        var cols = new List<ChartColumn>();
        var monthStart = new DateOnly(_chartStart.Year, _chartStart.Month, 1);
        var chartEndMonth = new DateOnly(_chartEnd.Year, _chartEnd.Month, 1);

        for (var d = monthStart; d <= chartEndMonth; d = d.AddMonths(1))
            cols.Add(new ChartColumn($"{d:M月}", d, false));

        return cols;
    }

    /// <summary>指定ノード群からリーフノードをすべて列挙する。</summary>
    /// <param name="nodes">検索対象のノード群。</param>
    private static IEnumerable<TaskNode> GetAllLeafNodes(List<TaskNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!node.HasChildren)
                yield return node;
            else
                foreach (var leaf in GetAllLeafNodes(node.Children))
                    yield return leaf;
        }
    }

    // ===== ツリー構築 =====

    /// <summary>フラットなタスクリストからツリー構造を構築する。</summary>
    /// <param name="tasks">対象タスクのリスト。</param>
    private static List<TaskNode> BuildTree(List<WbsTask> tasks)
    {
        var nodeMap = tasks.ToDictionary(t => t.Id, t => new TaskNode { Task = t });
        var roots = new List<TaskNode>();

        foreach (var task in tasks.OrderBy(t => t.SortOrder))
        {
            if (task.ParentId is null)
                roots.Add(nodeMap[task.Id]);
            else if (nodeMap.TryGetValue(task.ParentId.Value, out var parent))
                parent.Children.Add(nodeMap[task.Id]);
        }

        SetLevels(roots, 0);
        return roots;
    }

    /// <summary>ノードの階層レベルを再帰的に設定する。</summary>
    /// <param name="nodes">対象ノード群。</param>
    /// <param name="level">現在の階層レベル。</param>
    private static void SetLevels(List<TaskNode> nodes, int level)
    {
        foreach (var node in nodes)
        {
            node.Level = level;
            SetLevels(node.Children, level + 1);
        }
    }

    /// <summary>展開状態を考慮して表示対象のノードを列挙する。</summary>
    /// <param name="roots">ルートノード群。</param>
    private static IEnumerable<TaskNode> GetVisibleNodes(List<TaskNode> roots)
    {
        foreach (var node in roots)
        {
            yield return node;
            if (node.IsExpanded && node.HasChildren)
                foreach (var child in GetVisibleNodes(node.Children))
                    yield return child;
        }
    }

    /// <summary>ノードの展開・折り畳みを切り替える。</summary>
    /// <param name="node">対象ノード。</param>
    private void ToggleExpand(TaskNode node) => node.IsExpanded = !node.IsExpanded;

    // ===== 個人休日ロード =====

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

    // ===== 表示ヘルパー =====

    /// <summary>チャートセルに適用する CSS クラス名を返す。</summary>
    /// <param name="node">対象タスクノード。</param>
    /// <param name="col">対象チャート列。</param>
    /// <returns>CSS クラス名。該当なしの場合は空文字。</returns>
    private string GetCellClass(TaskNode node, ChartColumn col)
    {
        if (col.IsWeekend || col.IsHoliday)
            return "col-weekend";
        if (_scale == ChartScale.Day
            && !node.HasChildren
            && node.Task.AssigneeId.HasValue
            && _assigneeHolidays.TryGetValue(node.Task.AssigneeId.Value, out var ah)
            && ah.Contains(col.Date))
            return "col-personal-holiday";
        return string.Empty;
    }

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

    // ===== ナビゲーション・操作 =====

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

    // ===== 右クリックメニュー =====

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
        await TaskRepo.DeleteAsync(_ctxNode.Task.Id);
        _ctxNode = null;
        await ReloadTasksAsync();
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
        _taskRoots = BuildTree(_allTasks);
        _warningTaskIds = await ScheduleService.GetOverlappingTaskIdsAsync(ProjectId);
        CalculateChartPeriod();
        BuildChartColumns();
    }

    // ===== タスクバー描画 =====

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

        var left = GetPixelOffset(start);
        var width = GetPixelOffset(end.AddDays(1)) - left;
        if (width <= 0) return null;

        return $"left:{left:F1}px;width:{width:F1}px";
    }

    /// <summary>指定日のチャート左端からのピクセルオフセットを返す。</summary>
    /// <param name="date">対象日付。</param>
    private double GetPixelOffset(DateOnly date) => _scale switch
    {
        ChartScale.Day => (date.DayNumber - _chartStart.DayNumber) * 32.0,
        ChartScale.Week => (date.DayNumber - GetWeekStart(_chartStart).DayNumber) * (80.0 / 7.0),
        ChartScale.Month => GetMonthPixelOffset(date),
        _ => 0
    };

    /// <summary>月スケール用のピクセルオフセットを計算する。</summary>
    /// <param name="date">対象日付。</param>
    private double GetMonthPixelOffset(DateOnly date)
    {
        var origin = new DateOnly(_chartStart.Year, _chartStart.Month, 1);
        var cur = origin;
        var px = 0.0;
        while (new DateOnly(date.Year, date.Month, 1) > cur)
        {
            px += 90.0;
            cur = cur.AddMonths(1);
        }
        px += (date.Day - 1) * (90.0 / DateTime.DaysInMonth(date.Year, date.Month));
        return px;
    }

    /// <summary>指定日を含む週の月曜日を返す。</summary>
    /// <param name="date">起点の日付。</param>
    private static DateOnly GetWeekStart(DateOnly date)
    {
        var d = date;
        while (d.DayOfWeek != DayOfWeek.Monday)
            d = d.AddDays(-1);
        return d;
    }

    // ===== TaskNode =====

    /// <summary>ガントチャートのツリー表示用ノード。</summary>
    private class TaskNode
    {
        /// <summary>対応する WBS タスク。</summary>
        public WbsTask Task { get; init; } = null!;

        /// <summary>子ノードのコレクション。</summary>
        public List<TaskNode> Children { get; } = [];

        /// <summary>ルートから数えた階層レベル（0 始まり）。</summary>
        public int Level { get; set; }

        /// <summary>子ノードが展開表示されているかどうか。</summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>子ノードを持つかどうか。</summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>表示用の開始日。親タスクの場合は子の最小値を動的に返す。</summary>
        public string? DisplayStartDate => HasChildren
            ? Children.Select(c => c.DisplayStartDate).Where(d => d is not null).Min()
            : Task.StartDate;

        /// <summary>表示用の終了日。親タスクの場合は子の最大値を動的に返す。</summary>
        public string? DisplayEndDate => HasChildren
            ? Children.Select(c => c.DisplayEndDate).Where(d => d is not null).Max()
            : Task.EndDate;
    }
}
