namespace SamMALsurium.Models.ViewModels.Polls;

public class PollStatisticsViewModel
{
    public int TotalPolls { get; set; }
    public int ActivePolls { get; set; }
    public int ClosedPolls { get; set; }
    public int ArchivedPolls { get; set; }
    public int TotalVotes { get; set; }
    public List<MostActivePoll> MostActivePolls { get; set; } = new();
}

public class MostActivePoll
{
    public int PollId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int VoteCount { get; set; }
}
