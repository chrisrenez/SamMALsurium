namespace SamMALsurium.Models.ViewModels.Admin;

public class PendingApprovalsViewModel
{
    public List<PendingApprovalViewModel> PendingUsers { get; set; } = new();
    public int Count => PendingUsers.Count;
}
