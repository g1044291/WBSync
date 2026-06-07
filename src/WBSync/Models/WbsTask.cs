using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

/// <summary>WBS タスクエンティティ。無制限階層の親子関係と FS 依存（先行タスク）をサポートする。</summary>
[Table("tasks")]
public class WbsTask
{
    /// <summary>タスクID。</summary>
    [Column("id")]
    public int Id { get; set; }

    /// <summary>所属プロジェクトID。</summary>
    [Column("project_id")]
    public int ProjectId { get; set; }

    /// <summary>親タスクID。ルートタスクの場合は <see langword="null"/>。</summary>
    [Column("parent_id")]
    public int? ParentId { get; set; }

    /// <summary>先行タスクID（FS 依存）。依存なしの場合は <see langword="null"/>。</summary>
    [Column("predecessor_id")]
    public int? PredecessorId { get; set; }

    /// <summary>担当者ID。未割り当ての場合は <see langword="null"/>。</summary>
    [Column("assignee_id")]
    public int? AssigneeId { get; set; }

    /// <summary>タスク名。</summary>
    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>工数（人日）。未設定の場合は <see langword="null"/>。</summary>
    [Column("work_days")]
    public double? WorkDays { get; set; }

    /// <summary>開始日（yyyy-MM-dd 形式）。親タスクは DB に保存せず <see langword="null"/>。</summary>
    [Column("start_date")]
    public string? StartDate { get; set; }

    /// <summary>終了日（yyyy-MM-dd 形式）。親タスクは DB に保存せず <see langword="null"/>。</summary>
    [Column("end_date")]
    public string? EndDate { get; set; }

    /// <summary>ステータス（未着手 / 進行中 / 完了 / 保留）。</summary>
    [Column("status")]
    [Required]
    public string Status { get; set; } = "未着手";

    /// <summary>進捗率（0〜100）。</summary>
    [Column("progress")]
    public int Progress { get; set; }

    /// <summary>備考。</summary>
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>同階層内の表示順。</summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

    /// <summary>作成日時（yyyy-MM-dd HH:mm:ss 形式）。</summary>
    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>更新日時（yyyy-MM-dd HH:mm:ss 形式）。</summary>
    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>所属プロジェクト。</summary>
    public Project Project { get; set; } = null!;

    /// <summary>親タスク。</summary>
    public WbsTask? Parent { get; set; }

    /// <summary>先行タスク。</summary>
    public WbsTask? Predecessor { get; set; }

    /// <summary>担当者。</summary>
    public Assignee? Assignee { get; set; }

    /// <summary>子タスクのコレクション。</summary>
    public ICollection<WbsTask> Children { get; set; } = [];
}
