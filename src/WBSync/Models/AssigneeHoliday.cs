using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

/// <summary>担当者個人休日エンティティ。同一担当者内で日付は一意。</summary>
[Table("assignee_holidays")]
public class AssigneeHoliday
{
    /// <summary>個人休日ID。</summary>
    [Column("id")]
    public int Id { get; set; }

    /// <summary>担当者ID。</summary>
    [Column("assignee_id")]
    public int AssigneeId { get; set; }

    /// <summary>休日の日付（yyyy-MM-dd 形式）。同一担当者内で一意。</summary>
    [Column("date")]
    [Required]
    public string Date { get; set; } = string.Empty;

    /// <summary>メモ（例：有給休暇）。省略可。</summary>
    [Column("memo")]
    public string? Memo { get; set; }

    /// <summary>所属担当者。</summary>
    public Assignee Assignee { get; set; } = null!;
}
