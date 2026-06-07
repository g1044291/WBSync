using WBSync.Models;

namespace WBSync.Components.Pages;

public partial class DbDemo
{
    private void GoBack() => Nav.NavigateTo("/");

    private readonly List<string> _statuses = ["未着手", "進行中", "完了", "保留"];

    // ========== 1. Project ==========
    private List<Project> _projects = [];
    private string _pjName = string.Empty;
    private DateOnly? _pjDate = DateOnly.FromDateTime(DateTime.Today);
    private string? _pjError;

    protected override async Task OnInitializedAsync()
    {
        _projects = await ProjectRepo.GetAllAsync();
        _globalHolidays = await GlobalHolidayRepo.GetAllAsync();
        await RefreshAllAssignees();
    }

    private async Task CreateProject()
    {
        _pjError = null;
        if (string.IsNullOrWhiteSpace(_pjName) || _pjDate is null)
        {
            _pjError = "プロジェクト名と開始日を入力してください";
            return;
        }
        await ProjectRepo.CreateAsync(new Project
        {
            Name = _pjName.Trim(),
            StartDate = _pjDate.Value.ToString("yyyy-MM-dd")
        });
        _pjName = string.Empty;
        _projects = await ProjectRepo.GetAllAsync();
    }

    // ========== 2. Assignee ==========
    private int _asgPjId;
    private List<Assignee> _assignees = [];
    private string _asgName = string.Empty;
    private string? _asgError;
    private int? _editAsgId;
    private string _editAsgName = string.Empty;

    private async Task LoadAssignees()
    {
        _editAsgId = null;
        _assignees = _asgPjId > 0 ? await AssigneeRepo.GetByProjectAsync(_asgPjId) : [];
    }

    private async Task CreateAssignee()
    {
        _asgError = null;
        if (string.IsNullOrWhiteSpace(_asgName)) { _asgError = "担当者名を入力してください"; return; }
        var nextSort = _assignees.Count > 0 ? _assignees.Max(a => a.SortOrder) + 1 : 0;
        await AssigneeRepo.CreateAsync(new Assignee { ProjectId = _asgPjId, Name = _asgName.Trim(), SortOrder = nextSort });
        _asgName = string.Empty;
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
        await RefreshAllAssignees();
    }

    private void StartEditAssignee(Assignee a) { _editAsgId = a.Id; _editAsgName = a.Name; }
    private void CancelEditAssignee() => _editAsgId = null;

    private async Task SaveAssignee(Assignee a)
    {
        a.Name = _editAsgName.Trim();
        await AssigneeRepo.UpdateAsync(a);
        _editAsgId = null;
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
        await RefreshAllAssignees();
    }

    private async Task DeleteAssignee(int id)
    {
        await AssigneeRepo.DeleteAsync(id);
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
        await RefreshAllAssignees();
    }

    private async Task MoveAssigneeUp(Assignee a)
    {
        var idx = _assignees.FindIndex(x => x.Id == a.Id);
        if (idx <= 0) return;
        (_assignees[idx], _assignees[idx - 1]) = (_assignees[idx - 1], _assignees[idx]);
        await SaveAssigneeSortOrders();
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
    }

    private async Task MoveAssigneeDown(Assignee a)
    {
        var idx = _assignees.FindIndex(x => x.Id == a.Id);
        if (idx < 0 || idx >= _assignees.Count - 1) return;
        (_assignees[idx], _assignees[idx + 1]) = (_assignees[idx + 1], _assignees[idx]);
        await SaveAssigneeSortOrders();
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
    }

    private async Task SaveAssigneeSortOrders()
    {
        for (int i = 0; i < _assignees.Count; i++)
            await AssigneeRepo.UpdateSortOrderAsync(_assignees[i].Id, i);
    }

    private async Task RefreshAllAssignees()
    {
        _allAssignees = [];
        foreach (var p in _projects)
            _allAssignees.AddRange(await AssigneeRepo.GetByProjectAsync(p.Id));
    }

    // ========== 3. Task ==========
    private int _taskPjId;
    private List<WbsTask> _tasks = [];
    private string _taskName = string.Empty;
    private double? _taskWorkDays;
    private DateOnly? _taskStart;
    private DateOnly? _taskEnd;
    private string _taskStatus = "未着手";
    private string? _taskError;
    private int? _editTaskId;
    private string _editTaskName = string.Empty;
    private string _editTaskStatus = "未着手";
    private int _editTaskProgress;

    private async Task LoadTasks()
    {
        _editTaskId = null;
        _tasks = _taskPjId > 0 ? await TaskRepo.GetByProjectAsync(_taskPjId) : [];
    }

