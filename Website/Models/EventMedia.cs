using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models;

public class EventMedia
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    public EventMediaType MediaType { get; set; }

    [MaxLength(1000)]
    public string? FilePath { get; set; }

    [MaxLength(2000)]
    public string? Url { get; set; }

    [Required]
    public int DisplayOrder { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
