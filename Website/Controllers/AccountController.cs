using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SamMALsurium.Models;
using SamMALsurium.Models.ViewModels;

namespace SamMALsurium.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Assign Member role to new users
            await _userManager.AddToRoleAsync(user, "Member");

            // Log registration success with IP and timestamp
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            _logger.LogInformation(
                "User registered successfully. Email: {Email}, UserId: {UserId}, IP: {IpAddress}, Timestamp: {Timestamp}",
                user.Email, user.Id, ipAddress, DateTime.UtcNow);

            return RedirectToAction(nameof(Login));
        }

        // Log registration failure
        var failureIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        _logger.LogWarning(
            "Registration failed for email: {Email}, IP: {IpAddress}, Timestamp: {Timestamp}, Errors: {Errors}",
            model.Email, failureIpAddress, DateTime.UtcNow, string.Join(", ", result.Errors.Select(e => e.Description)));

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger.LogInformation(
                "User logged in successfully. Email: {Email}, UserId: {UserId}, IP: {IpAddress}, Timestamp: {Timestamp}, RememberMe: {RememberMe}",
                user?.Email, user?.Id, ipAddress, DateTime.UtcNow, model.RememberMe);

            // Prevent open redirect attacks
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // Log login failure
        var failureIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        _logger.LogWarning(
            "Login failed for email: {Email}, IP: {IpAddress}, Timestamp: {Timestamp}",
            model.Email, failureIpAddress, DateTime.UtcNow);

        ModelState.AddModelError(string.Empty, "Invalid login attempt");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userEmail = User.Identity?.Name;
        var userId = _userManager.GetUserId(User);

        await _signInManager.SignOutAsync();

        // Log logout event
        _logger.LogInformation(
            "User logged out. Email: {Email}, UserId: {UserId}, Timestamp: {Timestamp}",
            userEmail, userId, DateTime.UtcNow);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var model = new UserProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Track changes for logging
        var oldFirstName = user.FirstName;
        var oldLastName = user.LastName;

        // Update only editable fields (FirstName and LastName)
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger.LogInformation(
                "User profile updated. Email: {Email}, UserId: {UserId}, IP: {IpAddress}, Timestamp: {Timestamp}, Changes: FirstName: {OldFirstName} -> {NewFirstName}, LastName: {OldLastName} -> {NewLastName}",
                user.Email, user.Id, ipAddress, DateTime.UtcNow, oldFirstName, user.FirstName, oldLastName, user.LastName);

            TempData["SuccessMessage"] = "Profile updated successfully";
            return RedirectToAction(nameof(Profile));
        }

        // Log profile update failure
        var failureIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        _logger.LogWarning(
            "Profile update failed for email: {Email}, IP: {IpAddress}, Timestamp: {Timestamp}, Errors: {Errors}",
            user.Email, failureIpAddress, DateTime.UtcNow, string.Join(", ", result.Errors.Select(e => e.Description)));

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }
}