    private async Task CreateTask()
    {
        _taskError = null;
        if (string.IsNullOrWhiteSpace(_taskName)) { _taskError = "タスク名を入力してください"; return; }
        var nextSort = _tasks.Count > 0 ? _tasks.Max(t => t.SortOrder) + 1 : 0;
        await TaskRepo.CreateAsync(new WbsTask
        {
            ProjectId = _taskPjId,
            Name = _taskName.Trim(),
            WorkDays = _taskWorkDays,
            StartDate = _taskStart?.ToString("yyyy-MM-dd"),
            EndDate = _taskEnd?.ToString("yyyy-MM-dd"),
            Status = _taskStatus,
            SortOrder = nextSort
        });
        _taskName = string.Empty;
        _taskWorkDays = null;
        _taskStart = null;
        _taskEnd = null;
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    private void StartEditTask(WbsTask t)
    {
        _editTaskId = t.Id;
        _editTaskName = t.Name;
        _editTaskStatus = t.Status;
        _editTaskProgress = t.Progress;
    }

    private void CancelEditTask() => _editTaskId = null;

    private async Task SaveTask(WbsTask t)
    {
        t.Name = _editTaskName.Trim();
        t.Status = _editTaskStatus;
        t.Progress = _editTaskProgress;
        await TaskRepo.UpdateAsync(t);
        _editTaskId = null;
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    private async Task DeleteTask(int id)
    {
        await TaskRepo.DeleteAsync(id);
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    private async Task MoveTaskUp(WbsTask t)
    {
        var idx = _tasks.FindIndex(x => x.Id == t.Id);
        if (idx <= 0) return;
        (_tasks[idx], _tasks[idx - 1]) = (_tasks[idx - 1], _tasks[idx]);
        await SaveTaskSortOrders();
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    private async Task MoveTaskDown(WbsTask t)
    {
        var idx = _tasks.FindIndex(x => x.Id == t.Id);
        if (idx < 0 || idx >= _tasks.Count - 1) return;
        (_tasks[idx], _tasks[idx + 1]) = (_tasks[idx + 1], _tasks[idx]);
        await SaveTaskSortOrders();
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    private async Task SaveTaskSortOrders()
    {
        for (int i = 0; i < _tasks.Count; i++)
            await TaskRepo.UpdateSortOrderAsync(_tasks[i].Id, i);
    }

    // ========== 4. GlobalHoliday ==========
    private List<GlobalHoliday> _globalHolidays = [];
    private DateOnly? _ghDate = DateOnly.FromDateTime(DateTime.Today);
    private string _ghName = string.Empty;
    private string? _ghError;

    private async Task CreateGlobalHoliday()
    {
        _ghError = null;
        if (_ghDate is null) { _ghError = "日付を入力してください"; return; }
        try
        {
            await GlobalHolidayRepo.CreateAsync(new GlobalHoliday
            {
                Date = _ghDate.Value.ToString("yyyy-MM-dd"),
                Name = string.IsNullOrWhiteSpace(_ghName) ? null : _ghName.Trim()
            });
            _ghName = string.Empty;
            _ghDate = null;
            _globalHolidays = await GlobalHolidayRepo.GetAllAsync();
        }
        catch (Exception ex)
        {
            _ghError = $"エラー: {ex.InnerException?.Message ?? ex.Message}";
        }
    }

    private async Task DeleteGlobalHoliday(int id)
    {
        await GlobalHolidayRepo.DeleteAsync(id);
        _globalHolidays = await GlobalHolidayRepo.GetAllAsync();
    }

    // ========== 5. AssigneeHoliday ==========
    private List<Assignee> _allAssignees = [];
    private int _ahAssigneeId;
    private List<AssigneeHoliday> _assigneeHolidays = [];
    private DateOnly? _ahDate = DateOnly.FromDateTime(DateTime.Today);
    private string _ahMemo = string.Empty;
    private string? _ahError;

    private async Task LoadAssigneeHolidays()
    {
        _assigneeHolidays = _ahAssigneeId > 0
            ? await AssigneeHolidayRepo.GetByAssigneeAsync(_ahAssigneeId)
            : [];
    }

    private async Task CreateAssigneeHoliday()
    {
        _ahError = null;
        if (_ahDate is null) { _ahError = "日付を入力してください"; return; }
        try
        {
            await AssigneeHolidayRepo.CreateAsync(new AssigneeHoliday
            {
                AssigneeId = _ahAssigneeId,
                Date = _ahDate.Value.ToString("yyyy-MM-dd"),
                Memo = string.IsNullOrWhiteSpace(_ahMemo) ? null : _ahMemo.Trim()
            });
            _ahMemo = string.Empty;
            _ahDate = null;
            _assigneeHolidays = await AssigneeHolidayRepo.GetByAssigneeAsync(_ahAssigneeId);
        }
        catch (Exception ex)
        {
            _ahError = $"エラー: {ex.InnerException?.Message ?? ex.Message}";
        }
    }

    private async Task DeleteAssigneeHoliday(int id)
    {
        await AssigneeHolidayRepo.DeleteAsync(id);
        _assigneeHolidays = await AssigneeHolidayRepo.GetByAssigneeAsync(_ahAssigneeId);
    }
}
