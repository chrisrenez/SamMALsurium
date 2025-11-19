using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;
using SamMALsurium.Models.ViewModels.Admin;
using SamMALsurium.Services;

namespace SamMALsurium.Areas.Admin.Controllers;

public class MembersController : BaseAdminController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<MembersController> _logger;

    public MembersController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<MembersController> logger)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    // GET: Admin/Members/PendingApprovals
    public async Task<IActionResult> PendingApprovals()
    {
        var pendingUsers = await _context.Users
            .Where(u => !u.IsApproved)
            .OrderBy(u => u.Id)
            .Select(u => new PendingApprovalViewModel
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email!,
                RegistrationDate = DateTime.UtcNow // Note: EF Identity doesn't track registration date by default
            })
            .ToListAsync();

        var viewModel = new PendingApprovalsViewModel
        {
            PendingUsers = pendingUsers
        };

        return View(viewModel);
    }

    // POST: Admin/Members/Approve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        // Update user approval status
        user.IsApproved = true;
        user.AccountStatus = AccountStatus.Active;
        user.ApprovedById = _userManager.GetUserId(User);
        user.ApprovedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Fehler beim Genehmigen des Benutzers.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        // Log admin action
        await LogAdminActionAsync(
            "UserApproved",
            userId,
            $"Benutzer {user.FirstName} {user.LastName} ({user.Email}) wurde genehmigt."
        );

        TempData["SuccessMessage"] = $"Benutzer {user.FirstName} {user.LastName} wurde erfolgreich genehmigt.";
        return RedirectToAction(nameof(PendingApprovals));
    }

    // POST: Admin/Members/Reject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string userId, string? rejectionMessage)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        var userEmail = user.Email!;
        var userFirstName = user.FirstName;
        var userLastName = user.LastName;

        // Send rejection email if message is provided
        if (!string.IsNullOrWhiteSpace(rejectionMessage))
        {
            try
            {
                await _emailService.SendUserRejectionAsync(userEmail, userFirstName, rejectionMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection email to {Email}", userEmail);
                // Continue with deletion even if email fails
            }
        }

        // Log admin action before deleting user
        await LogAdminActionAsync(
            "UserRejected",
            userId,
            $"Benutzer {userFirstName} {userLastName} ({userEmail}) wurde abgelehnt. Nachricht: {rejectionMessage ?? "Keine Nachricht"}"
        );

        // Delete user permanently
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Fehler beim Ablehnen des Benutzers.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        TempData["SuccessMessage"] = $"Benutzer {userFirstName} {userLastName} wurde abgelehnt und gelöscht.";
        return RedirectToAction(nameof(PendingApprovals));
    }

    /// <summary>
    /// Logs an admin action to the audit log
    /// </summary>
    private async Task LogAdminActionAsync(string action, string? targetUserId, string? details)
    {
        var adminUserId = _userManager.GetUserId(User);
        if (adminUserId == null)
        {
            _logger.LogWarning("Could not determine admin user ID for audit log");
            return;
        }

        var auditLog = new AdminAuditLog
        {
            AdminUserId = adminUserId,
            TargetUserId = targetUserId,
            Action = action,
            Details = details,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        };

        _context.AdminAuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Admin action logged: {Action} by {AdminUserId} on {TargetUserId}",
                action, adminUserId, targetUserId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log admin action: {Action}", action);
            // Don't throw - audit logging shouldn't break the main flow
        }
    }
}
