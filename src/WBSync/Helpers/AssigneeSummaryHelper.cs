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
                return new AssigneeSummary(g.Key, assigneeNames.GetValueOrDefault(g.Key, "-"), planned, actual, planned - actual);
            })
            .OrderBy(s => s.AssigneeName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 指定担当者が関わったタスクのツリーを構築する。「関わった」は次のいずれかを満たすリーフタスクを指す。
    /// (1) 現在この担当者が割り当てられ、実績が記録されている（<see cref="BuildSummaries"/> の集計対象と同じ）、
    /// (2) 現在の担当者に関わらず、この担当者自身の実績ログ（<see cref="WorkLog.AssigneeId"/>）が記録されている。
    /// 対象リーフタスクの祖先タスクも、ツリー構造を保つために含める。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="allWorkLogs">プロジェクト内の全実績ログ。</param>
    /// <param name="assigneeId">対象担当者ID。</param>
    /// <returns>WBS表示順に並んだタスクツリーの行一覧。関わったタスクが1件もない場合は空リスト。</returns>
    internal static List<AssigneeTaskRow> BuildAssigneeTaskRows(
        List<WbsTask> allTasks,
        List<WorkLog> allWorkLogs,
        int assigneeId)
    {
        var roots = TaskTreeHelper.BuildTree(allTasks);
        var leafTasks = TaskTreeHelper.GetAllLeafNodes(roots).Select(n => n.Task).ToList();

        var actualTotalByTaskId = allWorkLogs
            .GroupBy(w => w.TaskId)
            .ToDictionary(g => g.Key, g => g.Sum(w => w.Minutes) / 480.0);
        var actualByAssigneeByTaskId = allWorkLogs
            .Where(w => w.AssigneeId == assigneeId)
            .GroupBy(w => w.TaskId)
            .ToDictionary(g => g.Key, g => g.Sum(w => w.Minutes) / 480.0);

        var ownedLeafIds = leafTasks
            .Where(t => t.AssigneeId == assigneeId && actualTotalByTaskId.ContainsKey(t.Id))
            .Select(t => t.Id)
            .ToHashSet();
        var loggedLeafIds = actualByAssigneeByTaskId.Keys.ToHashSet();

        var targetLeafIds = new HashSet<int>(ownedLeafIds);
        targetLeafIds.UnionWith(loggedLeafIds);
        if (targetLeafIds.Count == 0) return [];

        var taskById = allTasks.ToDictionary(t => t.Id);
        var keepIds = new HashSet<int>();
        foreach (var leafId in targetLeafIds)
        {
            int? id = leafId;
            while (id.HasValue)
            {
                keepIds.Add(id.Value);
                id = taskById.TryGetValue(id.Value, out var t) ? t.ParentId : null;
            }
        }

        var rows = new List<AssigneeTaskRow>();
        foreach (var node in TaskTreeHelper.GetAllNodesInDisplayOrder(roots))
        {
            if (!keepIds.Contains(node.Task.Id)) continue;

            if (node.HasChildren)
            {
                rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, true, false, null, null, null));
                continue;
            }

            if (ownedLeafIds.Contains(node.Task.Id))
            {
                var planned = node.Task.PlannedWorkDays ?? 0;
                var actual = actualTotalByTaskId.GetValueOrDefault(node.Task.Id);
                rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, true, planned, actual, planned - actual));
            }
            else
            {
                var actual = actualByAssigneeByTaskId.GetValueOrDefault(node.Task.Id);
                rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, false, null, actual, null));
            }
        }

        return rows;
    }
}
