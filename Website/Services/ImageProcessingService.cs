using Microsoft.Extensions.Options;
using SamMALsurium.Models.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace SamMALsurium.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ImageStorageSettings _settings;

    public ImageProcessingService(IWebHostEnvironment environment, IOptions<ImageStorageSettings> settings)
    {
        _environment = environment;
        _settings = settings.Value;
    }

    public async Task<ImageVariants> GenerateVariantsAsync(string originalPath, string userId)
    {
        var fullOriginalPath = Path.Combine(_environment.WebRootPath, originalPath);

        if (!File.Exists(fullOriginalPath))
        {
            throw new FileNotFoundException("Original image file not found", fullOriginalPath);
        }

        var variants = new ImageVariants();

        // Load the original image
        using (var image = await Image.LoadAsync(fullOriginalPath))
        {
            // Store original dimensions
            variants.OriginalWidth = image.Width;
            variants.OriginalHeight = image.Height;

            // Get the directory where the original is stored
            var originalDirectory = Path.GetDirectoryName(originalPath) ?? string.Empty;
            var variantsDirectory = Path.Combine(originalDirectory, "variants");
            var fullVariantsPath = Path.Combine(_environment.WebRootPath, variantsDirectory);

            // Create variants directory if it doesn't exist
            Directory.CreateDirectory(fullVariantsPath);

            // Get base filename without extension
            var originalFileName = Path.GetFileNameWithoutExtension(originalPath);

            // Generate high-res variant (2560px)
            variants.HighResPath = await GenerateVariantAsync(
                image,
                fullVariantsPath,
                variantsDirectory,
                originalFileName,
                "highres",
                _settings.HighResDimensionLimit,
                _settings.HighResQuality,
                isWebP: false
            );

            variants.HighResWebPPath = await GenerateVariantAsync(
                image,
                fullVariantsPath,
                variantsDirectory,
                originalFileName,
                "highres",
                _settings.HighResDimensionLimit,
                _settings.HighResQuality,
                isWebP: true
            );

            // Generate medium-res variant (1920px)
            variants.MediumResPath = await GenerateVariantAsync(
                image,
                fullVariantsPath,
                variantsDirectory,
                originalFileName,
                "medium",
                _settings.MediumResDimensionLimit,
                _settings.MediumResQuality,
                isWebP: false
            );

            variants.MediumResWebPPath = await GenerateVariantAsync(
                image,
                fullVariantsPath,
                variantsDirectory,
                originalFileName,
                "medium",
                _settings.MediumResDimensionLimit,
                _settings.MediumResQuality,
                isWebP: true
            );

            // Generate thumbnail variant (320px)
            variants.ThumbnailPath = await GenerateVariantAsync(
                image,
                fullVariantsPath,
                variantsDirectory,
                originalFileName,
                "thumb",
                _settings.ThumbnailDimensionLimit,
                _settings.ThumbnailQuality,
                isWebP: false
            );

            variants.ThumbnailWebPPath = await GenerateVariantAsync(
                image,
                fullVariantsPath,
                variantsDirectory,
                originalFileName,
                "thumb",
                _settings.ThumbnailDimensionLimit,
                _settings.ThumbnailQuality,
                isWebP: true
            );
        }

        return variants;
    }

    private async Task<string> GenerateVariantAsync(
        Image image,
        string fullOutputDirectory,
        string relativeOutputDirectory,
        string baseFileName,
        string variantName,
        int maxDimension,
        int quality,
        bool isWebP)
    {
        var extension = isWebP ? ".webp" : ".jpg";
        var fileName = $"{baseFileName}-{variantName}{extension}";
        var fullPath = Path.Combine(fullOutputDirectory, fileName);
        var relativePath = Path.Combine(relativeOutputDirectory, fileName).Replace("\\", "/");

        // Calculate new dimensions while maintaining aspect ratio
        var (newWidth, newHeight) = CalculateNewDimensions(
            image.Width,
            image.Height,
            maxDimension
        );

        // Clone the image and resize
        using (var resizedImage = image.Clone(ctx => ctx.Resize(newWidth, newHeight)))
        {
            if (isWebP)
            {
                var webpEncoder = new WebpEncoder
                {
                    Quality = quality
                };
                await resizedImage.SaveAsync(fullPath, webpEncoder);
            }
            else
            {
                var jpegEncoder = new JpegEncoder
                {
                    Quality = quality
                };
                await resizedImage.SaveAsync(fullPath, jpegEncoder);
            }
        }

        return relativePath;
    }

    private (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight, int maxDimension)
    {
        // If image is already smaller than max dimension, keep original size
        if (originalWidth <= maxDimension && originalHeight <= maxDimension)
        {
            return (originalWidth, originalHeight);
        }

        // Calculate aspect ratio
        var aspectRatio = (double)originalWidth / originalHeight;

        int newWidth, newHeight;

        if (originalWidth > originalHeight)
        {
            // Landscape or square: limit width
            newWidth = maxDimension;
            newHeight = (int)(maxDimension / aspectRatio);
        }
        else
        {
            // Portrait: limit height
            newHeight = maxDimension;
            newWidth = (int)(maxDimension * aspectRatio);
        }

        return (newWidth, newHeight);
    }
}
