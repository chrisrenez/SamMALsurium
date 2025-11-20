using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SamMALsurium.Models;

public class EventArtwork
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    public int ArtworkId { get; set; }

    // Note: Artwork entity is a placeholder for future artwork feature
    // This foreign key relationship will be configured when the Artwork feature is implemented

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
