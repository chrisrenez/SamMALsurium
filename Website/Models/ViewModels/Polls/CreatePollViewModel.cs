using System.ComponentModel.DataAnnotations;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels.Polls;

public class CreatePollViewModel
{
    [Required(ErrorMessage = "Titel ist erforderlich")]
    [StringLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Beschreibung darf maximal 2000 Zeichen lang sein")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Umfragetyp ist erforderlich")]
    public PollType Type { get; set; }

    [Required(ErrorMessage = "Ziel ist erforderlich")]
    public PollTarget Target { get; set; }

    public int? EventId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    // Privacy settings
    public bool IsAnonymous { get; set; } = false;

    public ResultsVisibility ResultsVisibility { get; set; } = ResultsVisibility.AfterVoting;

    public bool AllowVoteChange { get; set; } = true;

    // Type-specific configuration
    public bool IsMultiSelect { get; set; } = true;

    public int ScoreMin { get; set; } = 1;

    public int ScoreMax { get; set; } = 5;

    // Poll options
    public List<PollOptionInput> Options { get; set; } = new();
}

public class PollOptionInput
{
    [Required(ErrorMessage = "Option ist erforderlich")]
    [StringLength(200, ErrorMessage = "Option darf maximal 200 Zeichen lang sein")]
    public string Label { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Zusatzinfo darf maximal 500 Zeichen lang sein")]
    public string? AdditionalInfo { get; set; }

    public int DisplayOrder { get; set; }
}
