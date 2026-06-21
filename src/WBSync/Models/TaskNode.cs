namespace WBSync.Models;

/// <summary>ガントチャートのツリー表示用ノード。</summary>
internal class TaskNode
{
    /// <summary>対応する WBS タスク。</summary>
    public WbsTask Task { get; init; } = null!;

    /// <summary>子ノードのコレクション。</summary>
    public List<TaskNode> Children { get; } = [];

    /// <summary>ルートから数えた階層レベル（0 始まり）。</summary>
    public int Level { get; set; }

    /// <summary>子ノードが展開表示されているかどうか。</summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>子ノードを持つかどうか。</summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>表示用の開始日。親タスクの場合は子の最小値を動的に返す。</summary>
    public string? DisplayStartDate => HasChildren
        ? Children.Select(c => c.DisplayStartDate).Where(d => d is not null).Min()
        : Task.StartDate;

    /// <summary>表示用の終了日。親タスクの場合は子の最大値を動的に返す。</summary>
    public string? DisplayEndDate => HasChildren
        ? Children.Select(c => c.DisplayEndDate).Where(d => d is not null).Max()
        : Task.EndDate;
}
