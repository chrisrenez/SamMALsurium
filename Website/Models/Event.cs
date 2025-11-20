using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SamMALsurium.Models;

public class Event
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(200)]
    public string? LocationName { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [Required]
    public string OrganizedBy { get; set; } = string.Empty;

    [ForeignKey(nameof(OrganizedBy))]
    public ApplicationUser? Organizer { get; set; }

    [Required]
    public bool IsPublic { get; set; } = false;

    [Required]
    public bool RsvpEnabled { get; set; } = false;

    [Required]
    public int EventTypeId { get; set; }

    [ForeignKey(nameof(EventTypeId))]
    public EventType? EventType { get; set; }

    public int? ForumThreadId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsActive { get; set; } = true;

    // Maintain backward compatibility for polling system
    [Required]
    public string CreatedById { get; set; } = string.Empty;

    [ForeignKey(nameof(CreatedById))]
    public ApplicationUser? CreatedBy { get; set; }

    // Navigation properties
    public ICollection<Poll> Polls { get; set; } = new List<Poll>();
    public ICollection<EventMedia> EventMedia { get; set; } = new List<EventMedia>();
    public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    public ICollection<EventArtwork> EventArtworks { get; set; } = new List<EventArtwork>();
    public ICollection<EventAnnouncement> Announcements { get; set; } = new List<EventAnnouncement>();
}
