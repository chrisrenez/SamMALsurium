using SamMALsurium.Models;

namespace SamMALsurium.Services;

public interface IImageStorageService
{
    Task<string> SaveOriginalAsync(IFormFile file, string userId);
    Task<bool> DeleteImageFilesAsync(Image image);
    string GenerateStoragePath(string userId);
    string GetImageUrl(string relativePath);
}
