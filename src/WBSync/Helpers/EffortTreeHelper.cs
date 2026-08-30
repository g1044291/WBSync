using WBSync.Models;

namespace WBSync.Helpers;

/// <summary>工数管理画面向けの集計・フィルタ・並び替えユーティリティ。既存の <see cref="TaskNode"/>/<see cref="TaskTreeHelper"/> は変更しない。</summary>
internal static class EffortTreeHelper
{
    /// <summary>
    /// タスクツリー全体の見積工数・予定工数・実績・残工数・遅れ日数を集計する。
    /// リーフタスクは自身の値、親タスクは全子孫タスクからの動的集計値を持つ。
    /// ただし親タスク自身に見積工数・予定工数が設定されている場合は、その値を集計値より優先する。
    /// </summary>
    /// <param name="roots">ルートノード群。</param>
    /// <param name="actualPersonDaysByTaskId">タスクIDをキーとした実績（人日）の辞書。</param>
    /// <param name="delayDaysByTaskId">タスクIDをキーとした遅れ日数の辞書。</param>
    /// <returns>タスクIDをキーとした集計結果の辞書。</returns>
    internal static Dictionary<int, EffortAggregate> BuildAggregates(
        List<TaskNode> roots,
        IReadOnlyDictionary<int, double> actualPersonDaysByTaskId,
        IReadOnlyDictionary<int, int> delayDaysByTaskId)
    {
        var result = new Dictionary<int, EffortAggregate>();
        foreach (var root in roots)
            BuildNodeAggregate(root, actualPersonDaysByTaskId, delayDaysByTaskId, result);
        return result;
    }

    /// <summary>
    /// 1ノード分の集計値をボトムアップで算出し、結果辞書に格納する。
    /// 親タスクの見積工数・予定工数は、そのタスク自身に値が設定されていればその値、未設定なら子孫タスクの合計を用いる。
    /// 残工数は表示される予定工数（自身の値 or 集計値）と実績集計値から算出する。
    /// </summary>
    /// <param name="node">対象ノード。</param>
    /// <param name="actualPersonDaysByTaskId">タスクIDをキーとした実績（人日）の辞書。</param>
    /// <param name="delayDaysByTaskId">タスクIDをキーとした遅れ日数の辞書。</param>
    /// <param name="result">集計結果を格納する辞書。</param>
    /// <returns>このノードの集計結果。</returns>
    private static EffortAggregate BuildNodeAggregate(
        TaskNode node,
        IReadOnlyDictionary<int, double> actualPersonDaysByTaskId,
        IReadOnlyDictionary<int, int> delayDaysByTaskId,
        Dictionary<int, EffortAggregate> result)
    {
        EffortAggregate aggregate;

        if (!node.HasChildren)
        {
            var estimate = node.Task.WorkDays ?? 0;
            var planned = node.Task.PlannedWorkDays ?? 0;
            var actual = actualPersonDaysByTaskId.GetValueOrDefault(node.Task.Id);
            var remaining = node.Task.PlannedWorkDays.HasValue ? planned - actual : (double?)null;
            var delay = delayDaysByTaskId.TryGetValue(node.Task.Id, out var d) ? d : (int?)null;
            aggregate = new EffortAggregate(estimate, planned, actual, remaining, delay);
        }
        else
        {
            double childEstimate = 0, childPlanned = 0, actual = 0;
            int? maxDelay = null;

            foreach (var child in node.Children)
            {
                var childAggregate = BuildNodeAggregate(child, actualPersonDaysByTaskId, delayDaysByTaskId, result);
                childEstimate += childAggregate.EstimateWorkDays;
                childPlanned += childAggregate.PlannedWorkDays;
                actual += childAggregate.ActualPersonDays;
                if (childAggregate.DelayDays.HasValue)
                    maxDelay = maxDelay.HasValue ? Math.Max(maxDelay.Value, childAggregate.DelayDays.Value) : childAggregate.DelayDays.Value;
            }

            // 親タスク自身に値が設定されていればそれを優先し、未設定なら子孫タスクの合計を用いる。
            var estimate = node.Task.WorkDays ?? childEstimate;
            var planned = node.Task.PlannedWorkDays ?? childPlanned;

            aggregate = new EffortAggregate(estimate, planned, actual, planned - actual, maxDelay);
        }

        result[node.Task.Id] = aggregate;
        return aggregate;
    }

