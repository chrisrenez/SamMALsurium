namespace SamMALsurium.Models.ViewModels.Admin
{
    public class MemberListViewModel
    {
        public List<MemberListItemViewModel> Members { get; set; } = new();
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public string? RoleFilter { get; set; }

        public int TotalCount => Members.Count;
    }
}
