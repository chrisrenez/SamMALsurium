using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Configuration;
using SamMALsurium.Models.Enums;
using SamMALsurium.Models.ViewModels;
using SamMALsurium.Models.ViewModels.Admin;

namespace SamMALsurium.Services;

public class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EventService> _logger;
    private readonly EventSettings _eventSettings;

    public EventService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<EventService> logger,
        IOptions<EventSettings> eventSettings)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _eventSettings = eventSettings.Value;
    }

    #region Basic CRUD Operations

    public async Task<List<Event>> GetAllEventsAsync()
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Include(e => e.Attendees)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Include(e => e.EventMedia)
            .Include(e => e.Attendees)
                .ThenInclude(a => a.User)
            .Include(e => e.Announcements)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event> CreateEventAsync(Event eventEntity)
    {
        eventEntity.CreatedAt = DateTime.UtcNow;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        eventEntity.IsActive = true;

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event created: {EventId} - {EventTitle}", eventEntity.Id, eventEntity.Title);

        return eventEntity;
    }

    public async Task<Event> UpdateEventAsync(Event eventEntity)
    {
        eventEntity.UpdatedAt = DateTime.UtcNow;

        _context.Events.Update(eventEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event updated: {EventId} - {EventTitle}", eventEntity.Id, eventEntity.Title);

        return eventEntity;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var eventEntity = await _context.Events.FindAsync(id);
        if (eventEntity == null)
            return false;

        // Soft delete
        eventEntity.IsActive = false;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Event soft deleted: {EventId} - {EventTitle}", eventEntity.Id, eventEntity.Title);

        return true;
    }

    #endregion

    #region Filtered Queries

    public async Task<List<Event>> GetEventsByTypeAsync(int eventTypeId)
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Where(e => e.EventTypeId == eventTypeId && e.IsActive)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<List<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Where(e => e.IsActive && e.StartDate >= startDate && e.EndDate <= endDate)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<List<Event>> GetUpcomingEventsAsync(int count = 10)
    {
        var now = DateTime.UtcNow;
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Where(e => e.IsActive && e.StartDate >= now)
            .OrderBy(e => e.StartDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Event>> GetPublicEventsAsync()
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Where(e => e.IsActive && e.IsPublic)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<List<Event>> GetMemberEventsAsync()
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Where(e => e.IsActive)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    #endregion

    #region RSVP Operations

    public async Task<EventAttendee> SubmitRsvpAsync(string userId, int eventId, RsvpStatus status)
    {
        var existing = await _context.EventAttendees
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId);

        if (existing != null)
        {
            // Update existing RSVP
            return await UpdateRsvpAsync(userId, eventId, status);
        }

        var attendee = new EventAttendee
        {
            EventId = eventId,
            UserId = userId,
            RsvpStatus = status,
            RsvpDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EventAttendees.Add(attendee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("RSVP submitted: Event {EventId}, User {UserId}, Status {Status}", eventId, userId, status);

        return attendee;
    }

    public async Task<EventAttendee> UpdateRsvpAsync(string userId, int eventId, RsvpStatus status)
    {
        var attendee = await _context.EventAttendees
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId);

        if (attendee == null)
        {
            throw new InvalidOperationException("RSVP not found");
        }

        attendee.RsvpStatus = status;
        attendee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("RSVP updated: Event {EventId}, User {UserId}, Status {Status}", eventId, userId, status);

        return attendee;
    }

    public async Task<List<EventAttendee>> GetEventAttendeesAsync(int eventId)
    {
        return await _context.EventAttendees
            .Include(ea => ea.User)
            .Where(ea => ea.EventId == eventId)
            .OrderBy(ea => ea.RsvpDate)
            .ToListAsync();
    }

    public async Task<RsvpStatus?> GetUserRsvpStatusAsync(string userId, int eventId)
    {
        var attendee = await _context.EventAttendees
            .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == userId);

        return attendee?.RsvpStatus;
    }

    public async Task<Dictionary<RsvpStatus, int>> GetRsvpCountsAsync(int eventId)
    {
        var attendees = await _context.EventAttendees
            .Where(ea => ea.EventId == eventId)
            .GroupBy(ea => ea.RsvpStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var counts = new Dictionary<RsvpStatus, int>
        {
            { RsvpStatus.Going, 0 },
            { RsvpStatus.Maybe, 0 },
            { RsvpStatus.NotGoing, 0 }
        };

        foreach (var item in attendees)
        {
            counts[item.Status] = item.Count;
        }

        return counts;
    }

    #endregion

    #region Admin Operations

    public async Task<Models.ViewModels.Admin.EventListViewModel> GetAdminEventListAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        int? eventTypeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isActive = null)
    {
        var query = _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Organizer)
            .Include(e => e.Attendees)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e => e.Title.Contains(searchTerm) ||
                                    (e.Description != null && e.Description.Contains(searchTerm)));
        }

        if (eventTypeId.HasValue)
        {
            query = query.Where(e => e.EventTypeId == eventTypeId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.StartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EndDate <= endDate.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(e => e.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var events = await query
            .OrderByDescending(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new Models.ViewModels.Admin.EventListItemViewModel
            {
                Id = e.Id,
                Title = e.Title,
                EventTypeName = e.EventType != null ? e.EventType.Name : "",
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                IsPublic = e.IsPublic,
                RsvpEnabled = e.RsvpEnabled,
                IsActive = e.IsActive,
                AttendeeCount = e.Attendees.Count(a => a.RsvpStatus == RsvpStatus.Going),
                OrganizerName = e.Organizer != null ? $"{e.Organizer.FirstName} {e.Organizer.LastName}" : "",
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        var eventTypes = await GetAllEventTypesAsync();

        return new Models.ViewModels.Admin.EventListViewModel
        {
            Events = events,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            SearchTerm = searchTerm,
            FilterEventTypeId = eventTypeId,
            FilterStartDate = startDate,
            FilterEndDate = endDate,
            FilterIsActive = isActive,
            EventTypes = eventTypes
        };
    }

    public async Task<EventDetailViewModel> GetEventDetailViewModelAsync(int eventId, string? userId = null)
    {
        var eventEntity = await GetEventByIdAsync(eventId);
        if (eventEntity == null)
        {
            throw new InvalidOperationException($"Event {eventId} not found");
        }

        var rsvpCounts = await GetRsvpCountsAsync(eventId);

        var viewModel = new EventDetailViewModel
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
            EventTypeName = eventEntity.EventType?.Name ?? "",
            OrganizerName = eventEntity.Organizer != null ? $"{eventEntity.Organizer.FirstName} {eventEntity.Organizer.LastName}" : "",
            IsPublic = eventEntity.IsPublic,
            RsvpEnabled = eventEntity.RsvpEnabled,
            CreatedAt = eventEntity.CreatedAt,
            GoingCount = rsvpCounts[RsvpStatus.Going],
            MaybeCount = rsvpCounts[RsvpStatus.Maybe],
            NotGoingCount = rsvpCounts[RsvpStatus.NotGoing],
            IsAuthenticated = !string.IsNullOrEmpty(userId)
        };

        // Get media
        var media = eventEntity.EventMedia ?? new List<EventMedia>();
        var coverImage = media.FirstOrDefault(m => m.MediaType == EventMediaType.CoverImage);
        if (coverImage != null)
        {
            viewModel.CoverImagePath = coverImage.FilePath;
        }

        viewModel.GalleryImagePaths = media
            .Where(m => m.MediaType == EventMediaType.GalleryImage)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => m.FilePath!)
            .ToList();

        viewModel.ExternalLinks = media
            .Where(m => m.MediaType == EventMediaType.ExternalLink)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => m.Url!)
            .ToList();

        viewModel.Attachments = media
            .Where(m => m.MediaType == EventMediaType.FileAttachment)
            .OrderBy(m => m.DisplayOrder)
            .ToList();

        // Get current user RSVP status
        if (!string.IsNullOrEmpty(userId))
        {
            viewModel.CurrentUserRsvpStatus = await GetUserRsvpStatusAsync(userId, eventId);
        }

        // Get attendees
        if (eventEntity.RsvpEnabled)
        {
            var attendees = await GetEventAttendeesAsync(eventId);
            viewModel.Attendees = attendees
                .Where(a => a.RsvpStatus == RsvpStatus.Going)
                .Select(a => new EventAttendeeViewModel
                {
                    UserId = a.UserId,
                    UserName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "",
                    RsvpStatus = a.RsvpStatus,
                    RsvpDate = a.RsvpDate
                })
                .ToList();
        }

        return viewModel;
    }

    public async Task<Models.ViewModels.EventListViewModel> GetPublicEventListAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        int? eventTypeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Events
            .Include(e => e.EventType)
            .Include(e => e.EventMedia)
            .Include(e => e.Attendees)
            .Where(e => e.IsActive && e.IsPublic)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e => e.Title.Contains(searchTerm) ||
                                    (e.Description != null && e.Description.Contains(searchTerm)));
        }

        if (eventTypeId.HasValue)
        {
            query = query.Where(e => e.EventTypeId == eventTypeId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.StartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EndDate <= endDate.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var events = await query
            .OrderBy(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventCardViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                EventTypeName = e.EventType != null ? e.EventType.Name : "",
                CoverImagePath = e.EventMedia
                    .Where(m => m.MediaType == EventMediaType.CoverImage)
                    .Select(m => m.FilePath)
                    .FirstOrDefault(),
                RsvpEnabled = e.RsvpEnabled,
                AttendeeCount = e.Attendees.Count(a => a.RsvpStatus == RsvpStatus.Going)
            })
            .ToListAsync();

        var eventTypes = await GetAllEventTypesAsync();

        return new Models.ViewModels.EventListViewModel
        {
            Events = events,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            SearchTerm = searchTerm,
            FilterEventTypeId = eventTypeId,
            FilterStartDate = startDate,
            FilterEndDate = endDate,
            EventTypes = eventTypes
        };
    }

    public async Task<EventCalendarViewModel> GetCalendarEventsAsync(DateTime month)
    {
        var startOfMonth = new DateTime(month.Year, month.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var events = await _context.Events
            .Include(e => e.EventType)
            .Where(e => e.IsActive && e.IsPublic && e.StartDate >= startOfMonth && e.StartDate <= endOfMonth)
            .OrderBy(e => e.StartDate)
            .Select(e => new EventCalendarItemViewModel
            {
                Id = e.Id,
                Title = e.Title,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                EventTypeName = e.EventType != null ? e.EventType.Name : "",
                Location = e.Location,
                RsvpEnabled = e.RsvpEnabled
            })
            .ToListAsync();

        return new EventCalendarViewModel
        {
            Events = events,
            CurrentMonth = startOfMonth
        };
    }

    public async Task<EventMapViewModel> GetMapEventsAsync()
    {
        var events = await _context.Events
            .Include(e => e.EventType)
            .Where(e => e.IsActive && e.IsPublic && e.Latitude.HasValue && e.Longitude.HasValue)
            .OrderBy(e => e.StartDate)
            .Select(e => new EventMapItemViewModel
            {
                Id = e.Id,
                Title = e.Title,
                StartDate = e.StartDate,
                Location = e.Location,
                LocationName = e.LocationName,
                Latitude = e.Latitude!.Value,
                Longitude = e.Longitude!.Value,
                EventTypeName = e.EventType != null ? e.EventType.Name : ""
            })
            .ToListAsync();

        return new EventMapViewModel
        {
            Events = events,
            GoogleMapsApiKey = _eventSettings.GoogleMapsApiKey
        };
    }

    #endregion

    #region Event Types

    public async Task<List<EventType>> GetAllEventTypesAsync()
    {
        return await _context.EventTypes
            .OrderBy(et => et.Name)
            .ToListAsync();
    }

    public async Task<EventType?> GetEventTypeByIdAsync(int id)
    {
        return await _context.EventTypes.FindAsync(id);
    }

    public async Task<EventType> CreateEventTypeAsync(EventType eventType)
    {
        _context.EventTypes.Add(eventType);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event type created: {EventTypeId} - {EventTypeName}", eventType.Id, eventType.Name);

        return eventType;
    }

    public async Task<EventType> UpdateEventTypeAsync(EventType eventType)
    {
        _context.EventTypes.Update(eventType);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event type updated: {EventTypeId} - {EventTypeName}", eventType.Id, eventType.Name);

        return eventType;
    }

    public async Task<bool> DeleteEventTypeAsync(int id)
    {
        var eventType = await _context.EventTypes.FindAsync(id);
        if (eventType == null)
            return false;

        // Check if any events use this type
        var hasEvents = await _context.Events.AnyAsync(e => e.EventTypeId == id);
        if (hasEvents)
        {
            throw new InvalidOperationException("Cannot delete event type that is in use by events");
        }

        _context.EventTypes.Remove(eventType);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event type deleted: {EventTypeId} - {EventTypeName}", eventType.Id, eventType.Name);

        return true;
    }

    #endregion

    #region Media Operations

    public async Task<EventMedia> AddEventMediaAsync(int eventId, EventMediaType mediaType, string? filePath, string? url, int displayOrder)
    {
        var media = new EventMedia
        {
            EventId = eventId,
            MediaType = mediaType,
            FilePath = filePath,
            Url = url,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow
        };

        _context.EventMedia.Add(media);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Media added to event: {EventId}, Type: {MediaType}", eventId, mediaType);

        return media;
    }

    public async Task<bool> DeleteEventMediaAsync(int mediaId)
    {
        var media = await _context.EventMedia.FindAsync(mediaId);
        if (media == null)
            return false;

        _context.EventMedia.Remove(media);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Media deleted: {MediaId}", mediaId);

        return true;
    }

    public async Task<List<EventMedia>> GetEventMediaAsync(int eventId)
    {
        return await _context.EventMedia
            .Where(em => em.EventId == eventId)
            .OrderBy(em => em.DisplayOrder)
            .ToListAsync();
    }

    #endregion

    #region Announcements

    public async Task<EventAnnouncement> SendAnnouncementAsync(int eventId, string message, string sentBy)
    {
        var announcement = new EventAnnouncement
        {
            EventId = eventId,
            Message = message,
            SentBy = sentBy,
            SentAt = DateTime.UtcNow
        };

        _context.EventAnnouncements.Add(announcement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Announcement sent for event: {EventId} by {UserId}", eventId, sentBy);

        return announcement;
    }

    public async Task<List<EventAnnouncement>> GetEventAnnouncementsAsync(int eventId)
    {
        return await _context.EventAnnouncements
            .Include(ea => ea.Sender)
            .Where(ea => ea.EventId == eventId)
            .OrderByDescending(ea => ea.SentAt)
            .ToListAsync();
    }

    #endregion
}