    /// <summary>
    /// 担当者フィルター・遅れフィルターに一致するリーフタスクと、その祖先タスクのIDセットを返す。
    /// 一致するリーフを子孫に持たない親タスクは含まれない。
    /// </summary>
    /// <param name="allTasks">プロジェクト内の全タスク（祖先を辿るために使用）。</param>
    /// <param name="roots">ルートノード群。</param>
    /// <param name="aggregates">タスクIDをキーとした集計結果の辞書。</param>
    /// <param name="filterAssigneeId">担当者での絞り込み。<see langword="null"/> の場合は絞り込まない。</param>
    /// <param name="filterDelay">遅れ状況での絞り込み。</param>
    /// <returns>表示を維持するタスクIDのセット。</returns>
    internal static HashSet<int> BuildFilterKeepSet(
        List<WbsTask> allTasks,
        List<TaskNode> roots,
        Dictionary<int, EffortAggregate> aggregates,
        int? filterAssigneeId,
        DelayFilter filterDelay)
    {
        var taskById = allTasks.ToDictionary(t => t.Id);
        var keep = new HashSet<int>();

        foreach (var leaf in TaskTreeHelper.GetAllLeafNodes(roots))
        {
            if (filterAssigneeId.HasValue && leaf.Task.AssigneeId != filterAssigneeId.Value) continue;

            var delayDays = aggregates.TryGetValue(leaf.Task.Id, out var agg) ? agg.DelayDays : null;
            var matchesDelay = filterDelay switch
            {
                DelayFilter.Delayed => delayDays is > 0,
                DelayFilter.NotDelayed => delayDays is null or <= 0,
                _ => true
            };
            if (!matchesDelay) continue;

            int? id = leaf.Task.Id;
            while (id.HasValue)
            {
                keep.Add(id.Value);
                id = taskById.TryGetValue(id.Value, out var t) ? t.ParentId : null;
            }
        }

        return keep;
    }

    /// <summary>
    /// フィルター・並び替え・折りたたみ状態を反映した表示対象ノードを、表示順に列挙する。
    /// 兄弟ノードのみを指定キーで並び替え、親子関係・インデントは維持する。
    /// </summary>
    /// <param name="roots">ルートノード群。</param>
    /// <param name="aggregates">タスクIDをキーとした集計結果の辞書。</param>
    /// <param name="assigneeNames">タスクIDではなく担当者IDをキーとした担当者名の辞書。</param>
    /// <param name="sortMode">並び替えキー。</param>
    /// <param name="collapsedTaskIds">折りたたみ中のタスクIDセット。</param>
    /// <param name="keepTaskIds">表示を維持するタスクIDセット（フィルター結果）。</param>
    /// <returns>表示対象ノードの列挙（表示順）。</returns>
    internal static List<TaskNode> GetVisibleSortedFilteredNodes(
        List<TaskNode> roots,
        Dictionary<int, EffortAggregate> aggregates,
        IReadOnlyDictionary<int, string> assigneeNames,
        SortMode sortMode,
        HashSet<int> collapsedTaskIds,
        HashSet<int> keepTaskIds)
    {
        var result = new List<TaskNode>();
        AppendVisible(OrderSiblings(roots, aggregates, assigneeNames, sortMode), aggregates, assigneeNames, sortMode, collapsedTaskIds, keepTaskIds, result);
        return result;
    }

    /// <summary>並び替え済みの兄弟ノード群を再帰的に列挙結果へ追加する。</summary>
    private static void AppendVisible(
        IEnumerable<TaskNode> orderedSiblings,
        Dictionary<int, EffortAggregate> aggregates,
        IReadOnlyDictionary<int, string> assigneeNames,
        SortMode sortMode,
        HashSet<int> collapsedTaskIds,
        HashSet<int> keepTaskIds,
        List<TaskNode> result)
    {
        foreach (var node in orderedSiblings)
        {
            if (!keepTaskIds.Contains(node.Task.Id)) continue;

            result.Add(node);

            if (node.HasChildren && !collapsedTaskIds.Contains(node.Task.Id))
                AppendVisible(OrderSiblings(node.Children, aggregates, assigneeNames, sortMode), aggregates, assigneeNames, sortMode, collapsedTaskIds, keepTaskIds, result);
        }
    }

    /// <summary>
    /// 兄弟ノード群を指定キーで並び替えた新しい列挙を返す（元の <see cref="TaskNode.Children"/> はミューテートしない）。
    /// </summary>
    private static IEnumerable<TaskNode> OrderSiblings(
        List<TaskNode> nodes,
        Dictionary<int, EffortAggregate> aggregates,
        IReadOnlyDictionary<int, string> assigneeNames,
        SortMode sortMode) => sortMode switch
        {
            SortMode.AssigneeName => nodes
                .OrderBy(n => n.Task.AssigneeId is null)
                .ThenBy(n => n.Task.AssigneeId.HasValue ? assigneeNames.GetValueOrDefault(n.Task.AssigneeId.Value, string.Empty) : string.Empty, StringComparer.Ordinal),
            SortMode.DelayDays => nodes
                .OrderBy(n => !(aggregates.TryGetValue(n.Task.Id, out var agg) && agg.DelayDays.HasValue))
                .ThenByDescending(n => aggregates.TryGetValue(n.Task.Id, out var agg) ? agg.DelayDays ?? int.MinValue : int.MinValue),
            _ => nodes
        };
}
