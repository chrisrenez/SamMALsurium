using System.ComponentModel.DataAnnotations;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels.Polls;

public class EditPollViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titel ist erforderlich")]
    [StringLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Beschreibung darf maximal 2000 Zeichen lang sein")]
    public string? Description { get; set; }

    public PollType Type { get; set; }

    public PollTarget Target { get; set; }

    public int? EventId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    // Privacy settings
    public bool IsAnonymous { get; set; }

    public ResultsVisibility ResultsVisibility { get; set; }

    public bool AllowVoteChange { get; set; }

    // Type-specific configuration
    public bool IsMultiSelect { get; set; }

    public int ScoreMin { get; set; }

    public int ScoreMax { get; set; }

    // Poll options
    public List<PollOptionInput> Options { get; set; } = new();

    // Metadata
    public bool HasVotes { get; set; }
    public int VoteCount { get; set; }
}
