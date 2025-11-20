namespace SamMALsurium.Services;

public class ImageVariants
{
    public string HighResPath { get; set; } = string.Empty;
    public string HighResWebPPath { get; set; } = string.Empty;
    public string MediumResPath { get; set; } = string.Empty;
    public string MediumResWebPPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string ThumbnailWebPPath { get; set; } = string.Empty;
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }
}

public interface IImageProcessingService
{
    Task<ImageVariants> GenerateVariantsAsync(string originalPath, string userId);
}
