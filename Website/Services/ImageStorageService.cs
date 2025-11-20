using Microsoft.Extensions.Options;
using SamMALsurium.Models;
using SamMALsurium.Models.Configuration;
using System.Text.RegularExpressions;

namespace SamMALsurium.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ImageStorageSettings _settings;

    public ImageStorageService(IWebHostEnvironment environment, IOptions<ImageStorageSettings> settings)
    {
        _environment = environment;
        _settings = settings.Value;
    }

    public async Task<string> SaveOriginalAsync(IFormFile file, string userId)
    {
        var storagePath = GenerateStoragePath(userId);
        var fullPath = Path.Combine(_environment.WebRootPath, storagePath);

        // Create directory if it doesn't exist
        Directory.CreateDirectory(fullPath);

        // Sanitize filename
        var sanitizedFileName = SanitizeFileName(file.FileName);
        var uniqueFileName = GenerateUniqueFileName(sanitizedFileName);
        var filePath = Path.Combine(fullPath, uniqueFileName);

        // Save the file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative path
        return Path.Combine(storagePath, uniqueFileName).Replace("\\", "/");
    }

    public Task<bool> DeleteImageFilesAsync(Image image)
    {
        try
        {
            var filesToDelete = new[]
            {
                image.OriginalPath,
                image.HighResPath,
                image.HighResWebPPath,
                image.MediumResPath,
                image.MediumResWebPPath,
                image.ThumbnailPath,
                image.ThumbnailWebPPath
            };

            foreach (var relativePath in filesToDelete)
            {
                if (string.IsNullOrEmpty(relativePath))
                    continue;

                var fullPath = Path.Combine(_environment.WebRootPath, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public string GenerateStoragePath(string userId)
    {
        var now = DateTime.UtcNow;
        return Path.Combine(
            _settings.UploadPath,
            userId,
            now.Year.ToString(),
            now.Month.ToString("D2")
        );
    }

    public string GetImageUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return string.Empty;

        return "/" + relativePath.Replace("\\", "/");
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove any path information
        fileName = Path.GetFileName(fileName);

        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Remove potentially dangerous characters
        sanitized = Regex.Replace(sanitized, @"[^\w\s\-\.]", "_");

        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension.Substring(0, 200 - extension.Length) + extension;
        }

        return sanitized;
    }

    private string GenerateUniqueFileName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);

        return $"{nameWithoutExtension}_{timestamp}_{guid}{extension}";
    }
}
