namespace SamMALsurium.Services;

public interface IEmailService
{
    Task SendAdminNewUserNotificationAsync(string adminEmail, string newUserFirstName, string newUserLastName, string newUserEmail);

    Task SendUserRejectionAsync(string userEmail, string userFirstName, string? rejectionMessage);

    Task SendSetPasswordEmailAsync(string userEmail, string userFirstName, string passwordResetToken, string userId);

    Task SendAccountReactivatedAsync(string userEmail, string userFirstName);
}
