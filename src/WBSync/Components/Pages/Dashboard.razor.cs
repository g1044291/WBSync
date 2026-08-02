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
    private List<AssigneeSummary> _summaries = [];

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var projects = await ProjectRepo.GetAllAsync();
        var project = projects.FirstOrDefault(p => p.Id == ProjectId);
        if (project is null) { Nav.NavigateTo("/"); return; }
        _projectName = project.Name;

        var allTasks = await TaskRepo.GetByProjectAsync(ProjectId);
        var allWorkLogs = await WorkLogRepo.GetByProjectAsync(ProjectId);
        var allAssignees = await AssigneeRepo.GetByProjectAsync(ProjectId);

        _summaries = AssigneeSummaryHelper.BuildSummaries(allTasks, allWorkLogs, allAssignees);
    }

    /// <summary>工数（人日）を表示用にフォーマットする。</summary>
    /// <param name="value">工数（人日）。</param>
    /// <returns>小数第2位までの文字列。</returns>
    private static string FormatWorkDays(double value) => value.ToString("0.##");

    /// <summary>遅れ（人日）を表示用にフォーマットする。</summary>
    /// <param name="delayWorkDays">遅れ（予定工数合計 − 実績合計）。</param>
    /// <returns>プラスは「+n（前倒し）」、マイナスは「n（遅れ）」、0は「0」。</returns>
    private static string FormatDelay(double delayWorkDays) => delayWorkDays switch
    {
        > 0 => $"+{FormatWorkDays(delayWorkDays)}（前倒し）",
        < 0 => $"{FormatWorkDays(delayWorkDays)}（遅れ）",
        _ => "0"
    };

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");
}
