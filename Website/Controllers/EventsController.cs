using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;
using SamMALsurium.Models.ViewModels.Admin;
using SamMALsurium.Services;

namespace SamMALsurium.Controllers;

public class EventsController : BaseAdminController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEventService _eventService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEventService eventService,
        IImageStorageService imageStorageService,
        ILogger<EventsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _eventService = eventService;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    // GET: /Events/Index
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        int? eventTypeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isActive = null,
        int page = 1)
    {
        var viewModel = await _eventService.GetAdminEventListAsync(
            page,
            20,
            searchTerm,
            eventTypeId,
            startDate,
            endDate,
            isActive);

        return View(viewModel);
    }

    // GET: /Events/Create
    public async Task<IActionResult> Create()
    {
        var eventTypes = await _eventService.GetAllEventTypesAsync();
        ViewBag.EventTypes = new SelectList(eventTypes, "Id", "Name");

        return View(new EventCreateViewModel());
    }

    // POST: /Events/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var eventTypes = await _eventService.GetAllEventTypesAsync();
            ViewBag.EventTypes = new SelectList(eventTypes, "Id", "Name", model.EventTypeId);
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Create event entity
            var eventEntity = new Event
            {
                Title = model.Title,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Location = model.Location,
                LocationName = model.LocationName,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                OrganizedBy = user.Id,
                CreatedById = user.Id,
                IsPublic = model.IsPublic,
                RsvpEnabled = model.RsvpEnabled,
                EventTypeId = model.EventTypeId
            };

            var createdEvent = await _eventService.CreateEventAsync(eventEntity);

            // Handle cover image upload
            if (model.CoverImage != null)
            {
                var coverImagePath = await _imageStorageService.SaveOriginalAsync(model.CoverImage, user.Id);
                await _eventService.AddEventMediaAsync(
                    createdEvent.Id,
                    EventMediaType.CoverImage,
                    coverImagePath,
                    null,
                    0);
            }

            // Handle gallery images upload
            if (model.GalleryImages != null && model.GalleryImages.Any())
            {
                int displayOrder = 1;
                foreach (var image in model.GalleryImages)
                {
                    var imagePath = await _imageStorageService.SaveOriginalAsync(image, user.Id);
                    await _eventService.AddEventMediaAsync(
                        createdEvent.Id,
                        EventMediaType.GalleryImage,
                        imagePath,
                        null,
                        displayOrder++);
                }
            }

            // Handle external links
            if (!string.IsNullOrWhiteSpace(model.ExternalLinks))
            {
                var links = model.ExternalLinks.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                int displayOrder = 1;
                foreach (var link in links)
                {
                    await _eventService.AddEventMediaAsync(
                        createdEvent.Id,
                        EventMediaType.ExternalLink,
                        null,
                        link.Trim(),
                        displayOrder++);
                }
            }

            // Handle file attachments
            if (model.Attachments != null && model.Attachments.Any())
            {
                int displayOrder = 1;
                foreach (var attachment in model.Attachments)
                {
                    var filePath = await _imageStorageService.SaveOriginalAsync(attachment, user.Id);
                    await _eventService.AddEventMediaAsync(
                        createdEvent.Id,
                        EventMediaType.FileAttachment,
                        filePath,
                        null,
                        displayOrder++);
                }
            }

            // Log admin action
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = user.Id,
                Action = "EventCreated",
                Details = $"Created event: {createdEvent.Title}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Event '{createdEvent.Title}' wurde erfolgreich erstellt.";
            return RedirectToAction(nameof(Details), new { id = createdEvent.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            ModelState.AddModelError("", "Fehler beim Erstellen des Events.");

            var eventTypes = await _eventService.GetAllEventTypesAsync();
            ViewBag.EventTypes = new SelectList(eventTypes, "Id", "Name", model.EventTypeId);
            return View(model);
        }
    }

    // GET: /Events/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var eventEntity = await _eventService.GetEventByIdAsync(id);
        if (eventEntity == null)
        {
            return NotFound();
        }

        var model = new EventEditViewModel
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            Location = eventEntity.Location,
            LocationName = eventEntity.LocationName,
            Latitude = eventEntity.Latitude,
            Longitude = eventEntity.Longitude,
            EventTypeId = eventEntity.EventTypeId,
            IsPublic = eventEntity.IsPublic,
            RsvpEnabled = eventEntity.RsvpEnabled,
            IsActive = eventEntity.IsActive,
            ExistingMedia = eventEntity.EventMedia?.ToList()
        };

        // Populate external links textarea
        var externalLinks = eventEntity.EventMedia?
            .Where(m => m.MediaType == EventMediaType.ExternalLink)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => m.Url)
            .ToList();
        if (externalLinks != null && externalLinks.Any())
        {
            model.ExternalLinks = string.Join("\n", externalLinks);
        }

        var eventTypes = await _eventService.GetAllEventTypesAsync();
        ViewBag.EventTypes = new SelectList(eventTypes, "Id", "Name", model.EventTypeId);

        return View(model);
    }

    // POST: /Events/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var eventTypes = await _eventService.GetAllEventTypesAsync();
            ViewBag.EventTypes = new SelectList(eventTypes, "Id", "Name", model.EventTypeId);
            return View(model);
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var eventEntity = await _eventService.GetEventByIdAsync(id);
            if (eventEntity == null)
            {
                return NotFound();
            }

            // Update event properties
            eventEntity.Title = model.Title;
            eventEntity.Description = model.Description;
            eventEntity.StartDate = model.StartDate;
            eventEntity.EndDate = model.EndDate;
            eventEntity.Location = model.Location;
            eventEntity.LocationName = model.LocationName;
            eventEntity.Latitude = model.Latitude;
            eventEntity.Longitude = model.Longitude;
            eventEntity.EventTypeId = model.EventTypeId;
            eventEntity.IsPublic = model.IsPublic;
            eventEntity.RsvpEnabled = model.RsvpEnabled;
            eventEntity.IsActive = model.IsActive;

            await _eventService.UpdateEventAsync(eventEntity);

            // Handle media deletions
            if (model.MediaToDelete != null && model.MediaToDelete.Any())
            {
                foreach (var mediaId in model.MediaToDelete)
                {
                    await _eventService.DeleteEventMediaAsync(mediaId);
                }
            }

            // Handle new cover image
            if (model.CoverImage != null)
            {
                // Delete old cover image
                var oldCoverImage = eventEntity.EventMedia?
                    .FirstOrDefault(m => m.MediaType == EventMediaType.CoverImage);
                if (oldCoverImage != null)
                {
                    await _eventService.DeleteEventMediaAsync(oldCoverImage.Id);
                }

                // Upload new cover image
                var coverImagePath = await _imageStorageService.SaveOriginalAsync(model.CoverImage, user.Id);
                await _eventService.AddEventMediaAsync(
                    eventEntity.Id,
                    EventMediaType.CoverImage,
                    coverImagePath,
                    null,
                    0);
            }

            // Handle new gallery images
            if (model.GalleryImages != null && model.GalleryImages.Any())
            {
                var existingGalleryCount = eventEntity.EventMedia?
                    .Count(m => m.MediaType == EventMediaType.GalleryImage) ?? 0;
                int displayOrder = existingGalleryCount + 1;

                foreach (var image in model.GalleryImages)
                {
                    var imagePath = await _imageStorageService.SaveOriginalAsync(image, user.Id);
                    await _eventService.AddEventMediaAsync(
                        eventEntity.Id,
                        EventMediaType.GalleryImage,
                        imagePath,
                        null,
                        displayOrder++);
                }
            }

            // Handle external links (replace all)
            var oldLinks = eventEntity.EventMedia?
                .Where(m => m.MediaType == EventMediaType.ExternalLink)
                .ToList();
            if (oldLinks != null)
            {
                foreach (var link in oldLinks)
                {
                    await _eventService.DeleteEventMediaAsync(link.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(model.ExternalLinks))
            {
                var links = model.ExternalLinks.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                int displayOrder = 1;
                foreach (var link in links)
                {
                    await _eventService.AddEventMediaAsync(
                        eventEntity.Id,
                        EventMediaType.ExternalLink,
                        null,
                        link.Trim(),
                        displayOrder++);
                }
            }

            // Handle new file attachments
            if (model.Attachments != null && model.Attachments.Any())
            {
                var existingAttachmentCount = eventEntity.EventMedia?
                    .Count(m => m.MediaType == EventMediaType.FileAttachment) ?? 0;
                int displayOrder = existingAttachmentCount + 1;

                foreach (var attachment in model.Attachments)
                {
                    var filePath = await _imageStorageService.SaveOriginalAsync(attachment, user.Id);
                    await _eventService.AddEventMediaAsync(
                        eventEntity.Id,
                        EventMediaType.FileAttachment,
                        filePath,
                        null,
                        displayOrder++);
                }
            }

            // Log admin action
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = user.Id,
                Action = "EventUpdated",
                Details = $"Updated event: {eventEntity.Title}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Event '{eventEntity.Title}' wurde erfolgreich aktualisiert.";
            return RedirectToAction(nameof(Details), new { id = eventEntity.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", id);
            ModelState.AddModelError("", "Fehler beim Aktualisieren des Events.");

            var eventTypes = await _eventService.GetAllEventTypesAsync();
            ViewBag.EventTypes = new SelectList(eventTypes, "Id", "Name", model.EventTypeId);
            return View(model);
        }
    }

    // GET: /Events/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var viewModel = await _eventService.GetEventDetailViewModelAsync(id, user?.Id);

        if (viewModel == null)
        {
            return NotFound();
        }

        viewModel.CanEdit = true; // Admin can always edit

        return View(viewModel);
    }

    // POST: /Events/Delete/5
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

            var eventEntity = await _eventService.GetEventByIdAsync(id);
            if (eventEntity == null)
            {
                return NotFound();
            }

            var eventTitle = eventEntity.Title;
            await _eventService.DeleteEventAsync(id);

            // Log admin action
            _context.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminUserId = user.Id,
                Action = "EventDeleted",
                Details = $"Deleted event: {eventTitle}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Event '{eventTitle}' wurde erfolgreich gelöscht.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            TempData["ErrorMessage"] = "Fehler beim Löschen des Events.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // POST: /Events/SendAnnouncement/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAnnouncement(EventAnnouncementViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Ungültige Ankündigung.";
            return RedirectToAction(nameof(Details), new { id = model.EventId });
        }

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            await _eventService.SendAnnouncementAsync(model.EventId, model.Message, user.Id);

            TempData["SuccessMessage"] = "Ankündigung wurde erfolgreich gesendet.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending announcement for event {EventId}", model.EventId);
            TempData["ErrorMessage"] = "Fehler beim Senden der Ankündigung.";
        }

        return RedirectToAction(nameof(Details), new { id = model.EventId });
    }
}
