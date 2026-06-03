using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

[Table("assignee_holidays")]
public class AssigneeHoliday
{
    [Column("id")]
    public int Id { get; set; }

    [Column("assignee_id")]
    public int AssigneeId { get; set; }

    [Column("date")]
    [Required]
    public string Date { get; set; } = string.Empty;

    [Column("memo")]
    public string? Memo { get; set; }

    public Assignee Assignee { get; set; } = null!;
}
