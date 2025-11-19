using Microsoft.AspNetCore.Identity;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Approval and account status fields
    public bool IsApproved { get; set; } = false;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
    public string? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
}
