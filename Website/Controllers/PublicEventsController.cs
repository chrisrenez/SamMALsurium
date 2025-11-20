using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SamMALsurium.Models;
using SamMALsurium.Models.ViewModels;
using SamMALsurium.Services;

namespace SamMALsurium.Controllers;

public class PublicEventsController : Controller
{
    private readonly IEventService _eventService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PublicEventsController> _logger;

    public PublicEventsController(
        IEventService eventService,
        UserManager<ApplicationUser> userManager,
        ILogger<PublicEventsController> logger)
    {
        _eventService = eventService;
        _userManager = userManager;
        _logger = logger;
    }

    // GET: /PublicEvents/Index
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        int? eventTypeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1)
    {
        var viewModel = await _eventService.GetPublicEventListAsync(
            page,
            20,
            searchTerm,
            eventTypeId,
            startDate,
            endDate);

        return View(viewModel);
    }

    // GET: /PublicEvents/Details/5
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var viewModel = await _eventService.GetEventDetailViewModelAsync(id, user?.Id);

        if (viewModel == null)
        {
            return NotFound();
        }

        // Check if event is public or user is authenticated
        if (!viewModel.IsPublic && !User.Identity?.IsAuthenticated == true)
        {
            return Forbid();
        }

        viewModel.CanEdit = User.IsInRole("Admin");

        return View(viewModel);
    }

    // GET: /PublicEvents/Calendar
    [AllowAnonymous]
    public async Task<IActionResult> Calendar(int? year = null, int? month = null)
    {
        var targetMonth = year.HasValue && month.HasValue
            ? new DateTime(year.Value, month.Value, 1)
            : DateTime.Now;

        var viewModel = await _eventService.GetCalendarEventsAsync(targetMonth);

        return View(viewModel);
    }

    // GET: /PublicEvents/Map
    [AllowAnonymous]
    public async Task<IActionResult> Map()
    {
        var viewModel = await _eventService.GetMapEventsAsync();

        return View(viewModel);
    }

    // POST: /PublicEvents/SubmitRsvp
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRsvp(RsvpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ungültige RSVP-Anfrage.";
            return RedirectToAction(nameof(Details), new { id = model.EventId });
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            await _eventService.SubmitRsvpAsync(user.Id, model.EventId, model.RsvpStatus);

            TempData["SuccessMessage"] = "Ihre RSVP wurde erfolgreich gespeichert.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting RSVP for event {EventId}", model.EventId);
            TempData["ErrorMessage"] = "Fehler beim Speichern der RSVP.";
        }

        return RedirectToAction(nameof(Details), new { id = model.EventId });
    }

    // POST: /PublicEvents/UpdateRsvp
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRsvp(RsvpViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ungültige RSVP-Anfrage.";
            return RedirectToAction(nameof(Details), new { id = model.EventId });
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            await _eventService.UpdateRsvpAsync(user.Id, model.EventId, model.RsvpStatus);

            TempData["SuccessMessage"] = "Ihre RSVP wurde erfolgreich aktualisiert.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RSVP for event {EventId}", model.EventId);
            TempData["ErrorMessage"] = "Fehler beim Aktualisieren der RSVP.";
        }

        return RedirectToAction(nameof(Details), new { id = model.EventId });
    }
}
