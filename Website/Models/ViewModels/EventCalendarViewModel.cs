namespace SamMALsurium.Models.ViewModels;

public class EventCalendarViewModel
{
    public List<EventCalendarItemViewModel> Events { get; set; } = new();
    public DateTime CurrentMonth { get; set; }
}

public class EventCalendarItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool RsvpEnabled { get; set; }
}
