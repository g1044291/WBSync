using Microsoft.JSInterop;
using Microsoft.Maui.Storage;
using WBSync.Helpers;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>複数プロジェクトを横断して表示するWBS画面のコードビハインド。読み取り専用。</summary>
public partial class CrossProjectView
{
    private const string SelectedProjectIdsPreferenceKey = "CrossProjectView.SelectedProjectIds";

    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);

    private List<Project> _allProjects = [];
    private HashSet<int> _selectedProjectIds = [];
    private List<ProjectGroup> _groups = [];
    private Dictionary<int, string> _assigneeNames = [];
    private HashSet<DateOnly> _globalHolidays = [];

    private List<string> _assigneeFilterOptions = [];
    private string? _filterAssigneeName;

    private ChartScale _scale = ChartScale.Day;
    private DateOnly _chartStart;
    private DateOnly _chartEnd;
    private List<ChartColumn> _chartColumns = [];

    #region Lifecycle

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _allProjects = await ProjectRepo.GetAllAsync();

        _selectedProjectIds = LoadSelectedProjectIds();
        _selectedProjectIds.IntersectWith(_allProjects.Select(p => p.Id));

        var holidays = await GlobalHolidayRepo.GetAllAsync();
        _globalHolidays = holidays
            .Select(h => DateOnly.TryParse(h.Date, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToHashSet();

        await ReloadAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await JS.InvokeVoidAsync("initSplitter", "gantt-splitter", "task-pane", 320, 800);
        await JS.InvokeVoidAsync("initScrollSync", "task-pane-rows", "chart-pane");
        await JS.InvokeVoidAsync("initColNameResize", "col-name-resize", "task-pane", 120);

        if (_chartColumns.Count > 0)
        {
            var todayOffset = GanttChartLayoutHelper.GetPixelOffset(_scale, _chartStart, _today) + GetColumnWidth() / 2;
            await JS.InvokeVoidAsync("centerHorizontalScroll", "chart-pane", todayOffset);
        }
    }

    #endregion

    #region データ読み込み

    /// <summary>選択中プロジェクトのタスクツリー・担当者名・チャート期間を読み込み直す。</summary>
    private async Task ReloadAsync()
    {
        if (_selectedProjectIds.Count == 0)
        {
            _groups = [];
            _assigneeNames = [];
            _assigneeFilterOptions = [];
            _chartColumns = [];
            return;
        }

        var tasks = await TaskRepo.GetByProjectsAsync(_selectedProjectIds);
        var assignees = await AssigneeRepo.GetByProjectsAsync(_selectedProjectIds);
        _assigneeNames = assignees.ToDictionary(a => a.Id, a => a.Name);
        _assigneeFilterOptions = assignees.Select(a => a.Name).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();
        if (_filterAssigneeName is not null && !_assigneeFilterOptions.Contains(_filterAssigneeName))
            _filterAssigneeName = null;

        var tasksByProject = tasks.GroupBy(t => t.ProjectId).ToDictionary(g => g.Key, g => g.ToList());
        _groups = _allProjects
            .Where(p => _selectedProjectIds.Contains(p.Id))
            .Select(p =>
            {
                var projectTasks = tasksByProject.GetValueOrDefault(p.Id, []);
                return new ProjectGroup(p, projectTasks, TaskTreeHelper.BuildTree(projectTasks));
            })
            .ToList();

        CalculateChartPeriod(tasks);
        _chartColumns = GanttChartLayoutHelper.BuildColumns(_scale, _chartStart, _chartEnd, _globalHolidays);
    }

    /// <summary>チャートの表示期間を、選択中プロジェクトの開始日とタスクの終了日から算出する。</summary>
    /// <param name="tasks">選択中プロジェクトの全タスク。</param>
    private void CalculateChartPeriod(List<WbsTask> tasks)
    {
        var projectStartDates = _groups
            .Select(g => DateOnly.TryParse(g.Project.StartDate, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        _chartStart = projectStartDates.Count > 0 ? projectStartDates.Min() : DateOnly.FromDateTime(DateTime.Today);

        var endDates = tasks
            .Select(t => t.EndDate)
            .Where(d => !string.IsNullOrEmpty(d))
            .Select(d => DateOnly.TryParse(d, out var date) ? date : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        var lastEndDate = endDates.Count > 0 ? endDates.Max() : _chartStart;
        _chartEnd = lastEndDate.AddDays(30);
    }

    /// <summary>プロジェクト単位のタスクツリー・空タスクメッセージ行を表示順に列挙した一覧を構築する。担当者フィルターを反映する。</summary>
    /// <returns>表示行の一覧。</returns>
    private List<DisplayRow> BuildDisplayRows()
    {
        var rows = new List<DisplayRow>();
        foreach (var group in _groups)
        {
            rows.Add(new ProjectHeaderRow(group.Project));
            if (group.Roots.Count == 0)
            {
                rows.Add(new EmptyMessageRow(group.Project, IsFilterEmpty: false));
                continue;
            }

            var keepTaskIds = BuildAssigneeFilterKeepSet(group.Tasks, group.Roots);
            var hasVisibleTask = false;
            foreach (var node in GetVisibleFilteredNodes(group.Roots, keepTaskIds))
            {
                rows.Add(new TaskRowItem(node));
                hasVisibleTask = true;
            }

            if (!hasVisibleTask)
                rows.Add(new EmptyMessageRow(group.Project, IsFilterEmpty: true));
        }
        return rows;
    }

    /// <summary>
    /// 担当者フィルターに一致するリーフタスクと、その祖先タスクのIDセットを返す。フィルター未指定の場合は全タスクIDを返す。
    /// </summary>
    /// <param name="tasks">対象プロジェクトの全タスク（祖先を辿るために使用）。</param>
    /// <param name="roots">対象プロジェクトのルートノード群。</param>
    /// <returns>表示を維持するタスクIDのセット。</returns>
    private HashSet<int> BuildAssigneeFilterKeepSet(List<WbsTask> tasks, List<TaskNode> roots)
    {
        if (_filterAssigneeName is null)
            return TaskTreeHelper.GetAllNodesInDisplayOrder(roots).Select(n => n.Task.Id).ToHashSet();

        var taskById = tasks.ToDictionary(t => t.Id);
        var keep = new HashSet<int>();
        foreach (var leaf in TaskTreeHelper.GetAllLeafNodes(roots))
        {
            if (!leaf.Task.AssigneeId.HasValue) continue;
            if (!_assigneeNames.TryGetValue(leaf.Task.AssigneeId.Value, out var name) || name != _filterAssigneeName) continue;

            int? id = leaf.Task.Id;
            while (id.HasValue)
            {
                keep.Add(id.Value);
                id = taskById.TryGetValue(id.Value, out var t) ? t.ParentId : null;
            }
        }
        return keep;
    }

    /// <summary>展開状態とフィルターの両方を考慮して表示対象のノードを列挙する。</summary>
    /// <param name="nodes">対象ノード群。</param>
    /// <param name="keepTaskIds">表示を維持するタスクIDセット。</param>
    private static IEnumerable<TaskNode> GetVisibleFilteredNodes(List<TaskNode> nodes, HashSet<int> keepTaskIds)
    {
        foreach (var node in nodes)
        {
            if (!keepTaskIds.Contains(node.Task.Id)) continue;
            yield return node;
            if (node.IsExpanded && node.HasChildren)
                foreach (var child in GetVisibleFilteredNodes(node.Children, keepTaskIds))
                    yield return child;
        }
    }

    #endregion

    #region 操作

    /// <summary>プロジェクトの選択状態を切り替え、選択内容を保存してから再読み込みする。</summary>
    /// <param name="projectId">対象プロジェクトID。</param>
    private async Task ToggleProject(int projectId)
    {
        if (!_selectedProjectIds.Remove(projectId))
            _selectedProjectIds.Add(projectId);

        SaveSelectedProjectIds();
        await ReloadAsync();
    }

    /// <summary>チャートのスケールを切り替える。</summary>
    /// <param name="scale">切り替え先のスケール。</param>
    private void SetScale(ChartScale scale)
    {
        _scale = scale;
        _chartColumns = GanttChartLayoutHelper.BuildColumns(_scale, _chartStart, _chartEnd, _globalHolidays);
    }

    /// <summary>担当者フィルターを変更する。</summary>
    /// <param name="assigneeName">絞り込む担当者名。<see langword="null"/> の場合は絞り込まない。</param>
    private void OnFilterAssigneeChanged(string? assigneeName) => _filterAssigneeName = assigneeName;

    /// <summary>タスクノードの展開状態を切り替える。</summary>
    /// <param name="node">対象ノード。</param>
    private static void ToggleExpand(TaskNode node) => node.IsExpanded = !node.IsExpanded;

    /// <summary>プロジェクト一覧画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo("/");

    #endregion

    #region 表示ヘルパー

    /// <summary>担当者名を取得する。</summary>
    /// <param name="assigneeId">担当者ID。<see langword="null"/> の場合は未割り当て。</param>
    /// <returns>担当者名。見つからない場合は "-"。</returns>
    private string GetAssigneeName(int? assigneeId)
        => assigneeId.HasValue && _assigneeNames.TryGetValue(assigneeId.Value, out var name) ? name : "-";

    /// <summary>日付文字列を表示用にフォーマットする。</summary>
    /// <param name="date">yyyy-MM-dd 形式の日付文字列。</param>
    /// <returns>M/d 形式の文字列。空の場合は "-"。</returns>
    private static string FormatDate(string? date)
    {
        if (string.IsNullOrEmpty(date)) return "-";
        return DateOnly.TryParse(date, out var d) ? d.ToString("M/d") : date;
    }

    /// <summary>チャート列（ヘッダー・セル共通）に適用する CSS クラス名を返す。</summary>
    /// <param name="col">対象チャート列。</param>
    /// <returns>今日を含む列は "col-today"、週末・祝日の列は "col-weekend"、それ以外は空文字。</returns>
    private string GetTimelineCellClass(ChartColumn col)
    {
        if (IsToday(col)) return "col-today";
        if (col.IsWeekend || col.IsHoliday) return "col-weekend";
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

    /// <summary>現在のスケールにおける1列分のピクセル幅を返す（CSS の列幅と一致させる）。</summary>
    private double GetColumnWidth() => _scale switch
    {
        ChartScale.Day => 32.0,
        ChartScale.Week => 80.0,
        ChartScale.Month => 90.0,
        _ => 32.0
    };

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

    #region 選択状態の永続化

    /// <summary>前回保存された選択プロジェクトID一覧を読み込む。</summary>
    /// <returns>選択プロジェクトIDの集合。保存がない場合は空集合。</returns>
    private static HashSet<int> LoadSelectedProjectIds()
    {
        var saved = Preferences.Default.Get(SelectedProjectIdsPreferenceKey, string.Empty);
        return saved
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();
    }

    /// <summary>選択プロジェクトID一覧を保存する。</summary>
    private void SaveSelectedProjectIds()
        => Preferences.Default.Set(SelectedProjectIdsPreferenceKey, string.Join(",", _selectedProjectIds));

    #endregion

    /// <summary>プロジェクト単位のタスクツリーのグループ。</summary>
    /// <param name="Project">対象プロジェクト。</param>
    /// <param name="Tasks">当該プロジェクトの全タスク（祖先を辿るために使用）。</param>
    /// <param name="Roots">当該プロジェクトのタスクツリーのルートノード群。</param>
    private sealed record ProjectGroup(Project Project, List<WbsTask> Tasks, List<TaskNode> Roots);

    /// <summary>タスクペイン・チャートペインで表示順を共有するための表示行。</summary>
    private abstract record DisplayRow;

    /// <summary>プロジェクトグループの見出し行。</summary>
    /// <param name="Project">対象プロジェクト。</param>
    private sealed record ProjectHeaderRow(Project Project) : DisplayRow;

    /// <summary>表示するタスクが1件もないプロジェクトに表示するメッセージ行。</summary>
    /// <param name="Project">対象プロジェクト。</param>
    /// <param name="IsFilterEmpty">タスク自体は存在するが、担当者フィルターにより1件も該当しない場合は <see langword="true"/>。</param>
    private sealed record EmptyMessageRow(Project Project, bool IsFilterEmpty) : DisplayRow;

    /// <summary>タスクツリーの1ノードに対応する行。</summary>
    /// <param name="Node">対象タスクノード。</param>
    private sealed record TaskRowItem(TaskNode Node) : DisplayRow;
}
