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

    /// <summary>直接の子タスクの見積工数（人日）の合計。子タスクを持たない場合は <see langword="null"/>。</summary>
    public double? DirectChildrenWorkDaysSum => HasChildren
        ? Children.Sum(c => c.Task.WorkDays ?? 0)
        : null;

    /// <summary>直接の子タスクの見積工数合計が親タスク自身の見積工数を超えているかどうか。</summary>
    public bool HasWorkDaysOverflow =>
        HasChildren && Task.WorkDays.HasValue && DirectChildrenWorkDaysSum > Task.WorkDays.Value;

    /// <summary>見積工数の超過分（人日）。超過していない場合は <see langword="null"/>。</summary>
    public double? WorkDaysOverflowAmount => HasWorkDaysOverflow
        ? DirectChildrenWorkDaysSum - Task.WorkDays!.Value
        : null;
}
