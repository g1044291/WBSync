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
    /// 実績・担当タスクがない担当者も、予定工数0・実績0・前倒し/遅れ0として一覧に含める。
    /// 前倒し/遅れは予定工数合計 − 実績合計で求めるが、前倒し（プラス）はこの担当者の現在の担当リーフタスクが
    /// すべて「完了」の場合のみ算出し、未完了タスクがあれば 0 とする（遅れ＝マイナスは常に算出する）。
    /// 見積工数は現在の担当リーフタスクの <see cref="WbsTask.WorkDays"/> の合計、残工数は予定工数合計 − 実績合計。
    /// 期間（開始日・終了日）は現在の担当リーフタスクの開始日の最小値・終了日の最大値（<paramref name="includePlanned"/> に関わらず算出する）。
    /// <paramref name="includePlanned"/> が <see langword="false"/> の場合（集計期間指定時）は見積工数・予定工数・残工数・前倒し/遅れを算出せず
    /// <see langword="null"/> とし、期間で絞り込み済みの <paramref name="allWorkLogs"/> による実績のみを集計する。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="allWorkLogs">集計対象の実績ログ（集計期間指定時は<see cref="FilterByPeriod"/>で絞り込み済みを渡す）。</param>
    /// <param name="allAssignees">プロジェクト内の全担当者。</param>
    /// <param name="includePlanned">見積工数・予定工数・残工数・前倒し/遅れを算出する場合は <see langword="true"/>（既定）。集計期間指定時は <see langword="false"/>（期間・実績のみ算出）。</param>
    /// <returns>担当者名の五十音順に並んだ集計結果の一覧。</returns>
    internal static List<AssigneeSummary> BuildSummaries(
        List<WbsTask> allTasks,
        List<WorkLog> allWorkLogs,
        List<Assignee> allAssignees,
        bool includePlanned = true)
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
                var actual = actualByAssigneeId.GetValueOrDefault(a.Id);
                var ownedLeafTasks = ownedLeafTasksByAssigneeId.GetValueOrDefault(a.Id) ?? [];
                var start = ownedLeafTasks.Select(t => t.StartDate).Where(d => !string.IsNullOrEmpty(d)).Min();
                var end = ownedLeafTasks.Select(t => t.EndDate).Where(d => !string.IsNullOrEmpty(d)).Max();

                if (!includePlanned)
                    return new AssigneeSummary(a.Id, a.Name, start, end, null, null, actual, null, null);

                var estimate = ownedLeafTasks.Sum(t => t.WorkDays ?? 0);
                var planned = ownedLeafTasks.Sum(t => t.PlannedWorkDays ?? 0);
                var allOwnedCompleted = ownedLeafTasks.Count > 0 && ownedLeafTasks.All(t => t.Status == "完了");
                return new AssigneeSummary(a.Id, a.Name, start, end, estimate, planned, actual, planned - actual, GateDelayWorkDays(planned - actual, allOwnedCompleted));
            })
            .OrderBy(s => s.AssigneeName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 担当者別集計一覧の合計行を算出する。前倒し/遅れは各担当者の（ゲート済み）値の単純合計とし、
    /// 予定工数合計 − 実績合計とは一致しない場合がある（前倒しゲートで 0 に落ちた担当者があるため）。
    /// </summary>
    /// <param name="summaries">担当者別集計結果の一覧。</param>
    /// <param name="includePlanned">見積工数・予定工数・残工数・前倒し/遅れを合算する場合は <see langword="true"/>（既定）。集計期間指定時は <see langword="false"/>（これらは <see langword="null"/>）。</param>
    /// <returns>全担当者の期間・見積工数・予定工数・実績・残工数・前倒し/遅れの合計。</returns>
    internal static AssigneeSummaryTotal BuildTotal(List<AssigneeSummary> summaries, bool includePlanned = true)
    {
        var actual = summaries.Sum(s => s.ActualPersonDays);
        var start = summaries.Select(s => s.StartDate).Where(d => !string.IsNullOrEmpty(d)).Min();
        var end = summaries.Select(s => s.EndDate).Where(d => !string.IsNullOrEmpty(d)).Max();
        if (!includePlanned) return new AssigneeSummaryTotal(start, end, null, null, actual, null, null);
        var estimate = summaries.Sum(s => s.EstimateWorkDays ?? 0);
        var planned = summaries.Sum(s => s.PlannedWorkDays ?? 0);
        var remaining = summaries.Sum(s => s.RemainingWorkDays ?? 0);
        var delay = summaries.Sum(s => s.DelayWorkDays ?? 0);
        return new AssigneeSummaryTotal(start, end, estimate, planned, actual, remaining, delay);
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
    /// 各行には期間（開始日〜終了日）とステータスも持たせる（子タスクを持つ行の期間は子孫からの動的計算値、ステータスは <see langword="null"/>）。
    /// 見積工数・残工数は現在の担当タスク行（<see cref="AssigneeTaskRow.IsOwned"/>）のみ算出し、集計期間指定時は <see langword="null"/> とする。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク。</param>
    /// <param name="allWorkLogs">集計対象の実績ログ（集計期間指定時は<see cref="FilterByPeriod"/>で絞り込み済みを渡す）。</param>
    /// <param name="assigneeId">対象担当者ID。</param>
    /// <param name="includePlanned">現在の担当タスク行に見積工数・予定工数・残工数・前倒し/遅れを表示する場合は <see langword="true"/>（既定）。集計期間指定時は <see langword="false"/> とし、全行で期間・ステータス・実績のみを表示する。</param>
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
                rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, true, false,
                    node.DisplayStartDate, node.DisplayEndDate, null, null, null, null, null, null));
                continue;
            }

            if (ownedLeafIds.Contains(node.Task.Id))
            {
                var actual = actualTotalByTaskId.GetValueOrDefault(node.Task.Id);
                if (includePlanned)
                {
                    var estimate = node.Task.WorkDays ?? 0;
                    var planned = node.Task.PlannedWorkDays ?? 0;
                    var remaining = node.Task.PlannedWorkDays.HasValue ? planned - actual : (double?)null;
                    var delay = GateDelayWorkDays(planned - actual, node.Task.Status == "完了");
                    rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, true,
                        node.Task.StartDate, node.Task.EndDate, node.Task.Status, estimate, planned, actual, remaining, delay));
                }
                else
                {
                    rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, true,
                        node.Task.StartDate, node.Task.EndDate, node.Task.Status, null, null, actual, null, null));
                }
            }
            else
            {
                var actual = actualByAssigneeByTaskId.GetValueOrDefault(node.Task.Id);
                rows.Add(new AssigneeTaskRow(node.Task.Id, node.Task.Name, node.Level, false, false,
                    node.Task.StartDate, node.Task.EndDate, node.Task.Status, null, null, actual, null, null));
            }
        }

        return rows;
    }
}
