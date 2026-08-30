using WBSync.Models;

namespace WBSync.Helpers;

/// <summary>ダッシュボード画面向けの担当者別工数集計ユーティリティ。</summary>
internal static class AssigneeSummaryHelper
{
    /// <summary>
    /// 実績ログを集計期間で絞り込む。開始日・終了日はいずれも任意で、指定した側のみ境界として使う（両端を含む）。
    /// 両方 <see langword="null"/> の場合は絞り込みを行わず <paramref name="allWorkLogs"/> をそのまま返す。
    /// </summary>
    /// <param name="allWorkLogs">絞り込み対象の全実績ログ。</param>
    /// <param name="periodStart">集計期間の開始日（この日を含む）。<see langword="null"/> の場合は下限なし。</param>
    /// <param name="periodEnd">集計期間の終了日（この日を含む）。<see langword="null"/> の場合は上限なし。</param>
    /// <returns>期間内に記録された実績ログのみを含む一覧（絞り込み不要時は入力をそのまま返す）。</returns>
    /// <remarks><see cref="WorkLog.Date"/> は <c>yyyy-MM-dd</c> 形式のため、辞書順比較が日付順比較と一致する。</remarks>
    internal static List<WorkLog> FilterByPeriod(List<WorkLog> allWorkLogs, DateOnly? periodStart, DateOnly? periodEnd)
    {
        if (periodStart is null && periodEnd is null) return allWorkLogs;

        var startStr = periodStart?.ToString("yyyy-MM-dd");
        var endStr = periodEnd?.ToString("yyyy-MM-dd");
        return allWorkLogs
            .Where(w => (startStr is null || string.CompareOrdinal(w.Date, startStr) >= 0)
                     && (endStr is null || string.CompareOrdinal(w.Date, endStr) <= 0))
            .ToList();
    }

    /// <summary>
    /// プロジェクトの全担当者を対象に、担当者別の工数集計を行う（ステータスは問わない）。
    /// 予定工数は現在の担当者（<see cref="WbsTask.AssigneeId"/>）基準で、担当者が設定されたリーフタスクすべて
    /// （実績の有無は問わない）を対象に集計する。
    /// 実績は実績記録時点の担当者（<see cref="WorkLog.AssigneeId"/>）基準で集計する（タスクの担当が変わっても、
    /// 実績はそれを記録した本人の実績として扱うため。担当者削除により <see langword="null"/> になった実績は集計対象外）。
    /// この結果、あるタスクの現在の担当者と実績記録者が異なる場合、両者がそれぞれ一覧に表示されうる
    /// （記録者側は予定工数0・実績のみの合計になる）。
    /// 実績・担当タスクがない担当者も、予定工数0・実績0・遅れ0として一覧に含める。
    /// <paramref name="includePlanned"/> が <see langword="false"/> の場合（集計期間指定時）は予定工数・遅れを算出せず
    /// <see langword="null"/> とし、期間で絞り込み済みの <paramref name="allWorkLogs"/> による実績のみを集計する。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="allWorkLogs">集計対象の実績ログ（集計期間指定時は<see cref="FilterByPeriod"/>で絞り込み済みを渡す）。</param>
    /// <param name="allAssignees">プロジェクト内の全担当者。</param>
    /// <param name="includePlanned">予定工数・遅れを算出する場合は <see langword="true"/>（既定）。集計期間指定時は <see langword="false"/>。</param>
    /// <returns>担当者名の五十音順に並んだ集計結果の一覧。</returns>
    internal static List<AssigneeSummary> BuildSummaries(
        List<WbsTask> allTasks,
        List<WorkLog> allWorkLogs,
        List<Assignee> allAssignees,
        bool includePlanned = true)
    {
        var plannedByAssigneeId = includePlanned
            ? TaskTreeHelper.GetAllLeafNodes(TaskTreeHelper.BuildTree(allTasks))
                .Select(n => n.Task)
                .Where(t => t.AssigneeId.HasValue)
                .GroupBy(t => t.AssigneeId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.PlannedWorkDays ?? 0))
            : null;

        var actualByAssigneeId = allWorkLogs
            .Where(w => w.AssigneeId.HasValue)
            .GroupBy(w => w.AssigneeId!.Value)
            .ToDictionary(g => g.Key, g => PersonDayHelper.ToPersonDays(g.Sum(w => w.Minutes)));

        return allAssignees
            .Select(a =>
            {
                var actual = actualByAssigneeId.GetValueOrDefault(a.Id);
                if (plannedByAssigneeId is null)
                    return new AssigneeSummary(a.Id, a.Name, null, actual, null);
                var planned = plannedByAssigneeId.GetValueOrDefault(a.Id);
                return new AssigneeSummary(a.Id, a.Name, planned, actual, planned - actual);
            })
            .OrderBy(s => s.AssigneeName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>担当者別集計一覧の合計行を算出する。</summary>
    /// <param name="summaries">担当者別集計結果の一覧。</param>
    /// <param name="includePlanned">予定工数・遅れを合算する場合は <see langword="true"/>（既定）。集計期間指定時は <see langword="false"/>（予定工数・遅れは <see langword="null"/>）。</param>
    /// <returns>全担当者の予定工数・実績・遅れの合計。</returns>
    internal static AssigneeSummaryTotal BuildTotal(List<AssigneeSummary> summaries, bool includePlanned = true)
    {
        var actual = summaries.Sum(s => s.ActualPersonDays);
        if (!includePlanned) return new AssigneeSummaryTotal(null, actual, null);
        var planned = summaries.Sum(s => s.PlannedWorkDays ?? 0);
        return new AssigneeSummaryTotal(planned, actual, planned - actual);
    }

    /// <summary>
    /// 指定担当者が関わったタスクのツリーを構築する。「関わった」は次のいずれかを満たすリーフタスクを指す。
    /// (1) 現在この担当者が割り当てられている（<see cref="BuildSummaries"/> の予定工数集計対象と同じ）、
    /// (2) 現在の担当者に関わらず、この担当者自身の実績ログ（<see cref="WorkLog.AssigneeId"/>）が記録されている。
    /// 対象リーフタスクの祖先タスクも、ツリー構造を保つために含める。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="allWorkLogs">集計対象の実績ログ（集計期間指定時は<see cref="FilterByPeriod"/>で絞り込み済みを渡す）。</param>
    /// <param name="assigneeId">対象担当者ID。</param>
    /// <param name="includePlanned">現在の担当タスク行に予定工数・遅れを表示する場合は <see langword="true"/>（既定）。集計期間指定時は <see langword="false"/> とし、全行で実績のみを表示する。</param>
    /// <returns>WBS表示順に並んだタスクツリーの行一覧。関わったタスクが1件もない場合は空リスト。</returns>
    internal static List<AssigneeTaskRow> BuildAssigneeTaskRows(
        List<WbsTask> allTasks,
        List<WorkLog> allWorkLogs,
        int assigneeId,
        bool includePlanned = true)
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
                var actual = actualTotalByTaskId.GetValueOrDefault(node.Task.Id);
                if (includePlanned)
                {
                    var planned = node.Task.PlannedWorkDays ?? 0;
                    rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, true, planned, actual, planned - actual));
                }
                else
                {
                    rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, true, null, actual, null));
                }
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
