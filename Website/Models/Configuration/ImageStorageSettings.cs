namespace SamMALsurium.Models.Configuration;

public class ImageStorageSettings
{
    public string UploadPath { get; set; } = "uploads/users";
    public long MaxFileSizeBytes { get; set; } = 26214400; // 25MB default
    public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".gif" };

    // Dimension limits for variants (maximum pixels on longest edge)
    public int HighResDimensionLimit { get; set; } = 2560;
    public int MediumResDimensionLimit { get; set; } = 1920;
    public int ThumbnailDimensionLimit { get; set; } = 320;

    // JPEG quality settings (0-100)
    public int HighResQuality { get; set; } = 85;
    public int MediumResQuality { get; set; } = 80;
    public int ThumbnailQuality { get; set; } = 75;
}
