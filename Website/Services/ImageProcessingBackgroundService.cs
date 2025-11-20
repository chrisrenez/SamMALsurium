using Microsoft.EntityFrameworkCore;
using SamMALsurium.Data;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Services;

public class ImageProcessingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ImageProcessingQueueService _queueService;
    private readonly ILogger<ImageProcessingBackgroundService> _logger;

    public ImageProcessingBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ImageProcessingQueueService queueService,
        ILogger<ImageProcessingBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _queueService = queueService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Image Processing Background Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for an image ID to be queued
                var imageId = await _queueService.DequeueImageAsync(stoppingToken);

                _logger.LogInformation("Processing image with ID: {ImageId}", imageId);

                // Create a scope to resolve scoped services
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var processingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();

                    // Load the image entity
                    var image = await dbContext.Images.FindAsync(imageId);

                    if (image == null)
                    {
                        _logger.LogWarning("Image with ID {ImageId} not found in database", imageId);
                        continue;
                    }

                    if (string.IsNullOrEmpty(image.OriginalPath))
                    {
                        _logger.LogWarning("Image with ID {ImageId} has no original path", imageId);
                        image.ProcessingStatus = ImageProcessingStatus.Error;
                        image.ErrorMessage = "Original image path is missing";
                        await dbContext.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    try
                    {
                        // Update status to Processing
                        image.ProcessingStatus = ImageProcessingStatus.Processing;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        // Generate variants
                        var variants = await processingService.GenerateVariantsAsync(
                            image.OriginalPath,
                            image.UserId
                        );

                        // Update image entity with variant paths
                        image.HighResPath = variants.HighResPath;
                        image.HighResWebPPath = variants.HighResWebPPath;
                        image.MediumResPath = variants.MediumResPath;
                        image.MediumResWebPPath = variants.MediumResWebPPath;
                        image.ThumbnailPath = variants.ThumbnailPath;
                        image.ThumbnailWebPPath = variants.ThumbnailWebPPath;
                        image.OriginalWidth = variants.OriginalWidth;
                        image.OriginalHeight = variants.OriginalHeight;
                        image.ProcessingStatus = ImageProcessingStatus.Ready;
                        image.ErrorMessage = null;

                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Successfully processed image with ID: {ImageId}", imageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing image with ID: {ImageId}", imageId);

                        // Update image status to Error
                        image.ProcessingStatus = ImageProcessingStatus.Error;
                        image.ErrorMessage = $"Processing failed: {ex.Message}";

                        try
                        {
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception saveEx)
                        {
                            _logger.LogError(saveEx, "Error saving error status for image with ID: {ImageId}", imageId);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the service
                _logger.LogInformation("Image Processing Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Image Processing Background Service");
                // Continue processing other images
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Image Processing Background Service has stopped");
    }
}
