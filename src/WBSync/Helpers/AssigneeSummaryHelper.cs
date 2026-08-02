using WBSync.Models;

namespace WBSync.Helpers;

/// <summary>ダッシュボード画面向けの担当者別工数集計ユーティリティ。</summary>
internal static class AssigneeSummaryHelper
{
    /// <summary>
    /// 担当者が設定されたリーフタスクのうち、実績が1件以上記録されているタスクを対象に、
    /// 担当者別の工数集計を行う（ステータスは問わない）。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="allWorkLogs">プロジェクト内の全実績ログ。</param>
    /// <param name="allAssignees">プロジェクト内の全担当者。</param>
    /// <returns>担当者名の五十音順に並んだ集計結果の一覧。</returns>
    internal static List<AssigneeSummary> BuildSummaries(
        List<WbsTask> allTasks,
        List<WorkLog> allWorkLogs,
        List<Assignee> allAssignees)
    {
        var leafTasks = TaskTreeHelper.GetAllLeafNodes(TaskTreeHelper.BuildTree(allTasks)).Select(n => n.Task);
        var actualByTaskId = allWorkLogs
            .GroupBy(w => w.TaskId)
            .ToDictionary(g => g.Key, g => g.Sum(w => w.Minutes) / 480.0);
        var assigneeNames = allAssignees.ToDictionary(a => a.Id, a => a.Name);

        var targetTasks = leafTasks.Where(t =>
            t.AssigneeId.HasValue
            && actualByTaskId.ContainsKey(t.Id));

        return targetTasks
            .GroupBy(t => t.AssigneeId!.Value)
            .Select(g =>
            {
                var planned = g.Sum(t => t.PlannedWorkDays ?? 0);
                var actual = g.Sum(t => actualByTaskId.GetValueOrDefault(t.Id));
                return new AssigneeSummary(assigneeNames.GetValueOrDefault(g.Key, "-"), planned, actual, planned - actual);
            })
            .OrderBy(s => s.AssigneeName, StringComparer.Ordinal)
            .ToList();
    }
}
