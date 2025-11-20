using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models;

public class EventAttendee
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Required]
    public RsvpStatus RsvpStatus { get; set; }

    [Required]
    public DateTime RsvpDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
