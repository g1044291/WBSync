using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Modals;

public partial class TaskEditModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public WbsTask? Task { get; set; }
    [Parameter] public int ProjectId { get; set; }
    [Parameter] public int? ParentId { get; set; }
    [Parameter, EditorRequired] public List<Assignee> Assignees { get; set; } = [];
    [Parameter, EditorRequired] public List<WbsTask> AllTasks { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<WbsTask> OnSaved { get; set; }
    [Parameter] public EventCallback<int> OnDeleted { get; set; }

    private string _title => Task is null ? "タスクを追加" : "タスクを編集";
    private string _deleteMessage => $"「{Task?.Name}」およびその配下のタスクをすべて削除します。この操作は元に戻せません。";

    // フォーム状態
    private string _name = string.Empty;
    private WbsTask? _parent;
    private Assignee? _assignee;
    private string _workDaysStr = string.Empty;
    private DateOnly? _startDate;
    private DateOnly? _endDate;
    private WbsTask? _predecessor;
    private string _status = "未着手";
    private int _progress;
    private string _notes = string.Empty;

    private List<WbsTask> _parentCandidates = [];
    private List<WbsTask> _predecessorCandidates = [];
    private bool _isDirty;
    private bool _saving;
    private string? _error;
    private bool _showUnsavedDialog;
    private bool _showDeleteDialog;

    // パラメーター変化の追跡
    private bool _lastIsOpen;
    private int? _lastTaskId;

    protected override void OnParametersSet()
    {
        var shouldReset = (IsOpen && !_lastIsOpen) ||
                          (IsOpen && Task?.Id != _lastTaskId);
        _lastIsOpen = IsOpen;
        _lastTaskId = Task?.Id;

        if (!shouldReset) return;

        _isDirty = false;
        _error = null;
        _showUnsavedDialog = false;
        _showDeleteDialog = false;

        _parentCandidates = BuildParentCandidates();
        _predecessorCandidates = BuildPredecessorCandidates();

        if (Task is null)
        {
            _name = string.Empty;
            _parent = _parentCandidates.FirstOrDefault(t => t.Id == ParentId);
            _assignee = null;
            _workDaysStr = string.Empty;
            _startDate = null;
            _endDate = null;
            _predecessor = null;
            _status = "未着手";
            _progress = 0;
            _notes = string.Empty;
        }
        else
        {
            _name = Task.Name;
            _parent = _parentCandidates.FirstOrDefault(t => t.Id == Task.ParentId);
            _assignee = Assignees.FirstOrDefault(a => a.Id == Task.AssigneeId);
            _workDaysStr = Task.WorkDays?.ToString() ?? string.Empty;
            _startDate = DateOnly.TryParse(Task.StartDate, out var s) ? s : null;
            _endDate = DateOnly.TryParse(Task.EndDate, out var e) ? e : null;
            _predecessor = _predecessorCandidates.FirstOrDefault(t => t.Id == Task.PredecessorId);
            _status = Task.Status;
            _progress = Task.Progress;
            _notes = Task.Notes ?? string.Empty;
        }
    }

    private List<WbsTask> BuildParentCandidates()
    {
        if (Task is null) return [.. AllTasks];
        var excludeIds = GetDescendantIds(Task.Id);
        excludeIds.Add(Task.Id);
        return AllTasks.Where(t => !excludeIds.Contains(t.Id)).ToList();
    }

    private List<WbsTask> BuildPredecessorCandidates()
    {
        if (Task is null) return [.. AllTasks];
        var excludeIds = GetDescendantIds(Task.Id);
        excludeIds.Add(Task.Id);
        return AllTasks.Where(t => !excludeIds.Contains(t.Id)).ToList();
    }

    private HashSet<int> GetDescendantIds(int taskId)
    {
        var result = new HashSet<int>();
        foreach (var child in AllTasks.Where(t => t.ParentId == taskId))
        {
            result.Add(child.Id);
            foreach (var id in GetDescendantIds(child.Id))
                result.Add(id);
        }
        return result;
    }

    private void OnStatusChange(ChangeEventArgs e)
    {
        _status = e.Value?.ToString() ?? "未着手";
        _isDirty = true;
    }

    private async Task HandleClose()
    {
        if (_isDirty)
        {
            _showUnsavedDialog = true;
            return;
        }
        await OnClose.InvokeAsync();
    }

    private async Task HandleUnsavedConfirm()
    {
        _showUnsavedDialog = false;
        _isDirty = false;
        await OnClose.InvokeAsync();
    }

    private async Task HandleSave()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_name))
        {
            _error = "タスク名を入力してください";
            return;
        }

        double? workDays = null;
        if (!string.IsNullOrWhiteSpace(_workDaysStr) &&
            double.TryParse(_workDaysStr,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var wd))
            workDays = wd;

        _saving = true;
        try
        {
            WbsTask saved;
            var newParentId = _parent?.Id;

            if (Task is null)
            {
                var siblings = AllTasks.Where(t => t.ParentId == newParentId).ToList();
                var sortOrder = siblings.Count > 0 ? siblings.Max(t => t.SortOrder) + 1 : 0;

                saved = await TaskRepo.CreateAsync(new WbsTask
                {
                    ProjectId = ProjectId,
                    ParentId = newParentId,
                    PredecessorId = _predecessor?.Id,
                    AssigneeId = _assignee?.Id,
                    Name = _name.Trim(),
                    WorkDays = workDays,
                    StartDate = _startDate?.ToString("yyyy-MM-dd"),
                    EndDate = _endDate?.ToString("yyyy-MM-dd"),
                    Status = _status,
                    Progress = _progress,
                    Notes = string.IsNullOrWhiteSpace(_notes) ? null : _notes.Trim(),
                    SortOrder = sortOrder
                });
            }
            else
            {
                if (Task.ParentId != newParentId)
                {
                    var siblings = AllTasks.Where(t => t.ParentId == newParentId && t.Id != Task.Id).ToList();
                    Task.SortOrder = siblings.Count > 0 ? siblings.Max(t => t.SortOrder) + 1 : 0;
                    Task.ParentId = newParentId;
                }
                Task.Name = _name.Trim();
                Task.AssigneeId = _assignee?.Id;
                Task.WorkDays = workDays;
                Task.StartDate = _startDate?.ToString("yyyy-MM-dd");
                Task.EndDate = _endDate?.ToString("yyyy-MM-dd");
                Task.PredecessorId = _predecessor?.Id;
                Task.Status = _status;
                Task.Progress = _progress;
                Task.Notes = string.IsNullOrWhiteSpace(_notes) ? null : _notes.Trim();
                saved = await TaskRepo.UpdateAsync(Task);
            }

            _isDirty = false;
            await OnSaved.InvokeAsync(saved);
        }
        catch (Exception ex)
        {
            _error = $"保存に失敗しました: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }

    private void HandleDeleteClick() => _showDeleteDialog = true;

    private async Task HandleDeleteConfirm()
    {
        if (Task is null) return;
        var taskId = Task.Id;
        _showDeleteDialog = false;
        try
        {
            await TaskRepo.DeleteAsync(taskId);
            await OnDeleted.InvokeAsync(taskId);
        }
        catch (Exception ex)
        {
            _error = $"削除に失敗しました: {ex.Message}";
        }
    }
}
