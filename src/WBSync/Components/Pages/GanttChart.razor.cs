using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WBSync.Models;

namespace WBSync.Components.Pages;

public partial class GanttChart
{
    [Parameter] public int ProjectId { get; set; }

    private string _projectName = string.Empty;
    private string _projectStartDate = string.Empty;
    private List<TaskNode> _taskRoots = [];
    private List<WbsTask> _allTasks = [];
    private List<Assignee> _allAssignees = [];
    private Dictionary<int, string> _assigneeNames = [];
    private HashSet<DateOnly> _globalHolidays = [];
    private Dictionary<int, HashSet<DateOnly>> _assigneeHolidays = [];

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

    private enum ChartScale { Day, Week, Month }
    private ChartScale _scale = ChartScale.Day;

    private DateOnly _chartStart;
    private DateOnly _chartEnd;

    private record ChartColumn(string Label, DateOnly Date, bool IsWeekend, bool IsHoliday = false);
    private List<ChartColumn> _chartColumns = [];

    // ===== Lifecycle =====

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

        CalculateChartPeriod();
        BuildChartColumns();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await JS.InvokeVoidAsync("initSplitter", "gantt-splitter", "task-pane", 320, 800);
    }

    // ===== Scale management =====

    private void SetScale(ChartScale scale)
    {
        _scale = scale;
        BuildChartColumns();
    }

    // ===== Chart period & columns =====

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

    private List<ChartColumn> BuildMonthColumns()
    {
        var cols = new List<ChartColumn>();
        var monthStart = new DateOnly(_chartStart.Year, _chartStart.Month, 1);
        var chartEndMonth = new DateOnly(_chartEnd.Year, _chartEnd.Month, 1);

        for (var d = monthStart; d <= chartEndMonth; d = d.AddMonths(1))
            cols.Add(new ChartColumn($"{d:M月}", d, false));

        return cols;
    }

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

    private static void SetLevels(List<TaskNode> nodes, int level)
    {
        foreach (var node in nodes)
        {
            node.Level = level;
            SetLevels(node.Children, level + 1);
        }
    }

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

    private void ToggleExpand(TaskNode node) => node.IsExpanded = !node.IsExpanded;

    // ===== 個人休日ロード =====

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

    private string GetAssigneeName(int? assigneeId)
        => assigneeId.HasValue && _assigneeNames.TryGetValue(assigneeId.Value, out var name) ? name : "-";

    private static string FormatDate(string? date)
    {
        if (string.IsNullOrEmpty(date)) return "-";
        return DateOnly.TryParse(date, out var d) ? d.ToString("M/d") : date;
    }

    // ===== ナビゲーション・操作 =====

    private void GoBack() => Nav.NavigateTo("/");
    private void GoToAssignees() => Nav.NavigateTo($"/assignees/{ProjectId}");

    private void AddTask()
    {
        _taskModalTask = null;
        _taskModalParentId = null;
        _isTaskModalOpen = true;
    }

    private void OnTaskRowClick(TaskNode node)
    {
        _taskModalTask = node.Task;
        _taskModalParentId = null;
        _isTaskModalOpen = true;
    }

    // ===== 右クリックメニュー =====

    private void ShowContextMenu(MouseEventArgs e, TaskNode node)
    {
        _ctxX = e.ClientX;
        _ctxY = e.ClientY;
        _ctxNode = node;
        _ctxVisible = true;
    }

    private void CloseContextMenu() => _ctxVisible = false;

    private void AddChildTask()
    {
        CloseContextMenu();
        _taskModalTask = null;
        _taskModalParentId = _ctxNode!.Task.Id;
        _isTaskModalOpen = true;
    }

    private void AddSiblingTask()
    {
        CloseContextMenu();
        _taskModalTask = null;
        _taskModalParentId = _ctxNode!.Task.ParentId;
        _isTaskModalOpen = true;
    }

    private void ShowCtxDeleteConfirm()
    {
        _ctxVisible = false;
        _showCtxDeleteConfirm = true;
    }

    private async Task HandleCtxDeleteConfirm()
    {
        if (_ctxNode is null) return;
        _showCtxDeleteConfirm = false;
        await TaskRepo.DeleteAsync(_ctxNode.Task.Id);
        _ctxNode = null;
        await ReloadTasksAsync();
    }

    private void HandleTaskModalClose() => _isTaskModalOpen = false;

    private async Task HandleTaskSaved(WbsTask _)
    {
        _isTaskModalOpen = false;
        await ReloadTasksAsync();
    }

    private async Task HandleTaskDeleted(int _)
    {
        _isTaskModalOpen = false;
        await ReloadTasksAsync();
    }

    private async Task ReloadTasksAsync()
    {
        _allTasks = await TaskRepo.GetByProjectAsync(ProjectId);
        _taskRoots = BuildTree(_allTasks);
        CalculateChartPeriod();
        BuildChartColumns();
    }

    // ===== TaskNode =====

    private class TaskNode
    {
        public WbsTask Task { get; init; } = null!;
        public List<TaskNode> Children { get; } = [];
        public int Level { get; set; }
        public bool IsExpanded { get; set; } = true;

        public bool HasChildren => Children.Count > 0;

        public string? DisplayStartDate => HasChildren
            ? Children.Select(c => c.DisplayStartDate).Where(d => d is not null).Min()
            : Task.StartDate;

        public string? DisplayEndDate => HasChildren
            ? Children.Select(c => c.DisplayEndDate).Where(d => d is not null).Max()
            : Task.EndDate;
    }
}
