using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SamMALsurium.Models;
using SamMALsurium.Models.Configuration;

namespace SamMALsurium.Middleware;

/// <summary>
/// Middleware that enforces maintenance mode by redirecting non-admin users to a maintenance page.
/// When maintenance mode is enabled, all non-admin traffic is redirected to /Home/Maintenance,
/// while admin users retain full access to the site.
/// </summary>
public class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    public MaintenanceModeMiddleware(
        RequestDelegate next,
        ILogger<MaintenanceModeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<ApplicationSettings> applicationSettings,
        SignInManager<ApplicationUser> signInManager)
    {
        var isMaintenanceMode = applicationSettings.Value.IsMaintenanceMode;

        // If maintenance mode is disabled, continue with the request
        if (!isMaintenanceMode)
        {
            await _next(context);
            return;
        }

        // Check if the user is an admin
        var isAdmin = context.User.Identity?.IsAuthenticated == true &&
                      context.User.IsInRole("Admin");

        // If the user is an admin, allow full access
        if (isAdmin)
        {
            await _next(context);
            return;
        }

        // At this point, maintenance mode is enabled and the user is not an admin
        var currentPath = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var maintenancePath = "/home/maintenance";

        // Prevent redirect loop - allow access to the maintenance page itself
        if (currentPath == maintenancePath)
        {
            await _next(context);
            return;
        }

        // Allow access to login page and login action during maintenance
        if (currentPath.StartsWith("/account/login"))
        {
            await _next(context);
            return;
        }

        // Allow static files to pass through (CSS, JS, images, etc.)
        if (currentPath.StartsWith("/css") ||
            currentPath.StartsWith("/js") ||
            currentPath.StartsWith("/img") ||
            currentPath.StartsWith("/lib") ||
            currentPath.StartsWith("/fonts") ||
            Path.HasExtension(currentPath))
        {
            await _next(context);
            return;
        }

        // Log out authenticated non-admin users
        if (context.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation(
                "Maintenance mode: Signing out non-admin user {UserName}",
                context.User.Identity.Name);

            await signInManager.SignOutAsync();
        }

        // Redirect to maintenance page
        _logger.LogInformation(
            "Maintenance mode: Redirecting request from {Path} to {MaintenancePath}",
            currentPath,
            maintenancePath);

        context.Response.Redirect(maintenancePath);
    }
}
