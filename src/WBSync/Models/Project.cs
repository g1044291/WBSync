using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

/// <summary>プロジェクトエンティティ。</summary>
[Table("projects")]
public class Project
{
    /// <summary>プロジェクトID。</summary>
    [Column("id")]
    public int Id { get; set; }

    /// <summary>プロジェクト名。</summary>
    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>プロジェクト開始日（yyyy-MM-dd 形式）。</summary>
    [Column("start_date")]
    [Required]
    public string StartDate { get; set; } = string.Empty;

    /// <summary>作成日時（yyyy-MM-dd HH:mm:ss 形式）。</summary>
    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>更新日時（yyyy-MM-dd HH:mm:ss 形式）。</summary>
    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>このプロジェクトに属する担当者のコレクション。</summary>
    public ICollection<Assignee> Assignees { get; set; } = [];

    /// <summary>このプロジェクトに属するタスクのコレクション。</summary>
    public ICollection<WbsTask> Tasks { get; set; } = [];
}
