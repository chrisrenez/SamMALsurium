namespace SamMALsurium.Models.Configuration;

public class ApplicationSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsMaintenanceMode { get; set; } = false;
    public string MaintenanceMessage { get; set; } = "Nur noch wenige Tage ...";
}
