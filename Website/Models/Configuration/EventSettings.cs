namespace SamMALsurium.Models.Configuration;

public class EventSettings
{
    public string GoogleMapsApiKey { get; set; } = string.Empty;
    public int ReminderHoursBeforeEvent { get; set; } = 24;
    public int MaxFileUploadSizeMB { get; set; } = 10;
    public int EventsPerPage { get; set; } = 20;
    public bool EnableGeolocation { get; set; } = true;
    public bool EnableRsvpByDefault { get; set; } = false;
    public int MaxImagesPerEvent { get; set; } = 10;
    public int MaxAttachmentsPerEvent { get; set; } = 5;
}
