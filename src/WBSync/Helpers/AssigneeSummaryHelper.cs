using WBSync.Models;

namespace WBSync.Helpers;

/// <summary>ダッシュボード画面向けの担当者別工数集計ユーティリティ。</summary>
internal static class AssigneeSummaryHelper
{
    /// <summary>
    /// プロジェクトの全担当者を対象に、担当者別の工数集計を行う（ステータスは問わない）。
    /// 予定工数は現在の担当者（<see cref="WbsTask.AssigneeId"/>）基準で、担当者が設定されたリーフタスクすべて
    /// （実績の有無は問わない）を対象に集計する。
    /// 実績は実績記録時点の担当者（<see cref="WorkLog.AssigneeId"/>）基準で集計する（タスクの担当が変わっても、
    /// 実績はそれを記録した本人の実績として扱うため。担当者削除により <see langword="null"/> になった実績は集計対象外）。
    /// この結果、あるタスクの現在の担当者と実績記録者が異なる場合、両者がそれぞれ一覧に表示されうる
    /// （記録者側は予定工数0・実績のみの合計になる）。
    /// 実績・担当タスクがない担当者も、予定工数0・実績0・前倒し/遅れ0として一覧に含める。
    /// 前倒し/遅れは予定工数合計 − 実績合計で求めるが、前倒し（プラス）はこの担当者の現在の担当リーフタスクが
    /// すべて「完了」の場合のみ算出し、未完了タスクがあれば 0 とする（遅れ＝マイナスは常に算出する）。
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
        var ownedLeafTasksByAssigneeId = TaskTreeHelper.GetAllLeafNodes(TaskTreeHelper.BuildTree(allTasks))
            .Select(n => n.Task)
            .Where(t => t.AssigneeId.HasValue)
            .GroupBy(t => t.AssigneeId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var actualByAssigneeId = allWorkLogs
            .Where(w => w.AssigneeId.HasValue)
            .GroupBy(w => w.AssigneeId!.Value)
            .ToDictionary(g => g.Key, g => PersonDayHelper.ToPersonDays(g.Sum(w => w.Minutes)));

        return allAssignees
            .Select(a =>
            {
                var ownedLeafTasks = ownedLeafTasksByAssigneeId.GetValueOrDefault(a.Id) ?? [];
                var planned = ownedLeafTasks.Sum(t => t.PlannedWorkDays ?? 0);
                var actual = actualByAssigneeId.GetValueOrDefault(a.Id);
                var allOwnedCompleted = ownedLeafTasks.Count > 0 && ownedLeafTasks.All(t => t.Status == "完了");
                return new AssigneeSummary(a.Id, a.Name, planned, actual, GateDelayWorkDays(planned - actual, allOwnedCompleted));
            })
            .OrderBy(s => s.AssigneeName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 担当者別集計一覧の合計行を算出する。前倒し/遅れは各担当者の（ゲート済み）値の単純合計とし、
    /// 予定工数合計 − 実績合計とは一致しない場合がある（前倒しゲートで 0 に落ちた担当者があるため）。
    /// </summary>
    /// <param name="summaries">担当者別集計結果の一覧。</param>
    /// <returns>全担当者の予定工数・実績・前倒し/遅れの合計。</returns>
    internal static AssigneeSummaryTotal BuildTotal(List<AssigneeSummary> summaries)
    {
        var planned = summaries.Sum(s => s.PlannedWorkDays);
        var actual = summaries.Sum(s => s.ActualPersonDays);
        var delay = summaries.Sum(s => s.DelayWorkDays);
        return new AssigneeSummaryTotal(planned, actual, delay);
    }

    /// <summary>
    /// 前倒し/遅れ工数（予定工数 − 実績）にダッシュボードの判定条件を適用する。
    /// マイナス（実績が予定を超過＝遅れ）はそのまま、プラス（前倒し）は <paramref name="aheadAllowed"/> が
    /// <see langword="true"/> の場合のみそのまま、それ以外は 0 とする。
    /// </summary>
    /// <param name="delayWorkDays">予定工数 − 実績（人日）。</param>
    /// <param name="aheadAllowed">前倒し（プラス）を算出してよい場合は <see langword="true"/>。</param>
    /// <returns>判定条件適用後の前倒し/遅れ工数（人日）。</returns>
    private static double GateDelayWorkDays(double delayWorkDays, bool aheadAllowed)
        => delayWorkDays < 0 ? delayWorkDays
         : delayWorkDays > 0 && aheadAllowed ? delayWorkDays
         : 0;

    /// <summary>
    /// 指定担当者が関わったタスクのツリーを構築する。「関わった」は次のいずれかを満たすリーフタスクを指す。
    /// (1) 現在この担当者が割り当てられている（<see cref="BuildSummaries"/> の予定工数集計対象と同じ）、
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
            .ToDictionary(g => g.Key, g => PersonDayHelper.ToPersonDays(g.Sum(w => w.Minutes)));
        var actualByAssigneeByTaskId = allWorkLogs
            .Where(w => w.AssigneeId == assigneeId)
            .GroupBy(w => w.TaskId)
            .ToDictionary(g => g.Key, g => PersonDayHelper.ToPersonDays(g.Sum(w => w.Minutes)));

        var ownedLeafIds = leafTasks
            .Where(t => t.AssigneeId == assigneeId)
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
                var delay = GateDelayWorkDays(planned - actual, node.Task.Status == "完了");
                rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, true, planned, actual, delay));
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
