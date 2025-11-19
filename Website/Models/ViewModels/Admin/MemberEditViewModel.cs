using System.ComponentModel.DataAnnotations;

namespace SamMALsurium.Models.ViewModels.Admin
{
    public class MemberEditViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vorname ist erforderlich.")]
        [StringLength(100, ErrorMessage = "Vorname darf maximal 100 Zeichen lang sein.")]
        [Display(Name = "Vorname")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nachname ist erforderlich.")]
        [StringLength(100, ErrorMessage = "Nachname darf maximal 100 Zeichen lang sein.")]
        [Display(Name = "Nachname")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-Mail ist erforderlich.")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rolle ist erforderlich.")]
        [Display(Name = "Rolle")]
        public string Role { get; set; } = "Member";
    }
}
