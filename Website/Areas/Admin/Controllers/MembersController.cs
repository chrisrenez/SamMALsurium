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

    // GET: Admin/Members
    public async Task<IActionResult> Index(string? searchQuery, string? statusFilter, string? roleFilter)
    {
        // Start with all users
        var query = _context.Users.AsQueryable();

        // Apply search filter (name or email)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(u =>
                u.FirstName.Contains(searchQuery) ||
                u.LastName.Contains(searchQuery) ||
                u.Email!.Contains(searchQuery)
            );
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<AccountStatus>(statusFilter, out var status))
        {
            query = query.Where(u => u.AccountStatus == status);
        }

        // Get all users from database first
        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();

        // Project to view model with role information
        var memberItems = new List<MemberListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Member";
            var isAdmin = roles.Contains("Admin");

            // Apply role filter if specified
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                if (roleFilter == "Admin" && !isAdmin) continue;
                if (roleFilter == "Member" && isAdmin) continue;
            }

            memberItems.Add(new MemberListItemViewModel
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email!,
                RegistrationDate = DateTime.UtcNow, // Note: EF Identity doesn't track registration date by default
                AccountStatus = user.AccountStatus,
                Role = role,
                IsAdmin = isAdmin
            });
        }

        var viewModel = new MemberListViewModel
        {
            Members = memberItems,
            SearchQuery = searchQuery,
            StatusFilter = statusFilter,
            RoleFilter = roleFilter
        };

        return View(viewModel);
    }

    // GET: Admin/Members/Create
    public IActionResult Create()
    {
        var viewModel = new CreateMemberViewModel
        {
            Role = "Member" // Default role
        };
        return View(viewModel);
    }

    // POST: Admin/Members/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMemberViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.");
            return View(model);
        }

        // Create new user
        var user = new ApplicationUser
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            UserName = model.Email,
            IsApproved = true, // Manually added users are auto-approved
            AccountStatus = AccountStatus.Active
        };

        // Generate a temporary password (will be reset via email)
        var tempPassword = Guid.NewGuid().ToString();
        var createResult = await _userManager.CreateAsync(user, tempPassword);

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Add user to role
        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Fehler beim Hinzufügen der Rolle.");
            return View(model);
        }

        // Generate password reset token
        var passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Send "Set Password" email
        try
        {
            await _emailService.SendSetPasswordEmailAsync(user.Email, user.FirstName, passwordResetToken, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password setup email to {Email}", user.Email);
            TempData["ErrorMessage"] = "Benutzer wurde erstellt, aber die E-Mail konnte nicht gesendet werden.";
        }

        // Log admin action
        await LogAdminActionAsync(
            "MemberCreated",
            user.Id,
            $"Mitglied {user.FirstName} {user.LastName} ({user.Email}) wurde manuell erstellt mit Rolle {model.Role}."
        );

        TempData["SuccessMessage"] = $"Mitglied {user.FirstName} {user.LastName} wurde erfolgreich erstellt. Eine E-Mail zum Setzen des Passworts wurde gesendet.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Admin/Members/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Member";

        var viewModel = new MemberEditViewModel
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            Role = role
        };

        return View(viewModel);
    }

    // POST: Admin/Members/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MemberEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Index));
        }

        // Get current role
        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault() ?? "Member";

        // Update user properties
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;
        user.NormalizedEmail = model.Email.ToUpper();
        user.UserName = model.Email;
        user.NormalizedUserName = model.Email.ToUpper();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Fehler beim Aktualisieren des Benutzers.");
            return View(model);
        }

        // Update role if changed
        if (currentRole != model.Role)
        {
            // Remove from all roles
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Fehler beim Aktualisieren der Rolle.");
                    return View(model);
                }
            }

            // Add to new role
            var addResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Fehler beim Hinzufügen der neuen Rolle.");
                return View(model);
            }

            await LogAdminActionAsync(
                "MemberRoleChanged",
                user.Id,
                $"Rolle von {currentRole} zu {model.Role} geändert für {user.FirstName} {user.LastName} ({user.Email})"
            );
        }

        // Log the edit action
        await LogAdminActionAsync(
            "MemberEdited",
            user.Id,
            $"Mitglied {user.FirstName} {user.LastName} ({user.Email}) wurde bearbeitet."
        );

        TempData["SuccessMessage"] = $"Mitglied {user.FirstName} {user.LastName} wurde erfolgreich aktualisiert.";
        return RedirectToAction(nameof(Index));
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

    // POST: Admin/Members/Suspend
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Index));
        }

        user.AccountStatus = AccountStatus.Suspended;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Fehler beim Sperren des Benutzers.";
            return RedirectToAction(nameof(Index));
        }

        await LogAdminActionAsync(
            "MemberSuspended",
            userId,
            $"Mitglied {user.FirstName} {user.LastName} ({user.Email}) wurde gesperrt."
        );

        TempData["SuccessMessage"] = $"Mitglied {user.FirstName} {user.LastName} wurde gesperrt.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Members/Reactivate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Index));
        }

        user.AccountStatus = AccountStatus.Active;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Fehler beim Reaktivieren des Benutzers.";
            return RedirectToAction(nameof(Index));
        }

        // Send reactivation email
        try
        {
            await _emailService.SendAccountReactivatedAsync(user.Email!, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reactivation email to {Email}", user.Email);
            // Continue even if email fails
        }

        await LogAdminActionAsync(
            "MemberReactivated",
            userId,
            $"Mitglied {user.FirstName} {user.LastName} ({user.Email}) wurde reaktiviert."
        );

        TempData["SuccessMessage"] = $"Mitglied {user.FirstName} {user.LastName} wurde reaktiviert.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/Members/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
            return RedirectToAction(nameof(Index));
        }

        var userEmail = user.Email!;
        var userFirstName = user.FirstName;
        var userLastName = user.LastName;

        // TODO: When artwork feature is implemented, add artwork deletion logic here:
        // 1. Find all artworks by this user
        // 2. Delete artwork files from storage
        // 3. Delete artwork records from database

        // Log admin action before deleting user
        await LogAdminActionAsync(
            "MemberDeleted",
            userId,
            $"Mitglied {userFirstName} {userLastName} ({userEmail}) wurde permanent gelöscht."
        );

        // Delete user permanently
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Fehler beim Löschen des Benutzers.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"Mitglied {userFirstName} {userLastName} wurde permanent gelöscht.";
        return RedirectToAction(nameof(Index));
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
