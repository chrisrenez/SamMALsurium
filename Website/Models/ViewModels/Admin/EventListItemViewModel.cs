namespace SamMALsurium.Models.ViewModels.Admin;

public class EventListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? EventTypeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public bool IsPublic { get; set; }
    public bool RsvpEnabled { get; set; }
    public bool IsActive { get; set; }
    public int AttendeeCount { get; set; }
    public string? OrganizerName { get; set; }
    public DateTime CreatedAt { get; set; }
}
