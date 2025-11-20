using System.ComponentModel.DataAnnotations;

namespace SamMALsurium.Models.ViewModels.Admin;

public class EventAnnouncementViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nachricht ist erforderlich.")]
    [Display(Name = "Nachricht")]
    [DataType(DataType.MultilineText)]
    [StringLength(5000, ErrorMessage = "Nachricht darf maximal 5000 Zeichen lang sein.")]
    public string Message { get; set; } = string.Empty;

    public int AttendeeCount { get; set; }
}
