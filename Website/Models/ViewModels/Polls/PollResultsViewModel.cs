using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels.Polls;

public class PollResultsViewModel
{
    public int PollId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollType Type { get; set; }
    public PollStatus Status { get; set; }
    public bool IsAnonymous { get; set; }
    public int TotalParticipants { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Event association
    public int? EventId { get; set; }
    public string? EventTitle { get; set; }

    // Results data - type-specific, stored as Dictionary
    public Dictionary<string, object> Results { get; set; } = new();

    // Poll options for reference
    public List<PollOptionInfo> Options { get; set; } = new();

    // Permissions
    public bool CanViewResults { get; set; }
    public bool CanExportResults { get; set; }

    public class PollOptionInfo
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? AdditionalInfo { get; set; }
        public int DisplayOrder { get; set; }
    }

    // Helper classes for different poll types
    public class MultipleChoiceResult
    {
        public int OptionId { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public int VoteCount { get; set; }
        public decimal Percentage { get; set; }
        public List<string>? VoterNames { get; set; } // Only populated for non-anonymous polls
    }

    public class RankedChoiceResult
    {
        public int OptionId { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public decimal AverageRank { get; set; }
        public int TotalRankings { get; set; }
        public Dictionary<int, int>? RankCounts { get; set; } // Rank -> Count
        public List<RankingDetail>? VoterRankings { get; set; } // Only for non-anonymous
    }

    public class RankingDetail
    {
        public string VoterName { get; set; } = string.Empty;
        public int Rank { get; set; }
    }

    public class ScoreVotingResult
    {
        public int OptionId { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal? StandardDeviation { get; set; }
        public int TotalScores { get; set; }
        public Dictionary<int, int>? ScoreDistribution { get; set; } // Score -> Count
        public List<ScoreDetail>? VoterScores { get; set; } // Only for non-anonymous
    }

    public class ScoreDetail
    {
        public string VoterName { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    public class AvailabilityGridResult
    {
        public int OptionId { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public int YesCount { get; set; }
        public int MaybeCount { get; set; }
        public int NoCount { get; set; }
        public int TotalResponses { get; set; }
        public List<AvailabilityDetail>? VoterAvailability { get; set; } // Only for non-anonymous
    }

    public class AvailabilityDetail
    {
        public string VoterName { get; set; } = string.Empty;
        public Availability Availability { get; set; }
    }
}
