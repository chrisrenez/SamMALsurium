using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SamMALsurium.Models.Enums;

namespace SamMALsurium.Models.ViewModels.Admin;

public class EventEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titel ist erforderlich.")]
    [StringLength(200, ErrorMessage = "Titel darf maximal 200 Zeichen lang sein.")]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Beschreibung")]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Startdatum ist erforderlich.")]
    [Display(Name = "Startdatum")]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Enddatum ist erforderlich.")]
    [Display(Name = "Enddatum")]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; }

    [StringLength(500, ErrorMessage = "Ort darf maximal 500 Zeichen lang sein.")]
    [Display(Name = "Ort")]
    public string? Location { get; set; }

    [StringLength(200, ErrorMessage = "Ortsname darf maximal 200 Zeichen lang sein.")]
    [Display(Name = "Ortsname")]
    public string? LocationName { get; set; }

    [Display(Name = "Breitengrad")]
    [Range(-90, 90, ErrorMessage = "Breitengrad muss zwischen -90 und 90 liegen.")]
    public double? Latitude { get; set; }

    [Display(Name = "Längengrad")]
    [Range(-180, 180, ErrorMessage = "Längengrad muss zwischen -180 und 180 liegen.")]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Event-Typ ist erforderlich.")]
    [Display(Name = "Event-Typ")]
    public int EventTypeId { get; set; }

    [Display(Name = "Öffentlich sichtbar")]
    public bool IsPublic { get; set; }

    [Display(Name = "RSVP aktivieren")]
    public bool RsvpEnabled { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; }

    [Display(Name = "Neues Cover-Bild")]
    public IFormFile? CoverImage { get; set; }

    public string? ExistingCoverImagePath { get; set; }

    [Display(Name = "Neue Galerie-Bilder")]
    public List<IFormFile>? GalleryImages { get; set; }

    public List<EventMedia>? ExistingMedia { get; set; }

    [Display(Name = "Externe Links (ein Link pro Zeile)")]
    [DataType(DataType.MultilineText)]
    public string? ExternalLinks { get; set; }

    [Display(Name = "Neue Dateien anhängen")]
    public List<IFormFile>? Attachments { get; set; }

    public List<int>? MediaToDelete { get; set; }
}
