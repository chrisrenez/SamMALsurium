using System.ComponentModel.DataAnnotations;

namespace SamMALsurium.Models.ViewModels;

public class UserProfileViewModel
{
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed {1} characters")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed {1} characters")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;
}
