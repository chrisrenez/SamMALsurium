namespace SamMALsurium.Models;

public class AdminAuditLog
{
    public int Id { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string? TargetUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ApplicationUser? AdminUser { get; set; }
    public ApplicationUser? TargetUser { get; set; }
}
