using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels.Polls;

public class PollListViewModel
{
    public List<PollSummary> Polls { get; set; } = new();
    public FilterOptions Filters { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
    public int TotalCount { get; set; }
}

public class PollSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public PollType Type { get; set; }
    public PollStatus Status { get; set; }
    public string? EventTitle { get; set; }
    public int? EventId { get; set; }
    public int VoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
}

public class FilterOptions
{
    public PollStatus? Status { get; set; }
    public PollType? Type { get; set; }
    public int? EventId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
}

public class PaginationInfo
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
