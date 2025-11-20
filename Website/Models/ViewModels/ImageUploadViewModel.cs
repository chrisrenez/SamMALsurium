using System.ComponentModel.DataAnnotations;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels;

public class ImageUploadViewModel
{
    [Required(ErrorMessage = "Please select an image file")]
    [Display(Name = "Image File")]
    public IFormFile? ImageFile { get; set; }

    [Required]
    [Display(Name = "Privacy Level")]
    public ImagePrivacyLevel Privacy { get; set; } = ImagePrivacyLevel.CommunityOnly;

    [Display(Name = "Tags (comma-separated)")]
    [MaxLength(2000, ErrorMessage = "Tags cannot exceed 2000 characters")]
    public string? Tags { get; set; }

    public string? ReturnUrl { get; set; }

    public int? ContextId { get; set; }
}
