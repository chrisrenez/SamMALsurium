using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models;

public class Poll
{
    [Key]
    public int Id { get; set; }

    // Optional association with Event or Forum Thread
    public int? EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    public int? ThreadId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [Required]
    public PollType Type { get; set; }

    [Required]
    public PollTarget Target { get; set; } = PollTarget.Other;

    [Required]
    public PollStatus Status { get; set; } = PollStatus.Active;

    // Type-specific configuration
    public bool IsMultiSelect { get; set; } = true;  // For Multiple Choice
    public int ScoreMin { get; set; } = 1;  // For Score Voting
    public int ScoreMax { get; set; } = 5;  // For Score Voting

    // Privacy settings
    public bool IsAnonymous { get; set; } = false;

    [Required]
    public ResultsVisibility ResultsVisibility { get; set; } = ResultsVisibility.AfterVoting;

    public bool AllowVoteChange { get; set; } = true;

    // Scheduling
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedById { get; set; } = string.Empty;

    [ForeignKey(nameof(CreatedById))]
    public ApplicationUser? CreatedBy { get; set; }

    // Navigation properties
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
}
