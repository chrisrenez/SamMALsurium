using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models;

public class Image
{
    [Key]
    public int ImageId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(500)]
    public string OriginalFilename { get; set; } = string.Empty;

    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public ImagePrivacyLevel Privacy { get; set; } = ImagePrivacyLevel.CommunityOnly;

    [Required]
    public ImageProcessingStatus ProcessingStatus { get; set; } = ImageProcessingStatus.Uploading;

    // Original image path
    [MaxLength(1000)]
    public string? OriginalPath { get; set; }

    // High-res variant paths (2560px)
    [MaxLength(1000)]
    public string? HighResPath { get; set; }

    [MaxLength(1000)]
    public string? HighResWebPPath { get; set; }

    // Medium-res variant paths (1920px)
    [MaxLength(1000)]
    public string? MediumResPath { get; set; }

    [MaxLength(1000)]
    public string? MediumResWebPPath { get; set; }

    // Thumbnail variant paths (320px)
    [MaxLength(1000)]
    public string? ThumbnailPath { get; set; }

    [MaxLength(1000)]
    public string? ThumbnailWebPPath { get; set; }

    // Image dimensions (from original)
    public int? OriginalWidth { get; set; }

    public int? OriginalHeight { get; set; }

    // Tags (comma-separated string)
    [MaxLength(2000)]
    public string? Tags { get; set; }

    // Error message if processing fails
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}
