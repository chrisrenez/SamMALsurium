using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.ViewModels.Admin;
using SamMALsurium.Services;

namespace SamMALsurium.Controllers;

public class EventTypesController : BaseAdminController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEventService _eventService;
    private readonly ILogger<EventTypesController> _logger;

    public EventTypesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEventService eventService,
        ILogger<EventTypesController> logger)
    {
        _context = context;
        _userManager = userManager;
        _eventService = eventService;
        _logger = logger;
    }

    // GET: /EventTypes/Index
    public async Task<IActionResult> Index()
    {
        var eventTypes = await _eventService.GetAllEventTypesAsync();
        return View(eventTypes);
    }

    // GET: /EventTypes/Create
    public IActionResult Create()
    {
        return View(new EventTypeViewModel());
    }

    // POST: /EventTypes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var eventType = new EventType
            {
                Name = model.Name,
                Description = model.Description
            };

            await _eventService.CreateEventTypeAsync(eventType);

            // Log admin action
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = user.Id,
                Action = "EventTypeCreated",
                Details = $"Created event type: {eventType.Name}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Event-Typ '{eventType.Name}' wurde erfolgreich erstellt.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event type");
            ModelState.AddModelError("", "Fehler beim Erstellen des Event-Typs.");
            return View(model);
        }
    }

    // GET: /EventTypes/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var eventType = await _eventService.GetEventTypeByIdAsync(id);
        if (eventType == null)
        {
            return NotFound();
        }

        var model = new EventTypeViewModel
        {
            Id = eventType.Id,
            Name = eventType.Name,
            Description = eventType.Description
        };

        return View(model);
    }

    // POST: /EventTypes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventTypeViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var eventType = await _eventService.GetEventTypeByIdAsync(id);
            if (eventType == null)
            {
                return NotFound();
            }

            eventType.Name = model.Name;
            eventType.Description = model.Description;

            await _eventService.UpdateEventTypeAsync(eventType);

            // Log admin action
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = user.Id,
                Action = "EventTypeUpdated",
                Details = $"Updated event type: {eventType.Name}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Event-Typ '{eventType.Name}' wurde erfolgreich aktualisiert.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event type {EventTypeId}", id);
            ModelState.AddModelError("", "Fehler beim Aktualisieren des Event-Typs.");
            return View(model);
        }
    }

    // POST: /EventTypes/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var eventType = await _eventService.GetEventTypeByIdAsync(id);
            if (eventType == null)
            {
                return NotFound();
            }

            var eventTypeName = eventType.Name;
            await _eventService.DeleteEventTypeAsync(id);

            // Log admin action
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = user.Id,
                Action = "EventTypeDeleted",
                Details = $"Deleted event type: {eventTypeName}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Event-Typ '{eventTypeName}' wurde erfolgreich gelöscht.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete event type {EventTypeId} - in use", id);
            TempData["ErrorMessage"] = "Event-Typ kann nicht gelöscht werden, da er von Events verwendet wird.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event type {EventTypeId}", id);
            TempData["ErrorMessage"] = "Fehler beim Löschen des Event-Typs.";
        }

        return RedirectToAction(nameof(Index));
    }
}
