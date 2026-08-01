using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>DB 動作確認画面のコードビハインド。</summary>
public partial class DbDemo
{
    /// <summary>プロジェクト一覧に戻る。</summary>
    private void GoBack() => Nav.NavigateTo("/");

    private readonly List<string> _statuses = ["未着手", "進行中", "完了", "保留"];

    // ========== 1. Project ==========
    private List<Project> _projects = [];
    private string _pjName = string.Empty;
    private DateOnly? _pjDate = DateOnly.FromDateTime(DateTime.Today);
    private StatusMessage? _pjStatus;

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        _projects = await ProjectRepo.GetAllAsync();
        _globalHolidays = await GlobalHolidayRepo.GetAllAsync();
        await RefreshAllAssignees();
    }

    /// <summary>プロジェクトを作成する。</summary>
    private async Task CreateProject()
    {
        _pjStatus = null;
        if (string.IsNullOrWhiteSpace(_pjName) || _pjDate is null)
        {
            _pjStatus = StatusMessage.Error("プロジェクト名と開始日を入力してください");
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
    private StatusMessage? _asgStatus;
    private int? _editAsgId;
    private string _editAsgName = string.Empty;

    /// <summary>選択中プロジェクトの担当者一覧を読み込む。</summary>
    private async Task LoadAssignees()
    {
        _editAsgId = null;
        _assignees = _asgPjId > 0 ? await AssigneeRepo.GetByProjectAsync(_asgPjId) : [];
    }

    /// <summary>担当者を作成する。</summary>
    private async Task CreateAssignee()
    {
        _asgStatus = null;
        if (string.IsNullOrWhiteSpace(_asgName)) { _asgStatus = StatusMessage.Error("担当者名を入力してください"); return; }
        var nextSort = _assignees.Count > 0 ? _assignees.Max(a => a.SortOrder) + 1 : 0;
        await AssigneeRepo.CreateAsync(new Assignee { ProjectId = _asgPjId, Name = _asgName.Trim(), SortOrder = nextSort });
        _asgName = string.Empty;
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
        await RefreshAllAssignees();
    }

    /// <summary>担当者の編集モードを開始する。</summary>
    /// <param name="a">編集対象の担当者。</param>
    private void StartEditAssignee(Assignee a) { _editAsgId = a.Id; _editAsgName = a.Name; }

    /// <summary>担当者の編集をキャンセルする。</summary>
    private void CancelEditAssignee() => _editAsgId = null;

    /// <summary>担当者の編集を保存する。</summary>
    /// <param name="a">保存対象の担当者。</param>
    private async Task SaveAssignee(Assignee a)
    {
        a.Name = _editAsgName.Trim();
        await AssigneeRepo.UpdateAsync(a);
        _editAsgId = null;
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
        await RefreshAllAssignees();
    }

    /// <summary>担当者を削除する。</summary>
    /// <param name="id">削除する担当者ID。</param>
    private async Task DeleteAssignee(int id)
    {
        await AssigneeRepo.DeleteAsync(id);
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
        await RefreshAllAssignees();
    }

    /// <summary>担当者を一つ上に移動する。</summary>
    /// <param name="a">移動対象の担当者。</param>
    private async Task MoveAssigneeUp(Assignee a)
    {
        var idx = _assignees.FindIndex(x => x.Id == a.Id);
        if (idx <= 0) return;
        (_assignees[idx], _assignees[idx - 1]) = (_assignees[idx - 1], _assignees[idx]);
        await SaveAssigneeSortOrders();
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
    }

    /// <summary>担当者を一つ下に移動する。</summary>
    /// <param name="a">移動対象の担当者。</param>
    private async Task MoveAssigneeDown(Assignee a)
    {
        var idx = _assignees.FindIndex(x => x.Id == a.Id);
        if (idx < 0 || idx >= _assignees.Count - 1) return;
        (_assignees[idx], _assignees[idx + 1]) = (_assignees[idx + 1], _assignees[idx]);
        await SaveAssigneeSortOrders();
        _assignees = await AssigneeRepo.GetByProjectAsync(_asgPjId);
    }

    /// <summary>担当者一覧の表示順を DB に保存する。</summary>
    private async Task SaveAssigneeSortOrders()
    {
        for (int i = 0; i < _assignees.Count; i++)
            await AssigneeRepo.UpdateSortOrderAsync(_assignees[i].Id, i);
    }

    /// <summary>全プロジェクトの担当者一覧を再読み込みする。</summary>
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
    private StatusMessage? _taskAddStatus;
    private int? _editTaskId;
    private string _editTaskName = string.Empty;
    private string _editTaskStatus = "未着手";
    private int _editTaskProgress;

    /// <summary>選択中プロジェクトのタスク一覧を読み込む。</summary>
    private async Task LoadTasks()
    {
        _editTaskId = null;
        _tasks = _taskPjId > 0 ? await TaskRepo.GetByProjectAsync(_taskPjId) : [];
    }

    /// <summary>タスクを作成する。</summary>
    private async Task CreateTask()
    {
        _taskAddStatus = null;
        if (string.IsNullOrWhiteSpace(_taskName)) { _taskAddStatus = StatusMessage.Error("タスク名を入力してください"); return; }
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

    /// <summary>タスクの編集モードを開始する。</summary>
    /// <param name="t">編集対象のタスク。</param>
    private void StartEditTask(WbsTask t)
    {
        _editTaskId = t.Id;
        _editTaskName = t.Name;
        _editTaskStatus = t.Status;
        _editTaskProgress = t.Progress;
    }

    /// <summary>タスクの編集をキャンセルする。</summary>
    private void CancelEditTask() => _editTaskId = null;

    /// <summary>タスクの編集を保存する。</summary>
    /// <param name="t">保存対象のタスク。</param>
    private async Task SaveTask(WbsTask t)
    {
        t.Name = _editTaskName.Trim();
        t.Status = _editTaskStatus;
        t.Progress = _editTaskProgress;
        await TaskRepo.UpdateAsync(t);
        _editTaskId = null;
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    /// <summary>タスクを削除する。</summary>
    /// <param name="id">削除するタスクID。</param>
    private async Task DeleteTask(int id)
    {
        await TaskRepo.DeleteAsync(id);
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    /// <summary>タスクを一つ上に移動する。</summary>
    /// <param name="t">移動対象のタスク。</param>
    private async Task MoveTaskUp(WbsTask t)
    {
        var idx = _tasks.FindIndex(x => x.Id == t.Id);
        if (idx <= 0) return;
        (_tasks[idx], _tasks[idx - 1]) = (_tasks[idx - 1], _tasks[idx]);
        await SaveTaskSortOrders();
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    /// <summary>タスクを一つ下に移動する。</summary>
    /// <param name="t">移動対象のタスク。</param>
    private async Task MoveTaskDown(WbsTask t)
    {
        var idx = _tasks.FindIndex(x => x.Id == t.Id);
        if (idx < 0 || idx >= _tasks.Count - 1) return;
        (_tasks[idx], _tasks[idx + 1]) = (_tasks[idx + 1], _tasks[idx]);
        await SaveTaskSortOrders();
        _tasks = await TaskRepo.GetByProjectAsync(_taskPjId);
    }

    /// <summary>タスク一覧の表示順を DB に保存する。</summary>
    private async Task SaveTaskSortOrders()
    {
        for (int i = 0; i < _tasks.Count; i++)
            await TaskRepo.UpdateSortOrderAsync(_tasks[i].Id, i);
    }

    // ========== 4. GlobalHoliday ==========
    private List<GlobalHoliday> _globalHolidays = [];
    private DateOnly? _ghDate = DateOnly.FromDateTime(DateTime.Today);
    private string _ghName = string.Empty;
    private StatusMessage? _ghStatus;

    /// <summary>全体休日を作成する。</summary>
    private async Task CreateGlobalHoliday()
    {
        _ghStatus = null;
        if (_ghDate is null) { _ghStatus = StatusMessage.Error("日付を入力してください"); return; }
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
            _ghStatus = StatusMessage.Error($"エラー: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>全体休日を削除する。</summary>
    /// <param name="id">削除する休日ID。</param>
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
    private StatusMessage? _ahStatus;

    /// <summary>選択中担当者の個人休日一覧を読み込む。</summary>
    private async Task LoadAssigneeHolidays()
    {
        _assigneeHolidays = _ahAssigneeId > 0
            ? await AssigneeHolidayRepo.GetByAssigneeAsync(_ahAssigneeId)
            : [];
    }

    /// <summary>個人休日を作成する。</summary>
    private async Task CreateAssigneeHoliday()
    {
        _ahStatus = null;
        if (_ahDate is null) { _ahStatus = StatusMessage.Error("日付を入力してください"); return; }
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
            _ahStatus = StatusMessage.Error($"エラー: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>個人休日を削除する。</summary>
    /// <param name="id">削除する休日ID。</param>
    private async Task DeleteAssigneeHoliday(int id)
    {
        await AssigneeHolidayRepo.DeleteAsync(id);
        _assigneeHolidays = await AssigneeHolidayRepo.GetByAssigneeAsync(_ahAssigneeId);
    }
}
