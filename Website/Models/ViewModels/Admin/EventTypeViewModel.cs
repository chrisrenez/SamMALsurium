using System.ComponentModel.DataAnnotations;

namespace SamMALsurium.Models.ViewModels.Admin;

public class EventTypeViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(100, ErrorMessage = "Name darf maximal 100 Zeichen lang sein.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Beschreibung darf maximal 500 Zeichen lang sein.")]
    [Display(Name = "Beschreibung")]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }
}
