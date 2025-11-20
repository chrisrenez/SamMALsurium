namespace SamMALsurium.Services;

public interface IEmailService
{
    Task SendAdminNewUserNotificationAsync(string adminEmail, string newUserFirstName, string newUserLastName, string newUserEmail);

    Task SendUserRejectionAsync(string userEmail, string userFirstName, string? rejectionMessage);

    Task SendSetPasswordEmailAsync(string userEmail, string userFirstName, string passwordResetToken, string userId);

    Task SendAccountReactivatedAsync(string userEmail, string userFirstName);

    Task SendPollCreatedNotificationAsync(string userEmail, string userName, string pollTitle, string? pollDescription, string? eventTitle, string voteUrl);

    Task SendPollClosingSoonNotificationAsync(string userEmail, string userName, string pollTitle, string? pollDescription, string? eventTitle, string endDate, string voteUrl);

    Task SendPollResultsAvailableNotificationAsync(string userEmail, string userName, string pollTitle, string? pollDescription, string? eventTitle, string resultsUrl);
}
