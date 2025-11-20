using SamMALsurium.Models;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Services.Polls;

public interface IVoteService
{
    Task CastVoteAsync(int pollId, string userId, object voteData);
    Task ChangeVoteAsync(int pollId, string userId, object voteData);
    Task WithdrawVoteAsync(int pollId, string userId);
    Task<object?> GetUserVoteAsync(int pollId, string userId);
    Task<bool> HasUserVotedAsync(int pollId, string userId);
    Task<Dictionary<string, object>> GetPollResultsAsync(int pollId, string? requestingUserId = null);
    bool ValidateVote(Poll poll, object voteData);
}
