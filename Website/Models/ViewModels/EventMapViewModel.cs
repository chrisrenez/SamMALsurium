namespace SamMALsurium.Models.ViewModels;

public class EventMapViewModel
{
    public List<EventMapItemViewModel> Events { get; set; } = new();
    public string GoogleMapsApiKey { get; set; } = string.Empty;
}

public class EventMapItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string? Location { get; set; }
    public string? LocationName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
}
