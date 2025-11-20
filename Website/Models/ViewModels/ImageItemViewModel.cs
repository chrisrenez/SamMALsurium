using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels;

public class ImageItemViewModel
{
    public int ImageId { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string HighResUrl { get; set; } = string.Empty;
    public ImagePrivacyLevel Privacy { get; set; }
    public ImageProcessingStatus ProcessingStatus { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Tags { get; set; }
    public string? ErrorMessage { get; set; }
}
