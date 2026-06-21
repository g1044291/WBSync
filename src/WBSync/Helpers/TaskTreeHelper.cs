using WBSync.Models;

namespace WBSync.Helpers;

/// <summary>タスクノードのツリー構築・列挙ユーティリティ。</summary>
internal static class TaskTreeHelper
{
    /// <summary>フラットなタスクリストからツリー構造を構築する。</summary>
    /// <param name="tasks">対象タスクのリスト。</param>
    internal static List<TaskNode> BuildTree(List<WbsTask> tasks)
    {
        var nodeMap = tasks.ToDictionary(t => t.Id, t => new TaskNode { Task = t });
        var roots = new List<TaskNode>();

        foreach (var task in tasks.OrderBy(t => t.SortOrder))
        {
            if (task.ParentId is null)
                roots.Add(nodeMap[task.Id]);
            else if (nodeMap.TryGetValue(task.ParentId.Value, out var parent))
                parent.Children.Add(nodeMap[task.Id]);
        }

        SetLevels(roots, 0);
        return roots;
    }

    /// <summary>ノードの階層レベルを再帰的に設定する。</summary>
    /// <param name="nodes">対象ノード群。</param>
    /// <param name="level">現在の階層レベル。</param>
    internal static void SetLevels(List<TaskNode> nodes, int level)
    {
        foreach (var node in nodes)
        {
            node.Level = level;
            SetLevels(node.Children, level + 1);
        }
    }

    /// <summary>展開状態によらず全ノードを DFS 順で列挙する（WBS 表示順）。</summary>
    /// <param name="roots">ルートノード群。</param>
    internal static IEnumerable<TaskNode> GetAllNodesInDisplayOrder(List<TaskNode> roots)
    {
        foreach (var node in roots)
        {
            yield return node;
            foreach (var child in GetAllNodesInDisplayOrder(node.Children))
                yield return child;
        }
    }

    /// <summary>展開状態を考慮して表示対象のノードを列挙する。</summary>
    /// <param name="roots">ルートノード群。</param>
    internal static IEnumerable<TaskNode> GetVisibleNodes(List<TaskNode> roots)
    {
        foreach (var node in roots)
        {
            yield return node;
            if (node.IsExpanded && node.HasChildren)
                foreach (var child in GetVisibleNodes(node.Children))
                    yield return child;
        }
    }

    /// <summary>指定ノード群からリーフノードをすべて列挙する。</summary>
    /// <param name="nodes">検索対象のノード群。</param>
    internal static IEnumerable<TaskNode> GetAllLeafNodes(List<TaskNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!node.HasChildren)
                yield return node;
            else
                foreach (var leaf in GetAllLeafNodes(node.Children))
                    yield return leaf;
        }
    }
}
