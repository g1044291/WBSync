using Microsoft.AspNetCore.Components;
using WBSync.Models;

namespace WBSync.Components.Pages;

/// <summary>担当者別稼働時間カレンダー画面のコードビハインド。</summary>
public partial class AssigneeWorkloadCalendar
{
    /// <summary>表示するプロジェクトID。</summary>
    [Parameter] public int ProjectId { get; set; }

    private string _projectName = string.Empty;
    private List<Assignee> _allAssignees = [];
    private List<DateOnly> _days = [];
    private Dictionary<(int AssigneeId, DateOnly Date), int> _minutesByAssigneeDate = [];

    #region Lifecycle

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        var projects = await ProjectRepo.GetAllAsync();
        var project = projects.FirstOrDefault(p => p.Id == ProjectId);
        if (project is null) { Nav.NavigateTo("/"); return; }
        _projectName = project.Name;

        var tasks = await TaskRepo.GetByProjectAsync(ProjectId);
        _allAssignees = await AssigneeRepo.GetByProjectAsync(ProjectId);
        var workLogs = await WorkLogRepo.GetByProjectAsync(ProjectId);

        _days = BuildDayRange(project.StartDate, tasks, workLogs);
        _minutesByAssigneeDate = BuildMinutesByAssigneeDate(workLogs);
    }

    #endregion

    #region 日付範囲・集計

    /// <summary>
    /// カレンダーに表示する日付範囲を算出する。
    /// プロジェクト開始日〜（タスク終了日の最大値＋30日）を基準とし、実績ログの記録日がその範囲外にある場合は範囲を広げる
    /// （入力漏れの把握という画面の目的上、記録済みの実績を必ず表示範囲に含めるため）。
    /// </summary>
    /// <param name="projectStartDate">プロジェクト開始日（yyyy-MM-dd 形式）。</param>
    /// <param name="tasks">プロジェクト内の全タスク。</param>
    /// <param name="workLogs">プロジェクト内の全実績ログ。</param>
    /// <returns>表示対象の日付一覧（昇順）。</returns>
    private static List<DateOnly> BuildDayRange(string projectStartDate, List<WbsTask> tasks, List<WorkLog> workLogs)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = DateOnly.TryParse(projectStartDate, out var s) ? s : today;

        var endDates = tasks
            .Select(t => t.EndDate)
            .Where(d => !string.IsNullOrEmpty(d))
            .Select(d => DateOnly.TryParse(d, out var date) ? date : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        var end = (endDates.Count > 0 ? endDates.Max() : start).AddDays(30);

        var logDates = workLogs
            .Select(w => DateOnly.TryParse(w.Date, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        if (logDates.Count > 0)
        {
            var logMin = logDates.Min();
            var logMax = logDates.Max();
            if (logMin < start) start = logMin;
            if (logMax > end) end = logMax;
        }

        var days = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
            days.Add(d);
        return days;
    }

    /// <summary>担当者ID・日付ごとに実績（分）の合計を集計する。</summary>
    /// <param name="workLogs">プロジェクト内の全実績ログ。</param>
    /// <returns>(担当者ID, 日付) をキーとした分合計の辞書。担当者未割り当て（<see langword="null"/>）の実績は集計対象外。</returns>
    private static Dictionary<(int AssigneeId, DateOnly Date), int> BuildMinutesByAssigneeDate(List<WorkLog> workLogs)
    {
        var result = new Dictionary<(int, DateOnly), int>();
        foreach (var log in workLogs)
        {
            if (log.AssigneeId is not { } assigneeId) continue;
            if (!DateOnly.TryParse(log.Date, out var date)) continue;

            var key = (assigneeId, date);
            result[key] = result.GetValueOrDefault(key) + log.Minutes;
        }
        return result;
    }

    /// <summary>指定担当者・日付の実績（分）合計を取得する。</summary>
    /// <param name="assigneeId">担当者ID。</param>
    /// <param name="date">対象日。</param>
    /// <returns>実績（分）合計。記録がない場合は0。</returns>
    private int GetMinutes(int assigneeId, DateOnly date)
        => _minutesByAssigneeDate.GetValueOrDefault((assigneeId, date));

    #endregion

    #region ナビゲーション

    /// <summary>ガントチャート画面に戻る。</summary>
    private void GoBack() => Nav.NavigateTo($"/gantt/{ProjectId}");

    #endregion
}
