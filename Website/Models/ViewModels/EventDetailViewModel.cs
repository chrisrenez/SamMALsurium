using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels;

public class EventDetailViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool RsvpEnabled { get; set; }
    public DateTime CreatedAt { get; set; }

    // Media
    public string? CoverImagePath { get; set; }
    public List<string> GalleryImagePaths { get; set; } = new();
    public List<string> ExternalLinks { get; set; } = new();
    public List<EventMedia> Attachments { get; set; } = new();

    // RSVP Info
    public RsvpStatus? CurrentUserRsvpStatus { get; set; }
    public int GoingCount { get; set; }
    public int MaybeCount { get; set; }
    public int NotGoingCount { get; set; }
    public List<EventAttendeeViewModel> Attendees { get; set; } = new();

    // Navigation
    public bool CanEdit { get; set; }
    public bool IsAuthenticated { get; set; }
}

public class EventAttendeeViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public RsvpStatus RsvpStatus { get; set; }
    public DateTime RsvpDate { get; set; }
}
