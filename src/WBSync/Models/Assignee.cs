using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

[Table("assignees")]
public class Assignee
{
    [Column("id")]
    public int Id { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    public Project Project { get; set; } = null!;
    public ICollection<WbsTask> Tasks { get; set; } = [];
    public ICollection<AssigneeHoliday> Holidays { get; set; } = [];
}
