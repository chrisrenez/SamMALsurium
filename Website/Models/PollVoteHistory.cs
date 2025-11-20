using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SamMALsurium.Models;

public class PollVoteHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PollId { get; set; }

    [ForeignKey(nameof(PollId))]
    public Poll? Poll { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(5000)]
    public string PreviousVoteJson { get; set; } = string.Empty;  // JSON representation of previous vote

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
