using SamMALsurium.Models;
using SamMALsurium.Models.Enums;
using SamMALsurium.Models.ViewModels;
using SamMALsurium.Models.ViewModels.Admin;

namespace SamMALsurium.Services;

public interface IEventService
{
    // Basic CRUD operations
    Task<List<Event>> GetAllEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
    Task<Event> CreateEventAsync(Event eventEntity);
    Task<Event> UpdateEventAsync(Event eventEntity);
    Task<bool> DeleteEventAsync(int id);

    // Filtered queries
    Task<List<Event>> GetEventsByTypeAsync(int eventTypeId);
    Task<List<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<List<Event>> GetUpcomingEventsAsync(int count = 10);
    Task<List<Event>> GetPublicEventsAsync();
    Task<List<Event>> GetMemberEventsAsync();

    // RSVP operations
    Task<EventAttendee> SubmitRsvpAsync(string userId, int eventId, RsvpStatus status);
    Task<EventAttendee> UpdateRsvpAsync(string userId, int eventId, RsvpStatus status);
    Task<List<EventAttendee>> GetEventAttendeesAsync(int eventId);
    Task<RsvpStatus?> GetUserRsvpStatusAsync(string userId, int eventId);
    Task<Dictionary<RsvpStatus, int>> GetRsvpCountsAsync(int eventId);

    // Admin operations
    Task<Models.ViewModels.Admin.EventListViewModel> GetAdminEventListAsync(int page, int pageSize, string? searchTerm = null, int? eventTypeId = null, DateTime? startDate = null, DateTime? endDate = null, bool? isActive = null);
    Task<EventDetailViewModel> GetEventDetailViewModelAsync(int eventId, string? userId = null);
    Task<Models.ViewModels.EventListViewModel> GetPublicEventListAsync(int page, int pageSize, string? searchTerm = null, int? eventTypeId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<EventCalendarViewModel> GetCalendarEventsAsync(DateTime month);
    Task<EventMapViewModel> GetMapEventsAsync();

    // Event types
    Task<List<EventType>> GetAllEventTypesAsync();
    Task<EventType?> GetEventTypeByIdAsync(int id);
    Task<EventType> CreateEventTypeAsync(EventType eventType);
    Task<EventType> UpdateEventTypeAsync(EventType eventType);
    Task<bool> DeleteEventTypeAsync(int id);

    // Media operations
    Task<EventMedia> AddEventMediaAsync(int eventId, EventMediaType mediaType, string? filePath, string? url, int displayOrder);
    Task<bool> DeleteEventMediaAsync(int mediaId);
    Task<List<EventMedia>> GetEventMediaAsync(int eventId);

    // Announcements
    Task<EventAnnouncement> SendAnnouncementAsync(int eventId, string message, string sentBy);
    Task<List<EventAnnouncement>> GetEventAnnouncementsAsync(int eventId);
}
