using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SamMALsurium.Models;

public class EventAnnouncement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string SentBy { get; set; } = string.Empty;

    [ForeignKey(nameof(SentBy))]
    public ApplicationUser? Sender { get; set; }

    [Required]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
