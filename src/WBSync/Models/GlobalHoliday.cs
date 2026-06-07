using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

/// <summary>全体休日エンティティ（祝日・特別休日）。日付は一意制約あり。</summary>
[Table("global_holidays")]
public class GlobalHoliday
{
    /// <summary>休日ID。</summary>
    [Column("id")]
    public int Id { get; set; }

    /// <summary>休日の日付（yyyy-MM-dd 形式）。一意制約あり。</summary>
    [Column("date")]
    [Required]
    public string Date { get; set; } = string.Empty;

    /// <summary>休日名（例：元日）。省略可。</summary>
    [Column("name")]
    public string? Name { get; set; }
}
