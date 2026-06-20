using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

/// <summary>アプリ全体で共有するグローバル担当者マスタ。</summary>
[Table("global_assignees")]
public class GlobalAssignee
{
    /// <summary>グローバル担当者ID。</summary>
    [Column("id")]
    public int Id { get; set; }

    /// <summary>担当者名。</summary>
    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>このグローバル担当者を参照するプロジェクト内担当者のコレクション。</summary>
    public ICollection<Assignee> ProjectAssignees { get; set; } = [];
}
