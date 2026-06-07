using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Modals;

/// <summary>タスク編集モーダルのコードビハインド。新規作成と既存タスク編集を兼ねる。</summary>
public partial class TaskEditModal
{
    /// <summary>モーダルの開閉状態。</summary>
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>編集対象のタスク。<see langword="null"/> の場合は新規作成モード。</summary>
    [Parameter] public WbsTask? Task { get; set; }

    /// <summary>タスクを作成・編集するプロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    /// <summary>新規作成時のデフォルト親タスクID。</summary>
    [Parameter] public int? ParentId { get; set; }

    /// <summary>担当者の選択肢一覧。</summary>
    [Parameter, EditorRequired] public List<Assignee> Assignees { get; set; } = [];

    /// <summary>親タスク・先行タスクの候補に使用する全タスク一覧。</summary>
    [Parameter, EditorRequired] public List<WbsTask> AllTasks { get; set; } = [];

    /// <summary>モーダルを閉じるときに呼び出されるコールバック。</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>タスク保存完了時に呼び出されるコールバック。保存されたタスクを渡す。</summary>
    [Parameter] public EventCallback<WbsTask> OnSaved { get; set; }

    /// <summary>タスク削除完了時に呼び出されるコールバック。削除されたタスクIDを渡す。</summary>
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

    /// <inheritdoc/>
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

    /// <summary>親タスク選択肢を構築する。自タスクとその子孫を除外する。</summary>
    private List<WbsTask> BuildParentCandidates()
    {
        if (Task is null) return [.. AllTasks];
        var excludeIds = GetDescendantIds(Task.Id);
        excludeIds.Add(Task.Id);
        return AllTasks.Where(t => !excludeIds.Contains(t.Id)).ToList();
    }

    /// <summary>先行タスク選択肢を構築する。自タスクとその子孫を除外する。</summary>
    private List<WbsTask> BuildPredecessorCandidates()
    {
        if (Task is null) return [.. AllTasks];
        var excludeIds = GetDescendantIds(Task.Id);
        excludeIds.Add(Task.Id);
        return AllTasks.Where(t => !excludeIds.Contains(t.Id)).ToList();
    }

    /// <summary>指定タスクの子孫タスクID をすべて返す。</summary>
    /// <param name="taskId">起点タスクID。</param>
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

    /// <summary>
    /// 先行タスク選択変更時に開始日・終了日を自動計算する。
    /// 先行タスクの EndDate が未設定の場合は自動計算をスキップする。
    /// </summary>
    /// <param name="predecessor">選択された先行タスク。<see langword="null"/> の場合は先行タスクなし。</param>
    private async Task HandlePredecessorChanged(WbsTask? predecessor)
    {
        _predecessor = predecessor;
        _isDirty = true;

        if (string.IsNullOrEmpty(predecessor?.EndDate)) return;

        var (startDate, endDate) = await ScheduleService.CalcDatesFromPredecessorAsync(
            predecessor.EndDate, ParseWorkDays(), _assignee?.Id);

        _startDate = startDate;
        _endDate = endDate;
    }

    /// <summary>工数入力フィールドの文字列を数値にパースする。</summary>
    /// <returns>パース成功時は工数値、入力なし・パース失敗時は <see langword="null"/>。</returns>
    private double? ParseWorkDays()
    {
        if (string.IsNullOrWhiteSpace(_workDaysStr)) return null;
        return double.TryParse(_workDaysStr,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var wd) ? wd : null;
    }

    /// <summary>ステータス選択変更時にフォームを更新する。</summary>
    /// <param name="e">変更イベント。</param>
    private void OnStatusChange(ChangeEventArgs e)
    {
        _status = e.Value?.ToString() ?? "未着手";
        _isDirty = true;
    }

    /// <summary>未保存の変更がある場合は確認ダイアログを表示してからモーダルを閉じる。</summary>
    private async Task HandleClose()
    {
        if (_isDirty)
        {
            _showUnsavedDialog = true;
            return;
        }
        await OnClose.InvokeAsync();
    }

    /// <summary>未保存変更の破棄を確認してモーダルを閉じる。</summary>
    private async Task HandleUnsavedConfirm()
    {
        _showUnsavedDialog = false;
        _isDirty = false;
        await OnClose.InvokeAsync();
    }

    /// <summary>フォームを検証してタスクを保存する。</summary>
    private async Task HandleSave()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_name))
        {
            _error = "タスク名を入力してください";
            return;
        }

        var workDays = ParseWorkDays();

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

    /// <summary>タスク削除の確認ダイアログを表示する。</summary>
    private void HandleDeleteClick() => _showDeleteDialog = true;

    /// <summary>タスク削除の確認ダイアログで「削除」が押されたときの処理。</summary>
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
