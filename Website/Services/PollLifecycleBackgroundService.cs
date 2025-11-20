using Microsoft.EntityFrameworkCore;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;
using SamMALsurium.Services.Polls;

namespace SamMALsurium.Services;

/// <summary>
/// Background service that runs periodically to manage poll lifecycle.
/// Automatically closes polls that have reached their end date and
/// triggers notifications for polls closing soon.
/// </summary>
public class PollLifecycleBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PollLifecycleBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _closingSoonThreshold = TimeSpan.FromHours(24);

    public PollLifecycleBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PollLifecycleBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PollLifecycleBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPollLifecycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing poll lifecycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("PollLifecycleBackgroundService stopped");
    }

    private async Task ProcessPollLifecycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;

        // Find polls that should be auto-closed
        var pollsToClose = await dbContext.Polls
            .Where(p => p.Status == PollStatus.Active &&
                       p.EndDate.HasValue &&
                       p.EndDate.Value <= now)
            .Include(p => p.Event)
            .Include(p => p.CreatedBy)
            .ToListAsync(cancellationToken);

        if (pollsToClose.Any())
        {
            _logger.LogInformation("Auto-closing {Count} polls that have reached their end date", pollsToClose.Count);

            foreach (var poll in pollsToClose)
            {
                poll.Status = PollStatus.Closed;
                _logger.LogInformation("Auto-closed poll {PollId}: {PollTitle}", poll.Id, poll.Title);

                // Send results available notification if results were set to AfterVoting
                if (poll.ResultsVisibility == ResultsVisibility.AfterVoting)
                {
                    await SendResultsAvailableNotificationAsync(poll, dbContext, emailService, cancellationToken);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Find polls closing soon that haven't been notified yet
        var closingSoonTime = now.Add(_closingSoonThreshold);
        var pollsClosingSoon = await dbContext.Polls
            .Where(p => p.Status == PollStatus.Active &&
                       p.EndDate.HasValue &&
                       p.EndDate.Value > now &&
                       p.EndDate.Value <= closingSoonTime)
            .Include(p => p.Event)
            .Include(p => p.CreatedBy)
            .ToListAsync(cancellationToken);

        if (pollsClosingSoon.Any())
        {
            _logger.LogInformation("Found {Count} polls closing within 24 hours", pollsClosingSoon.Count);

            foreach (var poll in pollsClosingSoon)
            {
                await SendClosingSoonNotificationAsync(poll, dbContext, emailService, cancellationToken);
            }
        }
    }

    private async Task SendResultsAvailableNotificationAsync(
        Models.Poll poll,
        ApplicationDbContext dbContext,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get users who should be notified (event participants or all users with notification preferences)
            var recipients = await GetNotificationRecipientsAsync(poll, dbContext, cancellationToken);

            foreach (var user in recipients)
            {
                // Skip if user has disabled poll notifications
                if (!user.EnablePollNotifications)
                {
                    continue;
                }

                var model = new
                {
                    UserName = user.UserName,
                    PollTitle = poll.Title,
                    PollDescription = poll.Description,
                    EventTitle = poll.Event?.Title,
                    ResultsUrl = $"/Poll/Results/{poll.Id}"
                };

                await emailService.SendPollResultsAvailableNotificationAsync(
                    user.Email!,
                    user.UserName!,
                    poll.Title,
                    poll.Description,
                    poll.Event?.Title,
                    $"/Poll/Results/{poll.Id}");

                _logger.LogInformation("Sent results available notification for poll {PollId} to {Email}", poll.Id, user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending results available notification for poll {PollId}", poll.Id);
        }
    }

    private async Task SendClosingSoonNotificationAsync(
        Models.Poll poll,
        ApplicationDbContext dbContext,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if we've already sent the closing soon notification
            // We'll use a simple approach: only send if the poll is more than 24.5 hours from end
            // This prevents sending multiple notifications in subsequent runs
            var timeUntilEnd = poll.EndDate!.Value - DateTime.UtcNow;
            if (timeUntilEnd.TotalHours > 24.5)
            {
                return; // Not quite time yet
            }

            // Get users who should be notified
            var recipients = await GetNotificationRecipientsAsync(poll, dbContext, cancellationToken);

            // Filter to users who haven't voted yet
            var voterIds = await GetPollVoterIdsAsync(poll, dbContext, cancellationToken);

            foreach (var user in recipients)
            {
                // Skip if user has disabled poll notifications
                if (!user.EnablePollNotifications)
                {
                    continue;
                }

                // Skip if user already voted
                if (voterIds.Contains(user.Id))
                {
                    continue;
                }

                var model = new
                {
                    UserName = user.UserName,
                    PollTitle = poll.Title,
                    PollDescription = poll.Description,
                    EventTitle = poll.Event?.Title,
                    EndDate = poll.EndDate.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    VoteUrl = $"/Poll/Details/{poll.Id}"
                };

                await emailService.SendPollClosingSoonNotificationAsync(
                    user.Email!,
                    user.UserName!,
                    poll.Title,
                    poll.Description,
                    poll.Event?.Title,
                    poll.EndDate.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    $"/Poll/Details/{poll.Id}");

                _logger.LogInformation("Sent closing soon notification for poll {PollId} to {Email}", poll.Id, user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending closing soon notification for poll {PollId}", poll.Id);
        }
    }

    private async Task<List<ApplicationUser>> GetNotificationRecipientsAsync(
        Models.Poll poll,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // For MVP, send to all active users
        // In future, this could be refined to:
        // - Event participants only (if poll has EventId)
        // - Users following the event
        // - Users who have voted in past polls
        var recipients = await dbContext.Users
            .Where(u => u.AccountStatus == AccountStatus.Active)
            .ToListAsync(cancellationToken);

        return recipients;
    }

    private async Task<HashSet<string>> GetPollVoterIdsAsync(
        Models.Poll poll,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var voterIds = new HashSet<string>();

        switch (poll.Type)
        {
            case PollType.MultipleChoice:
                var mcVoterIds = await dbContext.PollVotes_MultipleChoice
                    .Where(v => v.PollId == poll.Id)
                    .Select(v => v.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                voterIds.UnionWith(mcVoterIds);
                break;

            case PollType.RankedChoice:
                var rcVoterIds = await dbContext.PollVotes_RankedChoice
                    .Where(v => v.PollId == poll.Id)
                    .Select(v => v.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                voterIds.UnionWith(rcVoterIds);
                break;

            case PollType.ScoreVoting:
                var svVoterIds = await dbContext.PollVotes_ScoreVoting
                    .Where(v => v.PollId == poll.Id)
                    .Select(v => v.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                voterIds.UnionWith(svVoterIds);
                break;

            case PollType.AvailabilityGrid:
                var agVoterIds = await dbContext.PollVotes_AvailabilityGrid
                    .Where(v => v.PollId == poll.Id)
                    .Select(v => v.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                voterIds.UnionWith(agVoterIds);
                break;
        }

        return voterIds;
    }
}
