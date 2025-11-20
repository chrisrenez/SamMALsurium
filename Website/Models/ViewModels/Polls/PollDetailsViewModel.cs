using SamMALsurium.Models;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels.Polls;

public class PollDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollType Type { get; set; }
    public PollTarget Target { get; set; }
    public PollStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAnonymous { get; set; }
    public ResultsVisibility ResultsVisibility { get; set; }
    public bool AllowVoteChange { get; set; }

    // Type-specific configuration
    public bool? IsMultiSelect { get; set; }
    public int? ScoreMin { get; set; }
    public int? ScoreMax { get; set; }

    // Poll options
    public List<PollOptionItem> Options { get; set; } = new();

    // Event association
    public int? EventId { get; set; }
    public string? EventTitle { get; set; }

    // Voting status for current user
    public bool HasUserVoted { get; set; }
    public bool CanVote { get; set; }
    public bool CanChangeVote { get; set; }
    public bool CanViewResults { get; set; }

    // User's existing vote (if they've voted)
    public object? UserVote { get; set; }

    // Participation info
    public int TotalVotes { get; set; }

    public class PollOptionItem
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? AdditionalInfo { get; set; }
        public int DisplayOrder { get; set; }
    }
}
