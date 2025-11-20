namespace SamMALsurium.Models.ViewModels;

public class EventListViewModel
{
    public List<EventCardViewModel> Events { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public int? FilterEventTypeId { get; set; }
    public DateTime? FilterStartDate { get; set; }
    public DateTime? FilterEndDate { get; set; }
    public string? FilterLocation { get; set; }
    public List<EventType> EventTypes { get; set; } = new();
}

public class EventCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public bool RsvpEnabled { get; set; }
    public int AttendeeCount { get; set; }
}
