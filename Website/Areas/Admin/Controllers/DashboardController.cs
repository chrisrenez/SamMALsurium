using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamMALsurium.Data;
using SamMALsurium.Models.ViewModels.Admin;

namespace SamMALsurium.Areas.Admin.Controllers;

public class DashboardController : BaseAdminController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;
    private readonly IWebHostEnvironment _environment;

    public DashboardController(
        ApplicationDbContext context,
        ILogger<DashboardController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    // GET: Admin/Dashboard
    public async Task<IActionResult> Index()
    {
        var viewModel = new DashboardViewModel();

        // Calculate User Metrics
        viewModel.Users.TotalMembers = await _context.Users.CountAsync();
        viewModel.Users.PendingApprovals = await _context.Users.CountAsync(u => !u.IsApproved);

        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        // Note: This is approximate since we don't track registration date yet
        viewModel.Users.NewThisMonth = 0; // TODO: Implement when registration date tracking is added

        // Note: Active users calculation would require last login tracking
        viewModel.Users.ActiveUsers = 0; // TODO: Implement when last login tracking is added

        // Calculate Content Metrics
        // Note: These features don't exist yet, so set to 0
        viewModel.Content.TotalArtworks = 0; // TODO: Implement when artwork feature exists
        viewModel.Content.TotalEvents = 0; // TODO: Implement when events feature exists
        viewModel.Content.TotalSurveys = 0; // TODO: Implement when surveys feature exists

        // Get recent activity from audit log
        var recentAuditLogs = await _context.AdminAuditLogs
            .Include(a => a.AdminUser)
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToListAsync();

        viewModel.Content.RecentActivity = recentAuditLogs.Select(log => new RecentActivityItem
        {
            Action = log.Action,
            Details = log.Details ?? string.Empty,
            Timestamp = log.Timestamp,
            AdminName = log.AdminUser != null
                ? $"{log.AdminUser.FirstName} {log.AdminUser.LastName}"
                : "Unknown"
        }).ToList();

        // Calculate System Health Metrics
        viewModel.SystemHealth.DatabaseStatus = await CheckDatabaseStatusAsync();
        var storageInfo = GetStorageInfo();
        viewModel.SystemHealth.StorageUsedBytes = storageInfo.UsedBytes;
        viewModel.SystemHealth.StorageAvailableBytes = storageInfo.AvailableBytes;
        viewModel.SystemHealth.RecentErrors = GetRecentErrorCount();

        return View(viewModel);
    }

    /// <summary>
    /// Checks database connection status
    /// </summary>
    private async Task<string> CheckDatabaseStatusAsync()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return "Connected";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection check failed");
            return "Disconnected";
        }
    }

    /// <summary>
    /// Gets storage information for the uploads directory
    /// </summary>
    private (long UsedBytes, long AvailableBytes) GetStorageInfo()
    {
        try
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Calculate used storage
            var directory = new DirectoryInfo(uploadsPath);
            long usedBytes = 0;
            if (directory.Exists)
            {
                usedBytes = directory.GetFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            }

            // Get available storage on the drive
            var driveInfo = new DriveInfo(Path.GetPathRoot(uploadsPath) ?? "/");
            long availableBytes = driveInfo.AvailableFreeSpace;

            return (usedBytes, availableBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage information");
            return (0, 0);
        }
    }

    /// <summary>
    /// Gets count of recent errors from logs (last 24 hours)
    /// </summary>
    private int GetRecentErrorCount()
    {
        // This is a simplified implementation
        // In a real application, you would query from a logging system or database
        // For now, return 0 as we don't have error logging in the database
        return 0;
    }
}
