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

    /// <summary>工数（人日）を表示用にフォーマットする。</summary>
    /// <param name="value">工数（人日）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>小数第4位までの文字列。</returns>
    private static string FormatWorkDays(double? value) => value.HasValue ? value.Value.ToString("0.####") : "-";

    /// <summary>遅れ（人日）を表示用にフォーマットする。</summary>
    /// <param name="delayWorkDays">遅れ（予定工数合計 − 実績合計）。<see langword="null"/> の場合は "-"。</param>
    /// <returns>プラスは「+n（前倒し）」、マイナスは「n（遅れ）」、0は「0」、算出不可は「-」。</returns>
    private static string FormatDelay(double? delayWorkDays) => delayWorkDays switch
    {
        null => "-",
        > 0 => $"+{FormatWorkDays(delayWorkDays)}（前倒し）",
        < 0 => $"{FormatWorkDays(delayWorkDays)}（遅れ）",
        _ => "0"
    };

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");
}
