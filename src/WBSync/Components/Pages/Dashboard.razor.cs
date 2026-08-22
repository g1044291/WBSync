using Microsoft.AspNetCore.Components;
using WBSync.Helpers;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>ダッシュボード画面のコードビハインド。</summary>
public partial class Dashboard
{
    /// <summary>表示するプロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private string _projectName = string.Empty;
    private List<WbsTask> _allTasks = [];
    private List<WorkLog> _allWorkLogs = [];
    private List<AssigneeSummary> _summaries = [];
    private AssigneeSummaryTotal _total = new(0, 0, 0);
    private readonly Dictionary<int, List<AssigneeTaskRow>> _taskRowsByAssigneeId = [];
    private readonly HashSet<int> _expandedAssigneeIds = [];

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var projects = await ProjectRepo.GetAllAsync();
        var project = projects.FirstOrDefault(p => p.Id == ProjectId);
        if (project is null) { Nav.NavigateTo("/"); return; }
        _projectName = project.Name;

        _allTasks = await TaskRepo.GetByProjectAsync(ProjectId);
        _allWorkLogs = await WorkLogRepo.GetByProjectAsync(ProjectId);
        var allAssignees = await AssigneeRepo.GetByProjectAsync(ProjectId);

        _summaries = AssigneeSummaryHelper.BuildSummaries(_allTasks, _allWorkLogs, allAssignees);
        _total = AssigneeSummaryHelper.BuildTotal(_summaries);
    }

    /// <summary>担当者行のタスクツリー展開状態を切り替える。初回展開時はタスクツリーを構築する。</summary>
    /// <param name="summary">対象担当者の集計結果。</param>
    private void ToggleExpand(AssigneeSummary summary)
    {
        if (!_expandedAssigneeIds.Add(summary.AssigneeId))
        {
            _expandedAssigneeIds.Remove(summary.AssigneeId);
            return;
        }

        if (!_taskRowsByAssigneeId.ContainsKey(summary.AssigneeId))
            _taskRowsByAssigneeId[summary.AssigneeId] = AssigneeSummaryHelper.BuildAssigneeTaskRows(_allTasks, _allWorkLogs, summary.AssigneeId);
    }

    /// <summary>工数（人日）を表示用にフォーマットする。単位「人日」を付与する。</summary>
    /// <param name="value">工数（人日）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>「n人日」形式の文字列。算出不可の場合は "-"。</returns>
    private static string FormatWorkDays(double? value)
        => value.HasValue ? $"{PersonDayHelper.FormatWorkDays(value)}人日" : "-";

    /// <summary>遅れ（人日）を表示用にフォーマットする。</summary>
    /// <param name="delayWorkDays">遅れ（予定工数合計 − 実績合計）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>プラスは「+n人日（前倒し）」、マイナスは「n人日（遅れ）」、0は「0人日」、算出不可は「-」。</returns>
    private static string FormatDelay(double? delayWorkDays) => delayWorkDays switch
    {
        null => "-",
        > 0 => $"+{PersonDayHelper.FormatWorkDays(delayWorkDays)}人日（前倒し）",
        < 0 => $"{PersonDayHelper.FormatWorkDays(delayWorkDays)}人日（遅れ）",
        _ => "0人日"
    };

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");
}
