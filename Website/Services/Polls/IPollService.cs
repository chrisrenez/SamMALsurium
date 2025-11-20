using SamMALsurium.Models;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Services.Polls;

public interface IPollService
{
    Task<Poll> CreatePollAsync(Poll poll);
    Task<Poll?> GetPollByIdAsync(int pollId);
    Task<List<Poll>> GetPollsByEventAsync(int eventId);
    Task<List<Poll>> GetAllPollsAsync(PollStatus? status = null, PollType? type = null, int? eventId = null, DateTime? startDate = null, DateTime? endDate = null, int skip = 0, int take = 50);
    Task<Poll> UpdatePollAsync(Poll poll);
    Task<Poll> ClosePollAsync(int pollId, string userId);
    Task<Poll> ArchivePollAsync(int pollId, string userId);
    Task<Dictionary<string, object>> GetPollStatisticsAsync();
    Task<byte[]> ExportResultsToCsvAsync(int pollId);
    Task<bool> CanEditPollAsync(int pollId);
    Task<int> GetVoteCountAsync(int pollId);
}
