using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

/// <summary>担当者エンティティ。</summary>
[Table("assignees")]
public class Assignee
{
    /// <summary>担当者ID。</summary>
    [Column("id")]
    public int Id { get; set; }

    /// <summary>所属プロジェクトID。</summary>
    [Column("project_id")]
    public int ProjectId { get; set; }

    /// <summary>担当者名。</summary>
    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>グローバル担当者マスタへの参照ID。NULL の場合はプロジェクト専用担当者。</summary>
    [Column("global_assignee_id")]
    public int? GlobalAssigneeId { get; set; }

    /// <summary>プロジェクト内での生産性係数。グローバルマスタからの引き継ぎ値をオーバーライドできる。</summary>
    [Column("productivity_coefficient")]
    public decimal ProductivityCoefficient { get; set; } = 1.0m;

    /// <summary>同プロジェクト内の表示順。</summary>
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

    /// <summary>参照するグローバル担当者マスタ。NULL の場合はプロジェクト専用。</summary>
    public GlobalAssignee? GlobalAssignee { get; set; }

    /// <summary>この担当者に割り当てられたタスクのコレクション。</summary>
    public ICollection<WbsTask> Tasks { get; set; } = [];

    /// <summary>この担当者の個人休日コレクション。</summary>
    public ICollection<AssigneeHoliday> Holidays { get; set; } = [];

    /// <summary>この担当者の作業実績コレクション。</summary>
    public ICollection<WorkLog> WorkLogs { get; set; } = [];
}
