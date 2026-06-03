using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WBSync.Models;

[Table("global_holidays")]
public class GlobalHoliday
{
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    [Required]
    public string Date { get; set; } = string.Empty;

    [Column("name")]
    public string? Name { get; set; }
}
