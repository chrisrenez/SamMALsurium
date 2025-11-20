using System.ComponentModel.DataAnnotations;

namespace SamMALsurium.Models;

public class EventType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Navigation property
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
