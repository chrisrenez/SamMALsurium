namespace SamMALsurium.Models.ViewModels;

public class ImageLibraryViewModel
{
    public List<ImageItemViewModel> Images { get; set; } = new List<ImageItemViewModel>();
    public string? ReturnUrl { get; set; }
    public int? ContextId { get; set; }
}
