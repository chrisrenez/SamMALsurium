using System.ComponentModel.DataAnnotations;

namespace SamMALsurium.Models.ViewModels;

public class NotificationPreferencesViewModel
{
    [Display(Name = "Umfrage-Benachrichtigungen aktivieren")]
    public bool EnablePollNotifications { get; set; } = true;
}
