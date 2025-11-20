using System.ComponentModel.DataAnnotations;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels;

public class RsvpViewModel
{
    [Required]
    public int EventId { get; set; }

    [Required(ErrorMessage = "Bitte wählen Sie eine RSVP-Option.")]
    public RsvpStatus RsvpStatus { get; set; }
}
