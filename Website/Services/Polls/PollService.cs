using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Services.Polls;

public class PollService : IPollService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PollService> _logger;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;
    private const int CacheExpirationMinutes = 5;

    public PollService(ApplicationDbContext context, ILogger<PollService> logger, IEmailService emailService, IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _cache = cache;
    }

    public async Task<Poll> CreatePollAsync(Poll poll)
    {
        // Validate dates
        if (poll.StartDate.HasValue && poll.EndDate.HasValue && poll.StartDate >= poll.EndDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        // Validate minimum options
        if (poll.Options == null || poll.Options.Count < 2)
        {
            throw new ArgumentException("Poll must have at least 2 options");
        }

        // Validate maximum options
        if (poll.Options.Count > 50)
        {
            throw new ArgumentException("Poll cannot have more than 50 options");
        }

        // Set display order for options if not set
        var order = 0;
        foreach (var option in poll.Options)
        {
            if (option.DisplayOrder == 0)
            {
                option.DisplayOrder = order++;
            }
        }

        poll.CreatedAt = DateTime.UtcNow;
        poll.Status = PollStatus.Active;

        _context.Polls.Add(poll);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Poll created with ID {PollId} by user {UserId}", poll.Id, poll.CreatedById);

        // Invalidate cache for event polls if associated with an event
        if (poll.EventId.HasValue)
        {
            _cache.Remove($"event_polls_{poll.EventId.Value}");
        }

        // Send notifications to users with poll notifications enabled
        await SendPollCreatedNotificationsAsync(poll);

        return poll;
    }

    private async Task SendPollCreatedNotificationsAsync(Poll poll)
    {
        try
        {
            // Reload poll with navigation properties
            var pollWithDetails = await _context.Polls
                .Include(p => p.Event)
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == poll.Id);

            if (pollWithDetails == null)
            {
                return;
            }

            // Get users who have poll notifications enabled and are active
            var recipients = await _context.Users
                .Where(u => u.EnablePollNotifications && u.AccountStatus == AccountStatus.Active)
                .ToListAsync();

            foreach (var user in recipients)
            {
                try
                {
                    await _emailService.SendPollCreatedNotificationAsync(
                        user.Email!,
                        user.UserName!,
                        pollWithDetails.Title,
                        pollWithDetails.Description,
                        pollWithDetails.Event?.Title,
                        $"/Poll/Details/{pollWithDetails.Id}");

                    _logger.LogInformation("Sent poll created notification for poll {PollId} to {Email}", pollWithDetails.Id, user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending poll created notification to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending poll created notifications for poll {PollId}", poll.Id);
        }
    }

    public async Task<Poll?> GetPollByIdAsync(int pollId)
    {
        return await _context.Polls
            .Include(p => p.Options)
            .Include(p => p.CreatedBy)
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == pollId);
    }

    public async Task<List<Poll>> GetPollsByEventAsync(int eventId)
    {
        var cacheKey = $"event_polls_{eventId}";

        if (!_cache.TryGetValue(cacheKey, out List<Poll>? polls))
        {
            polls = await _context.Polls
                .Include(p => p.Options)
                .Include(p => p.CreatedBy)
                .Where(p => p.EventId == eventId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes));

            _cache.Set(cacheKey, polls, cacheOptions);
            _logger.LogDebug("Cached polls for event {EventId}", eventId);
        }

        return polls ?? new List<Poll>();
    }

    public async Task<List<Poll>> GetAllPollsAsync(
        PollStatus? status = null,
        PollType? type = null,
        int? eventId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int skip = 0,
        int take = 50)
    {
        var query = _context.Polls
            .Include(p => p.Options)
            .Include(p => p.CreatedBy)
            .Include(p => p.Event)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        if (eventId.HasValue)
        {
            query = query.Where(p => p.EventId == eventId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Poll> UpdatePollAsync(Poll poll)
    {
        var existingPoll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == poll.Id);

        if (existingPoll == null)
        {
            throw new ArgumentException($"Poll with ID {poll.Id} not found");
        }

        // Check if votes have been cast
        var hasVotes = await GetVoteCountAsync(poll.Id) > 0;

        if (hasVotes)
        {
            // Only allow updating certain fields after votes have been cast
            existingPoll.Title = poll.Title;
            existingPoll.Description = poll.Description;
            existingPoll.EndDate = poll.EndDate;
            existingPoll.IsAnonymous = poll.IsAnonymous;
            existingPoll.ResultsVisibility = poll.ResultsVisibility;
            existingPoll.AllowVoteChange = poll.AllowVoteChange;

            _logger.LogInformation("Poll {PollId} updated (limited update due to existing votes)", poll.Id);
        }
        else
        {
            // Full update allowed if no votes
            existingPoll.Title = poll.Title;
            existingPoll.Description = poll.Description;
            existingPoll.Type = poll.Type;
            existingPoll.Target = poll.Target;
            existingPoll.IsMultiSelect = poll.IsMultiSelect;
            existingPoll.ScoreMin = poll.ScoreMin;
            existingPoll.ScoreMax = poll.ScoreMax;
            existingPoll.IsAnonymous = poll.IsAnonymous;
            existingPoll.ResultsVisibility = poll.ResultsVisibility;
            existingPoll.AllowVoteChange = poll.AllowVoteChange;
            existingPoll.StartDate = poll.StartDate;
            existingPoll.EndDate = poll.EndDate;
            existingPoll.EventId = poll.EventId;

            // Update options
            _context.PollOptions.RemoveRange(existingPoll.Options);
            existingPoll.Options = poll.Options;

            _logger.LogInformation("Poll {PollId} updated (full update, no votes cast)", poll.Id);
        }

        await _context.SaveChangesAsync();

        // Invalidate cache for event polls if associated with an event
        if (existingPoll.EventId.HasValue)
        {
            _cache.Remove($"event_polls_{existingPoll.EventId.Value}");
        }

        return existingPoll;
    }

    public async Task<Poll> ClosePollAsync(int pollId, string userId)
    {
        var poll = await GetPollByIdAsync(pollId);
        if (poll == null)
        {
            throw new ArgumentException($"Poll with ID {pollId} not found");
        }

        if (poll.Status == PollStatus.Closed || poll.Status == PollStatus.Archived)
        {
            throw new InvalidOperationException($"Poll is already {poll.Status.ToString().ToLower()}");
        }

        poll.Status = PollStatus.Closed;
        await _context.SaveChangesAsync();

        // Invalidate cache for event polls if associated with an event
        if (poll.EventId.HasValue)
        {
            _cache.Remove($"event_polls_{poll.EventId.Value}");
        }

        _logger.LogInformation("Poll {PollId} closed by user {UserId}", pollId, userId);

        return poll;
    }

    public async Task<Poll> ArchivePollAsync(int pollId, string userId)
    {
        var poll = await GetPollByIdAsync(pollId);
        if (poll == null)
        {
            throw new ArgumentException($"Poll with ID {pollId} not found");
        }

        poll.Status = PollStatus.Archived;
        await _context.SaveChangesAsync();

        // Invalidate cache for event polls if associated with an event
        if (poll.EventId.HasValue)
        {
            _cache.Remove($"event_polls_{poll.EventId.Value}");
        }

        _logger.LogInformation("Poll {PollId} archived by user {UserId}", pollId, userId);

        return poll;
    }

    public async Task<Dictionary<string, object>> GetPollStatisticsAsync()
    {
        var totalPolls = await _context.Polls.CountAsync();
        var activePolls = await _context.Polls.CountAsync(p => p.Status == PollStatus.Active);
        var closedPolls = await _context.Polls.CountAsync(p => p.Status == PollStatus.Closed);
        var archivedPolls = await _context.Polls.CountAsync(p => p.Status == PollStatus.Archived);

        var totalVotes =
            await _context.PollVotes_MultipleChoice.CountAsync() +
            await _context.PollVotes_RankedChoice.CountAsync() +
            await _context.PollVotes_ScoreVoting.CountAsync() +
            await _context.PollVotes_AvailabilityGrid.CountAsync();

        // Get most active polls by vote count
        var pollVoteCounts = new Dictionary<int, int>();

        var mcVotes = await _context.PollVotes_MultipleChoice
            .GroupBy(v => v.PollId)
            .Select(g => new { PollId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var vote in mcVotes)
        {
            pollVoteCounts[vote.PollId] = pollVoteCounts.GetValueOrDefault(vote.PollId, 0) + vote.Count;
        }

        var rcVotes = await _context.PollVotes_RankedChoice
            .GroupBy(v => v.PollId)
            .Select(g => new { PollId = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .ToListAsync();

        foreach (var vote in rcVotes)
        {
            pollVoteCounts[vote.PollId] = pollVoteCounts.GetValueOrDefault(vote.PollId, 0) + vote.Count;
        }

        var svVotes = await _context.PollVotes_ScoreVoting
            .GroupBy(v => v.PollId)
            .Select(g => new { PollId = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .ToListAsync();

        foreach (var vote in svVotes)
        {
            pollVoteCounts[vote.PollId] = pollVoteCounts.GetValueOrDefault(vote.PollId, 0) + vote.Count;
        }

        var agVotes = await _context.PollVotes_AvailabilityGrid
            .GroupBy(v => v.PollId)
            .Select(g => new { PollId = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .ToListAsync();

        foreach (var vote in agVotes)
        {
            pollVoteCounts[vote.PollId] = pollVoteCounts.GetValueOrDefault(vote.PollId, 0) + vote.Count;
        }

        var mostActivePolls = pollVoteCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp => new { PollId = kvp.Key, VoteCount = kvp.Value })
            .ToList();

        return new Dictionary<string, object>
        {
            { "TotalPolls", totalPolls },
            { "ActivePolls", activePolls },
            { "ClosedPolls", closedPolls },
            { "ArchivedPolls", archivedPolls },
            { "TotalVotes", totalVotes },
            { "MostActivePolls", mostActivePolls }
        };
    }

    public async Task<byte[]> ExportResultsToCsvAsync(int pollId)
    {
        var poll = await GetPollByIdAsync(pollId);
        if (poll == null)
        {
            throw new ArgumentException($"Poll with ID {pollId} not found");
        }

        var csv = new StringBuilder();
        csv.AppendLine($"Poll: {poll.Title}");
        csv.AppendLine($"Type: {poll.Type}");
        csv.AppendLine($"Status: {poll.Status}");
        csv.AppendLine($"Created: {poll.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine();

        switch (poll.Type)
        {
            case PollType.MultipleChoice:
                csv.AppendLine("Option,Votes,Percentage");
                var mcVotes = await _context.PollVotes_MultipleChoice
                    .Where(v => v.PollId == pollId)
                    .GroupBy(v => v.OptionId)
                    .Select(g => new { OptionId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var totalMcVotes = mcVotes.Sum(v => v.Count);
                foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
                {
                    var voteCount = mcVotes.FirstOrDefault(v => v.OptionId == option.Id)?.Count ?? 0;
                    var percentage = totalMcVotes > 0 ? (voteCount * 100.0 / totalMcVotes) : 0;
                    csv.AppendLine($"\"{option.Label}\",{voteCount},{percentage:F2}%");
                }
                break;

            case PollType.RankedChoice:
                csv.AppendLine("Option,Average Rank,Total Rankings");
                var rcVotes = await _context.PollVotes_RankedChoice
                    .Where(v => v.PollId == pollId)
                    .GroupBy(v => v.OptionId)
                    .Select(g => new { OptionId = g.Key, AvgRank = g.Average(x => x.Rank), Count = g.Count() })
                    .ToListAsync();

                foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
                {
                    var stats = rcVotes.FirstOrDefault(v => v.OptionId == option.Id);
                    var avgRank = stats?.AvgRank ?? 0;
                    var count = stats?.Count ?? 0;
                    csv.AppendLine($"\"{option.Label}\",{avgRank:F2},{count}");
                }
                break;

            case PollType.ScoreVoting:
                csv.AppendLine("Option,Average Score,Total Scores,Min Score,Max Score");
                var svVotes = await _context.PollVotes_ScoreVoting
                    .Where(v => v.PollId == pollId)
                    .GroupBy(v => v.OptionId)
                    .Select(g => new
                    {
                        OptionId = g.Key,
                        AvgScore = g.Average(x => x.Score),
                        Count = g.Count(),
                        MinScore = g.Min(x => x.Score),
                        MaxScore = g.Max(x => x.Score)
                    })
                    .ToListAsync();

                foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
                {
                    var stats = svVotes.FirstOrDefault(v => v.OptionId == option.Id);
                    if (stats != null)
                    {
                        csv.AppendLine($"\"{option.Label}\",{stats.AvgScore:F2},{stats.Count},{stats.MinScore},{stats.MaxScore}");
                    }
                    else
                    {
                        csv.AppendLine($"\"{option.Label}\",0,0,0,0");
                    }
                }
                break;

            case PollType.AvailabilityGrid:
                csv.AppendLine("Option,Yes,Maybe,No,Total");
                var agVotes = await _context.PollVotes_AvailabilityGrid
                    .Where(v => v.PollId == pollId)
                    .GroupBy(v => new { v.OptionId, v.Availability })
                    .Select(g => new { g.Key.OptionId, g.Key.Availability, Count = g.Count() })
                    .ToListAsync();

                foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
                {
                    var yesCount = agVotes.FirstOrDefault(v => v.OptionId == option.Id && v.Availability == Availability.Yes)?.Count ?? 0;
                    var maybeCount = agVotes.FirstOrDefault(v => v.OptionId == option.Id && v.Availability == Availability.Maybe)?.Count ?? 0;
                    var noCount = agVotes.FirstOrDefault(v => v.OptionId == option.Id && v.Availability == Availability.No)?.Count ?? 0;
                    var total = yesCount + maybeCount + noCount;
                    csv.AppendLine($"\"{option.Label}\",{yesCount},{maybeCount},{noCount},{total}");
                }
                break;
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<bool> CanEditPollAsync(int pollId)
    {
        var voteCount = await GetVoteCountAsync(pollId);
        return voteCount == 0;
    }

    public async Task<int> GetVoteCountAsync(int pollId)
    {
        var poll = await _context.Polls.FindAsync(pollId);
        if (poll == null) return 0;

        return poll.Type switch
        {
            PollType.MultipleChoice => await _context.PollVotes_MultipleChoice.CountAsync(v => v.PollId == pollId),
            PollType.RankedChoice => await _context.PollVotes_RankedChoice.Where(v => v.PollId == pollId).Select(v => v.UserId).Distinct().CountAsync(),
            PollType.ScoreVoting => await _context.PollVotes_ScoreVoting.Where(v => v.PollId == pollId).Select(v => v.UserId).Distinct().CountAsync(),
            PollType.AvailabilityGrid => await _context.PollVotes_AvailabilityGrid.Where(v => v.PollId == pollId).Select(v => v.UserId).Distinct().CountAsync(),
            _ => 0
        };
    }
}
