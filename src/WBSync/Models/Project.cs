using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

[Table("projects")]
public class Project
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [Column("start_date")]
    [Required]
    public string StartDate { get; set; } = string.Empty;

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    public ICollection<Assignee> Assignees { get; set; } = [];
    public ICollection<WbsTask> Tasks { get; set; } = [];
}
