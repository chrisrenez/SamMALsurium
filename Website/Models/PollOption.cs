using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SamMALsurium.Models;

public class PollOption
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PollId { get; set; }

    [ForeignKey(nameof(PollId))]
    public Poll? Poll { get; set; }

    [Required]
    [MaxLength(500)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? AdditionalInfo { get; set; }

    [Required]
    public int DisplayOrder { get; set; }
}
