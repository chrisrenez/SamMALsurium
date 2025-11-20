using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SamMALsurium.Data;
using SamMALsurium.Models;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Services.Polls;

public class VoteService : IVoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VoteService> _logger;

    public VoteService(ApplicationDbContext context, ILogger<VoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CastVoteAsync(int pollId, string userId, object voteData)
    {
        var poll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll == null)
        {
            throw new ArgumentException($"Poll with ID {pollId} not found");
        }

        // Validate poll status
        if (poll.Status != PollStatus.Active)
        {
            throw new InvalidOperationException($"Poll is {poll.Status.ToString().ToLower()} and cannot accept votes");
        }

        // Check if poll hasn't started yet
        if (poll.StartDate.HasValue && poll.StartDate.Value > DateTime.UtcNow)
        {
            throw new InvalidOperationException("Poll has not started yet");
        }

        // Check if poll has ended
        if (poll.EndDate.HasValue && poll.EndDate.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Poll has ended");
        }

        // Check if user has already voted
        var hasVoted = await HasUserVotedAsync(pollId, userId);
        if (hasVoted)
        {
            throw new InvalidOperationException("User has already voted in this poll");
        }

        // Validate vote data
        if (!ValidateVote(poll, voteData))
        {
            throw new ArgumentException("Invalid vote data");
        }

        // Cast vote based on poll type
        switch (poll.Type)
        {
            case PollType.MultipleChoice:
                await CastMultipleChoiceVoteAsync(poll, userId, voteData);
                break;
            case PollType.RankedChoice:
                await CastRankedChoiceVoteAsync(poll, userId, voteData);
                break;
            case PollType.ScoreVoting:
                await CastScoreVotingVoteAsync(poll, userId, voteData);
                break;
            case PollType.AvailabilityGrid:
                await CastAvailabilityGridVoteAsync(poll, userId, voteData);
                break;
            default:
                throw new NotSupportedException($"Poll type {poll.Type} is not supported");
        }

        _logger.LogInformation("Vote cast for poll {PollId} by user {UserId}", pollId, userId);
    }

    public async Task ChangeVoteAsync(int pollId, string userId, object voteData)
    {
        var poll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll == null)
        {
            throw new ArgumentException($"Poll with ID {pollId} not found");
        }

        if (!poll.AllowVoteChange)
        {
            throw new InvalidOperationException("Vote changes are not allowed for this poll");
        }

        // Validate poll status
        if (poll.Status != PollStatus.Active)
        {
            throw new InvalidOperationException($"Poll is {poll.Status.ToString().ToLower()} and cannot accept vote changes");
        }

        // Get existing vote and save to history
        var existingVote = await GetUserVoteAsync(pollId, userId);
        if (existingVote == null)
        {
            throw new InvalidOperationException("User has not voted in this poll yet");
        }

        // Save to history
        var history = new PollVoteHistory
        {
            PollId = pollId,
            UserId = userId,
            PreviousVoteJson = JsonSerializer.Serialize(existingVote),
            ChangedAt = DateTime.UtcNow
        };
        _context.PollVoteHistories.Add(history);

        // Remove existing vote
        await WithdrawVoteInternalAsync(pollId, userId, poll.Type);

        // Validate new vote data
        if (!ValidateVote(poll, voteData))
        {
            throw new ArgumentException("Invalid vote data");
        }

        // Cast new vote
        switch (poll.Type)
        {
            case PollType.MultipleChoice:
                await CastMultipleChoiceVoteAsync(poll, userId, voteData);
                break;
            case PollType.RankedChoice:
                await CastRankedChoiceVoteAsync(poll, userId, voteData);
                break;
            case PollType.ScoreVoting:
                await CastScoreVotingVoteAsync(poll, userId, voteData);
                break;
            case PollType.AvailabilityGrid:
                await CastAvailabilityGridVoteAsync(poll, userId, voteData);
                break;
        }

        _logger.LogInformation("Vote changed for poll {PollId} by user {UserId}", pollId, userId);
    }

    public async Task WithdrawVoteAsync(int pollId, string userId)
    {
        var poll = await _context.Polls.FindAsync(pollId);
        if (poll == null)
        {
            throw new ArgumentException($"Poll with ID {pollId} not found");
        }

        if (!poll.AllowVoteChange)
        {
            throw new InvalidOperationException("Vote withdrawal is not allowed for this poll");
        }

        await WithdrawVoteInternalAsync(pollId, userId, poll.Type);
        _logger.LogInformation("Vote withdrawn for poll {PollId} by user {UserId}", pollId, userId);
    }

    private async Task WithdrawVoteInternalAsync(int pollId, string userId, PollType pollType)
    {
        switch (pollType)
        {
            case PollType.MultipleChoice:
                var mcVotes = await _context.PollVotes_MultipleChoice
                    .Where(v => v.PollId == pollId && v.UserId == userId)
                    .ToListAsync();
                _context.PollVotes_MultipleChoice.RemoveRange(mcVotes);
                break;

            case PollType.RankedChoice:
                var rcVotes = await _context.PollVotes_RankedChoice
                    .Where(v => v.PollId == pollId && v.UserId == userId)
                    .ToListAsync();
                _context.PollVotes_RankedChoice.RemoveRange(rcVotes);
                break;

            case PollType.ScoreVoting:
                var svVotes = await _context.PollVotes_ScoreVoting
                    .Where(v => v.PollId == pollId && v.UserId == userId)
                    .ToListAsync();
                _context.PollVotes_ScoreVoting.RemoveRange(svVotes);
                break;

            case PollType.AvailabilityGrid:
                var agVotes = await _context.PollVotes_AvailabilityGrid
                    .Where(v => v.PollId == pollId && v.UserId == userId)
                    .ToListAsync();
                _context.PollVotes_AvailabilityGrid.RemoveRange(agVotes);
                break;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<object?> GetUserVoteAsync(int pollId, string userId)
    {
        var poll = await _context.Polls.FindAsync(pollId);
        if (poll == null) return null;

        return poll.Type switch
        {
            PollType.MultipleChoice => await _context.PollVotes_MultipleChoice
                .Where(v => v.PollId == pollId && v.UserId == userId)
                .Select(v => new { v.OptionId, v.VotedAt })
                .ToListAsync(),

            PollType.RankedChoice => await _context.PollVotes_RankedChoice
                .Where(v => v.PollId == pollId && v.UserId == userId)
                .OrderBy(v => v.Rank)
                .Select(v => new { v.OptionId, v.Rank, v.VotedAt })
                .ToListAsync(),

            PollType.ScoreVoting => await _context.PollVotes_ScoreVoting
                .Where(v => v.PollId == pollId && v.UserId == userId)
                .Select(v => new { v.OptionId, v.Score, v.VotedAt })
                .ToListAsync(),

            PollType.AvailabilityGrid => await _context.PollVotes_AvailabilityGrid
                .Where(v => v.PollId == pollId && v.UserId == userId)
                .Select(v => new { v.OptionId, v.Availability, v.VotedAt })
                .ToListAsync(),

            _ => null
        };
    }

    public async Task<bool> HasUserVotedAsync(int pollId, string userId)
    {
        var poll = await _context.Polls.FindAsync(pollId);
        if (poll == null) return false;

        return poll.Type switch
        {
            PollType.MultipleChoice => await _context.PollVotes_MultipleChoice
                .AnyAsync(v => v.PollId == pollId && v.UserId == userId),

            PollType.RankedChoice => await _context.PollVotes_RankedChoice
                .AnyAsync(v => v.PollId == pollId && v.UserId == userId),

            PollType.ScoreVoting => await _context.PollVotes_ScoreVoting
                .AnyAsync(v => v.PollId == pollId && v.UserId == userId),

            PollType.AvailabilityGrid => await _context.PollVotes_AvailabilityGrid
                .AnyAsync(v => v.PollId == pollId && v.UserId == userId),

            _ => false
        };
    }

    public async Task<Dictionary<string, object>> GetPollResultsAsync(int pollId, string? requestingUserId = null)
    {
        var poll = await _context.Polls
            .Include(p => p.Options)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll == null)
        {
            throw new ArgumentException($"Poll with ID {pollId} not found");
        }

        // Check visibility permissions
        var canViewResults = CanViewResults(poll, requestingUserId);
        if (!canViewResults)
        {
            throw new UnauthorizedAccessException("You do not have permission to view these results");
        }

        var results = new Dictionary<string, object>
        {
            { "PollId", pollId },
            { "Title", poll.Title },
            { "Type", poll.Type.ToString() },
            { "Status", poll.Status.ToString() },
            { "IsAnonymous", poll.IsAnonymous }
        };

        switch (poll.Type)
        {
            case PollType.MultipleChoice:
                results["Results"] = await GetMultipleChoiceResultsAsync(poll);
                break;
            case PollType.RankedChoice:
                results["Results"] = await GetRankedChoiceResultsAsync(poll);
                break;
            case PollType.ScoreVoting:
                results["Results"] = await GetScoreVotingResultsAsync(poll);
                break;
            case PollType.AvailabilityGrid:
                results["Results"] = await GetAvailabilityGridResultsAsync(poll);
                break;
        }

        // Add total participant count
        var participantCount = await GetParticipantCountAsync(pollId, poll.Type);
        results["TotalParticipants"] = participantCount;

        return results;
    }

    private bool CanViewResults(Poll poll, string? requestingUserId)
    {
        return poll.ResultsVisibility switch
        {
            ResultsVisibility.RealTime => true,
            ResultsVisibility.AfterVoting => poll.Status == PollStatus.Closed || poll.Status == PollStatus.Archived,
            ResultsVisibility.CreatorOnly => requestingUserId == poll.CreatedById,
            _ => false
        };
    }

    private async Task<int> GetParticipantCountAsync(int pollId, PollType pollType)
    {
        return pollType switch
        {
            PollType.MultipleChoice => await _context.PollVotes_MultipleChoice
                .Where(v => v.PollId == pollId)
                .Select(v => v.UserId)
                .Distinct()
                .CountAsync(),

            PollType.RankedChoice => await _context.PollVotes_RankedChoice
                .Where(v => v.PollId == pollId)
                .Select(v => v.UserId)
                .Distinct()
                .CountAsync(),

            PollType.ScoreVoting => await _context.PollVotes_ScoreVoting
                .Where(v => v.PollId == pollId)
                .Select(v => v.UserId)
                .Distinct()
                .CountAsync(),

            PollType.AvailabilityGrid => await _context.PollVotes_AvailabilityGrid
                .Where(v => v.PollId == pollId)
                .Select(v => v.UserId)
                .Distinct()
                .CountAsync(),

            _ => 0
        };
    }

    private async Task<List<object>> GetMultipleChoiceResultsAsync(Poll poll)
    {
        var votes = await _context.PollVotes_MultipleChoice
            .Where(v => v.PollId == poll.Id)
            .Include(v => v.User)
            .ToListAsync();

        var totalVotes = votes.Count;
        var results = new List<object>();

        foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
        {
            var optionVotes = votes.Where(v => v.OptionId == option.Id).ToList();
            var voteCount = optionVotes.Count;
            var percentage = totalVotes > 0 ? (voteCount * 100.0 / totalVotes) : 0;

            var result = new Dictionary<string, object>
            {
                { "OptionId", option.Id },
                { "Label", option.Label },
                { "VoteCount", voteCount },
                { "Percentage", percentage }
            };

            if (!poll.IsAnonymous)
            {
                result["Voters"] = optionVotes.Select(v => new
                {
                    UserId = v.UserId,
                    Name = $"{v.User?.FirstName} {v.User?.LastName}".Trim(),
                    VotedAt = v.VotedAt
                }).ToList();
            }

            results.Add(result);
        }

        return results;
    }

    private async Task<List<object>> GetRankedChoiceResultsAsync(Poll poll)
    {
        var votes = await _context.PollVotes_RankedChoice
            .Where(v => v.PollId == poll.Id)
            .Include(v => v.User)
            .ToListAsync();

        var results = new List<object>();

        foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
        {
            var optionVotes = votes.Where(v => v.OptionId == option.Id).ToList();
            var rankCount = optionVotes.Count;
            var avgRank = rankCount > 0 ? optionVotes.Average(v => v.Rank) : 0;

            var result = new Dictionary<string, object>
            {
                { "OptionId", option.Id },
                { "Label", option.Label },
                { "AverageRank", avgRank },
                { "TotalRankings", rankCount }
            };

            if (!poll.IsAnonymous)
            {
                result["Voters"] = optionVotes.Select(v => new
                {
                    UserId = v.UserId,
                    Name = $"{v.User?.FirstName} {v.User?.LastName}".Trim(),
                    Rank = v.Rank,
                    VotedAt = v.VotedAt
                }).OrderBy(v => v.Rank).ToList();
            }

            results.Add(result);
        }

        return results;
    }

    private async Task<List<object>> GetScoreVotingResultsAsync(Poll poll)
    {
        var votes = await _context.PollVotes_ScoreVoting
            .Where(v => v.PollId == poll.Id)
            .Include(v => v.User)
            .ToListAsync();

        var results = new List<object>();

        foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
        {
            var optionVotes = votes.Where(v => v.OptionId == option.Id).ToList();
            var scoreCount = optionVotes.Count;
            var avgScore = scoreCount > 0 ? optionVotes.Average(v => v.Score) : 0;
            var stdDev = scoreCount > 1 ? CalculateStandardDeviation(optionVotes.Select(v => (double)v.Score)) : 0;

            var result = new Dictionary<string, object>
            {
                { "OptionId", option.Id },
                { "Label", option.Label },
                { "AverageScore", avgScore },
                { "StandardDeviation", stdDev },
                { "TotalScores", scoreCount }
            };

            if (!poll.IsAnonymous)
            {
                result["Voters"] = optionVotes.Select(v => new
                {
                    UserId = v.UserId,
                    Name = $"{v.User?.FirstName} {v.User?.LastName}".Trim(),
                    Score = v.Score,
                    VotedAt = v.VotedAt
                }).ToList();
            }

            results.Add(result);
        }

        return results;
    }

    private async Task<List<object>> GetAvailabilityGridResultsAsync(Poll poll)
    {
        var votes = await _context.PollVotes_AvailabilityGrid
            .Where(v => v.PollId == poll.Id)
            .Include(v => v.User)
            .ToListAsync();

        var results = new List<object>();

        foreach (var option in poll.Options.OrderBy(o => o.DisplayOrder))
        {
            var optionVotes = votes.Where(v => v.OptionId == option.Id).ToList();
            var yesCount = optionVotes.Count(v => v.Availability == Availability.Yes);
            var maybeCount = optionVotes.Count(v => v.Availability == Availability.Maybe);
            var noCount = optionVotes.Count(v => v.Availability == Availability.No);

            var result = new Dictionary<string, object>
            {
                { "OptionId", option.Id },
                { "Label", option.Label },
                { "YesCount", yesCount },
                { "MaybeCount", maybeCount },
                { "NoCount", noCount },
                { "TotalResponses", optionVotes.Count }
            };

            if (!poll.IsAnonymous)
            {
                result["Voters"] = optionVotes.Select(v => new
                {
                    UserId = v.UserId,
                    Name = $"{v.User?.FirstName} {v.User?.LastName}".Trim(),
                    Availability = v.Availability.ToString(),
                    VotedAt = v.VotedAt
                }).ToList();
            }

            results.Add(result);
        }

        return results;
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (valueList.Count <= 1) return 0;

        var avg = valueList.Average();
        var sumOfSquares = valueList.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumOfSquares / (valueList.Count - 1));
    }

    public bool ValidateVote(Poll poll, object voteData)
    {
        return poll.Type switch
        {
            PollType.MultipleChoice => ValidateMultipleChoiceVote(poll, voteData),
            PollType.RankedChoice => ValidateRankedChoiceVote(poll, voteData),
            PollType.ScoreVoting => ValidateScoreVotingVote(poll, voteData),
            PollType.AvailabilityGrid => ValidateAvailabilityGridVote(poll, voteData),
            _ => false
        };
    }

    private bool ValidateMultipleChoiceVote(Poll poll, object voteData)
    {
        if (voteData is not List<int> optionIds)
        {
            return false;
        }

        if (optionIds.Count == 0)
        {
            return false;
        }

        // Check if single-select and more than one option selected
        if (!poll.IsMultiSelect && optionIds.Count > 1)
        {
            return false;
        }

        // Verify all option IDs belong to this poll
        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        return optionIds.All(id => validOptionIds.Contains(id));
    }

    private bool ValidateRankedChoiceVote(Poll poll, object voteData)
    {
        if (voteData is not Dictionary<int, int> rankings)
        {
            return false;
        }

        if (rankings.Count == 0)
        {
            return false;
        }

        // Verify all option IDs belong to this poll
        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        if (!rankings.Keys.All(id => validOptionIds.Contains(id)))
        {
            return false;
        }

        // Verify ranks are unique and start from 1
        var ranks = rankings.Values.OrderBy(r => r).ToList();
        for (int i = 0; i < ranks.Count; i++)
        {
            if (ranks[i] != i + 1)
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateScoreVotingVote(Poll poll, object voteData)
    {
        if (voteData is not Dictionary<int, int> scores)
        {
            return false;
        }

        if (scores.Count == 0)
        {
            return false;
        }

        // Verify all option IDs belong to this poll
        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        if (!scores.Keys.All(id => validOptionIds.Contains(id)))
        {
            return false;
        }

        // Verify scores are within range
        return scores.Values.All(score => score >= poll.ScoreMin && score <= poll.ScoreMax);
    }

    private bool ValidateAvailabilityGridVote(Poll poll, object voteData)
    {
        if (voteData is not Dictionary<int, Availability> availabilities)
        {
            return false;
        }

        if (availabilities.Count == 0)
        {
            return false;
        }

        // Verify all option IDs belong to this poll
        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        return availabilities.Keys.All(id => validOptionIds.Contains(id));
    }

    private async Task CastMultipleChoiceVoteAsync(Poll poll, string userId, object voteData)
    {
        var optionIds = (List<int>)voteData;
        var now = DateTime.UtcNow;

        foreach (var optionId in optionIds)
        {
            var vote = new PollVote_MultipleChoice
            {
                PollId = poll.Id,
                OptionId = optionId,
                UserId = userId,
                VotedAt = now
            };
            _context.PollVotes_MultipleChoice.Add(vote);
        }

        await _context.SaveChangesAsync();
    }

    private async Task CastRankedChoiceVoteAsync(Poll poll, string userId, object voteData)
    {
        var rankings = (Dictionary<int, int>)voteData;
        var now = DateTime.UtcNow;

        foreach (var (optionId, rank) in rankings)
        {
            var vote = new PollVote_RankedChoice
            {
                PollId = poll.Id,
                OptionId = optionId,
                UserId = userId,
                Rank = rank,
                VotedAt = now
            };
            _context.PollVotes_RankedChoice.Add(vote);
        }

        await _context.SaveChangesAsync();
    }

    private async Task CastScoreVotingVoteAsync(Poll poll, string userId, object voteData)
    {
        var scores = (Dictionary<int, int>)voteData;
        var now = DateTime.UtcNow;

        foreach (var (optionId, score) in scores)
        {
            var vote = new PollVote_ScoreVoting
            {
                PollId = poll.Id,
                OptionId = optionId,
                UserId = userId,
                Score = score,
                VotedAt = now
            };
            _context.PollVotes_ScoreVoting.Add(vote);
        }

        await _context.SaveChangesAsync();
    }

    private async Task CastAvailabilityGridVoteAsync(Poll poll, string userId, object voteData)
    {
        var availabilities = (Dictionary<int, Availability>)voteData;
        var now = DateTime.UtcNow;

        foreach (var (optionId, availability) in availabilities)
        {
            var vote = new PollVote_AvailabilityGrid
            {
                PollId = poll.Id,
                OptionId = optionId,
                UserId = userId,
                Availability = availability,
                VotedAt = now
            };
            _context.PollVotes_AvailabilityGrid.Add(vote);
        }

        await _context.SaveChangesAsync();
    }
}
