using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Configuration;
using SamMALsurium.Models.Enums;
using SamMALsurium.Models.ViewModels;
using SamMALsurium.Services;

namespace SamMALsurium.Controllers;

[Authorize]
public class ImagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IImageStorageService _storageService;
    private readonly ImageProcessingQueueService _queueService;
    private readonly ImageStorageSettings _settings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        ApplicationDbContext context,
        IImageStorageService storageService,
        ImageProcessingQueueService queueService,
        IOptions<ImageStorageSettings> settings,
        UserManager<ApplicationUser> userManager,
        ILogger<ImagesController> logger)
    {
        _context = context;
        _storageService = storageService;
        _queueService = queueService;
        _settings = settings.Value;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Upload(string? returnUrl, int? contextId)
    {
        var model = new ImageUploadViewModel
        {
            ReturnUrl = returnUrl,
            ContextId = contextId,
            Privacy = ImagePrivacyLevel.CommunityOnly
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(ImageUploadViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.ImageFile == null)
        {
            ModelState.AddModelError("ImageFile", "Please select an image file");
            return View(model);
        }

        // Validate file size
        if (model.ImageFile.Length > _settings.MaxFileSizeBytes)
        {
            var maxSizeMB = _settings.MaxFileSizeBytes / 1024.0 / 1024.0;
            ModelState.AddModelError("ImageFile", $"File size exceeds {maxSizeMB:F0}MB limit. Please compress your image and try again.");
            return View(model);
        }

        // Validate file type
        var fileExtension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
        if (!_settings.AllowedExtensions.Contains(fileExtension))
        {
            ModelState.AddModelError("ImageFile", $"File type {fileExtension} is not supported. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}");
            return View(model);
        }

        try
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Create Image entity with status Uploading
            var image = new Image
            {
                UserId = userId,
                OriginalFilename = model.ImageFile.FileName,
                UploadedAt = DateTime.UtcNow,
                Privacy = model.Privacy,
                ProcessingStatus = ImageProcessingStatus.Uploading,
                Tags = model.Tags
            };

            // Save to database first
            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            // Save original file
            var originalPath = await _storageService.SaveOriginalAsync(model.ImageFile, userId);

            // Update image entity with original path
            image.OriginalPath = originalPath;
            await _context.SaveChangesAsync();

            // Enqueue image for processing
            _queueService.EnqueueImage(image.ImageId);

            _logger.LogInformation("Image {ImageId} uploaded by user {UserId}", image.ImageId, userId);

            // Redirect to returnUrl or Library
            if (!string.IsNullOrEmpty(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction(nameof(Library));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            ModelState.AddModelError("", "An error occurred while uploading your image. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Library(string? returnUrl, int? contextId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        var images = await _context.Images
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.UploadedAt)
            .Select(i => new ImageItemViewModel
            {
                ImageId = i.ImageId,
                ThumbnailUrl = i.ThumbnailPath != null ? _storageService.GetImageUrl(i.ThumbnailPath) : string.Empty,
                HighResUrl = i.HighResPath != null ? _storageService.GetImageUrl(i.HighResPath) : string.Empty,
                Privacy = i.Privacy,
                ProcessingStatus = i.ProcessingStatus,
                UploadedAt = i.UploadedAt,
                Tags = i.Tags,
                ErrorMessage = i.ErrorMessage
            })
            .ToListAsync();

        var model = new ImageLibraryViewModel
        {
            Images = images,
            ReturnUrl = returnUrl,
            ContextId = contextId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectFromLibrary(int imageId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Validate user owns the image
        var image = await _context.Images
            .Where(i => i.ImageId == imageId && i.UserId == userId)
            .FirstOrDefaultAsync();

        if (image == null)
        {
            _logger.LogWarning("User {UserId} attempted to select image {ImageId} they don't own", userId, imageId);
            return NotFound();
        }

        // Redirect to returnUrl with imageId parameter
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var separator = returnUrl.Contains('?') ? "&" : "?";
            return Redirect($"{returnUrl}{separator}imageId={imageId}");
        }

        return RedirectToAction(nameof(Library));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrivacy(int imageId, ImagePrivacyLevel privacy)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Find the image and validate user owns it
        var image = await _context.Images
            .Where(i => i.ImageId == imageId && i.UserId == userId)
            .FirstOrDefaultAsync();

        if (image == null)
        {
            _logger.LogWarning("User {UserId} attempted to update privacy for image {ImageId} they don't own", userId, imageId);
            return NotFound();
        }

        // Update privacy level
        image.Privacy = privacy;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated privacy for image {ImageId} to {Privacy}", userId, imageId, privacy);

        // Return JSON for AJAX requests
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, privacy = privacy.ToString() });
        }

        // Redirect to library for normal form submission
        return RedirectToAction(nameof(Library));
    }
}
