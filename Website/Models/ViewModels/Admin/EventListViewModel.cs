namespace SamMALsurium.Models.ViewModels.Admin;

public class EventListViewModel
{
    public List<EventListItemViewModel> Events { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public int? FilterEventTypeId { get; set; }
    public DateTime? FilterStartDate { get; set; }
    public DateTime? FilterEndDate { get; set; }
    public bool? FilterIsActive { get; set; }
    public List<EventType> EventTypes { get; set; } = new();
}
