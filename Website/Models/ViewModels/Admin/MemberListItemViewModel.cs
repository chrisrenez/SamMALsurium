using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels.Admin
{
    public class MemberListItemViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public AccountStatus AccountStatus { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
