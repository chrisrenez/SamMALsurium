using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Middleware;

/// <summary>
/// Middleware that enforces privacy controls on image access.
/// Ensures that Community Only images are only accessible to authenticated users who own them,
/// while Public Profile images are accessible to everyone.
/// </summary>
public class ImagePrivacyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ImagePrivacyMiddleware> _logger;

    public ImagePrivacyMiddleware(RequestDelegate next, ILogger<ImagePrivacyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        var path = context.Request.Path.Value;

        // Only check image paths
        if (path != null && path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the relative path to query the database
            var relativePath = path.TrimStart('/');

            // Query the Image entity by path (check all variant paths)
            var image = await dbContext.Images
                .FirstOrDefaultAsync(i =>
                    i.OriginalPath == relativePath ||
                    i.HighResPath == relativePath ||
                    i.HighResWebPPath == relativePath ||
                    i.MediumResPath == relativePath ||
                    i.MediumResWebPPath == relativePath ||
                    i.ThumbnailPath == relativePath ||
                    i.ThumbnailWebPPath == relativePath);

            if (image != null)
            {
                // If image is PublicProfile, allow access
                if (image.Privacy == ImagePrivacyLevel.PublicProfile)
                {
                    _logger.LogDebug("Allowing access to public image: {Path}", path);
                    await _next(context);
                    return;
                }

                // For CommunityOnly images, user must be authenticated
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("Unauthenticated user attempted to access private image: {Path}", path);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access denied. This image is private.");
                    return;
                }

                // Get current user ID
                var currentUser = await userManager.GetUserAsync(context.User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Could not resolve user for authenticated request to: {Path}", path);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access denied.");
                    return;
                }

                // Check if user owns the image
                if (image.UserId == currentUser.Id)
                {
                    _logger.LogDebug("Allowing owner access to private image: {Path}", path);
                    await _next(context);
                    return;
                }

                // User is authenticated but doesn't own the image and it's CommunityOnly
                _logger.LogWarning("User {UserId} attempted to access image owned by {OwnerId}: {Path}",
                    currentUser.Id, image.UserId, path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied. You do not have permission to view this image.");
                return;
            }
            else
            {
                // Image not found in database - log it but allow the request to continue
                // (this will result in a 404 from the static file middleware)
                _logger.LogDebug("Image path not found in database: {Path}", path);
            }
        }

        // Continue to next middleware
        await _next(context);
    }
}
