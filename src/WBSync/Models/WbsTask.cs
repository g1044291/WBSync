using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

[Table("tasks")]
public class WbsTask
{
    [Column("id")]
    public int Id { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [Column("predecessor_id")]
    public int? PredecessorId { get; set; }

    [Column("assignee_id")]
    public int? AssigneeId { get; set; }

    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [Column("work_days")]
    public double? WorkDays { get; set; }

    [Column("start_date")]
    public string? StartDate { get; set; }

    [Column("end_date")]
    public string? EndDate { get; set; }

    [Column("status")]
    [Required]
    public string Status { get; set; } = "未着手";

    [Column("progress")]
    public int Progress { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    public Project Project { get; set; } = null!;
    public WbsTask? Parent { get; set; }
    public WbsTask? Predecessor { get; set; }
    public Assignee? Assignee { get; set; }
    public ICollection<WbsTask> Children { get; set; } = [];
}
