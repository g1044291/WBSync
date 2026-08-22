using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

/// <summary>日々の作業実績エンティティ。工数管理画面・集計ダッシュボードの基盤。</summary>
[Table("work_logs")]
public class WorkLog
{
    /// <summary>作業実績ID。</summary>
    [Column("id")]
    public int Id { get; set; }

    /// <summary>対象タスクID。</summary>
    [Column("task_id")]
    public int TaskId { get; set; }

    /// <summary>担当者ID。作業時点の担当者が変わる場合を考慮し、担当者削除時は <see langword="null"/> になる。</summary>
    [Column("assignee_id")]
    public int? AssigneeId { get; set; }

    /// <summary>作業日（yyyy-MM-dd 形式）。</summary>
    [Column("date")]
    [Required]
    public string Date { get; set; } = string.Empty;

    /// <summary>作業時間（分単位）。</summary>
    [Column("minutes")]
    public int Minutes { get; set; }

    /// <summary>備忘用の任意コメント。</summary>
    [Column("comment")]
    public string? Comment { get; set; }

    /// <summary>対象タスク。</summary>
    public WbsTask Task { get; set; } = null!;

    /// <summary>担当者。未割り当ての場合は <see langword="null"/>。</summary>
    public Assignee? Assignee { get; set; }
}
