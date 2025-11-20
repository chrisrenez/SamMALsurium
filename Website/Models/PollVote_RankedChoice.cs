using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SamMALsurium.Models;

public class PollVote_RankedChoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PollId { get; set; }

    [ForeignKey(nameof(PollId))]
    public Poll? Poll { get; set; }

    [Required]
    public int OptionId { get; set; }

    [ForeignKey(nameof(OptionId))]
    public PollOption? Option { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Required]
    public int Rank { get; set; }  // 1 = top choice

    [Required]
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}
