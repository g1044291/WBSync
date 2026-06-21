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

    /// <summary>デフォルト生産性係数。標準を 1.0 とし、0 より大きい値を設定する。</summary>
    [Column("productivity_coefficient")]
    public decimal ProductivityCoefficient { get; set; } = 1.0m;

    /// <summary>このグローバル担当者を参照するプロジェクト内担当者のコレクション。</summary>
    public ICollection<Assignee> ProjectAssignees { get; set; } = [];
}
