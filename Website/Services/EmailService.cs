using System.Text;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using MimeKit;
using SamMALsurium.Models.Configuration;

namespace SamMALsurium.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ApplicationSettings _applicationSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        IOptions<ApplicationSettings> applicationSettings,
        ILogger<EmailService> logger,
        IRazorViewEngine razorViewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider)
    {
        _emailSettings = emailSettings.Value;
        _applicationSettings = applicationSettings.Value;
        _logger = logger;
        _razorViewEngine = razorViewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task SendAdminNewUserNotificationAsync(string adminEmail, string newUserFirstName, string newUserLastName, string newUserEmail)
    {
        var model = new
        {
            NewUserFirstName = newUserFirstName,
            NewUserLastName = newUserLastName,
            NewUserEmail = newUserEmail
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/AdminNewUserNotification.cshtml", model);

        await SendEmailAsync(
            adminEmail,
            "Neue Mitgliederanfrage",
            htmlBody
        );
    }

    public async Task SendUserRejectionAsync(string userEmail, string userFirstName, string? rejectionMessage)
    {
        var model = new
        {
            UserFirstName = userFirstName,
            RejectionMessage = rejectionMessage
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/UserRejectionNotification.cshtml", model);

        await SendEmailAsync(
            userEmail,
            "SamMALsurium - Mitgliederanfrage",
            htmlBody
        );
    }

    public async Task SendSetPasswordEmailAsync(string userEmail, string userFirstName, string passwordResetToken, string userId)
    {
        var model = new
        {
            UserFirstName = userFirstName,
            PasswordResetToken = passwordResetToken,
            UserId = userId,
            BaseUrl = _applicationSettings.BaseUrl
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/SetPasswordEmail.cshtml", model);

        await SendEmailAsync(
            userEmail,
            "Willkommen bei SamMALsurium - Passwort festlegen",
            htmlBody
        );
    }

    public async Task SendAccountReactivatedAsync(string userEmail, string userFirstName)
    {
        var model = new
        {
            UserFirstName = userFirstName,
            BaseUrl = _applicationSettings.BaseUrl
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/AccountReactivated.cshtml", model);

        await SendEmailAsync(
            userEmail,
            "Ihr SamMALsurium-Konto wurde reaktiviert",
            htmlBody
        );
    }

    public async Task SendPollCreatedNotificationAsync(string userEmail, string userName, string pollTitle, string? pollDescription, string? eventTitle, string voteUrl)
    {
        var model = new
        {
            UserName = userName,
            PollTitle = pollTitle,
            PollDescription = pollDescription,
            EventTitle = eventTitle,
            VoteUrl = _applicationSettings.BaseUrl + voteUrl,
            BaseUrl = _applicationSettings.BaseUrl
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/PollCreated.cshtml", model);

        await SendEmailAsync(
            userEmail,
            "Neue Umfrage verfügbar",
            htmlBody
        );
    }

    public async Task SendPollClosingSoonNotificationAsync(string userEmail, string userName, string pollTitle, string? pollDescription, string? eventTitle, string endDate, string voteUrl)
    {
        var model = new
        {
            UserName = userName,
            PollTitle = pollTitle,
            PollDescription = pollDescription,
            EventTitle = eventTitle,
            EndDate = endDate,
            VoteUrl = _applicationSettings.BaseUrl + voteUrl,
            BaseUrl = _applicationSettings.BaseUrl
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/PollClosingSoon.cshtml", model);

        await SendEmailAsync(
            userEmail,
            "Umfrage endet bald",
            htmlBody
        );
    }

    public async Task SendPollResultsAvailableNotificationAsync(string userEmail, string userName, string pollTitle, string? pollDescription, string? eventTitle, string resultsUrl)
    {
        var model = new
        {
            UserName = userName,
            PollTitle = pollTitle,
            PollDescription = pollDescription,
            EventTitle = eventTitle,
            ResultsUrl = _applicationSettings.BaseUrl + resultsUrl,
            BaseUrl = _applicationSettings.BaseUrl
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/PollResultsAvailable.cshtml", model);

        await SendEmailAsync(
            userEmail,
            "Umfrageergebnisse verfügbar",
            htmlBody
        );
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = ConvertHtmlToPlainText(htmlBody)
        };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject '{Subject}'", toEmail, subject);

            if (_emailSettings.SaveEmailsToFileOnFailure)
            {
                await SaveEmailToFileAsync(message);
            }
            else
            {
                throw;
            }
        }
    }

    private async Task SaveEmailToFileAsync(MimeMessage message)
    {
        try
        {
            var saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "SavedEmails");
            Directory.CreateDirectory(saveDirectory);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var recipient = message.To.Mailboxes.FirstOrDefault()?.Address ?? "unknown";
            var sanitizedSubject = SanitizeFileName(message.Subject);
            var baseFileName = $"{timestamp}_{recipient}_{sanitizedSubject}";

            var htmlFilePath = Path.Combine(saveDirectory, $"{baseFileName}.html");
            var textFilePath = Path.Combine(saveDirectory, $"{baseFileName}.txt");

            var metadata = BuildEmailMetadata(message);

            var htmlContent = new StringBuilder();
            htmlContent.AppendLine("<!--");
            htmlContent.AppendLine(metadata);
            htmlContent.AppendLine("-->");
            htmlContent.AppendLine();
            htmlContent.AppendLine(GetHtmlBody(message));

            await File.WriteAllTextAsync(htmlFilePath, htmlContent.ToString());

            var textContent = new StringBuilder();
            textContent.AppendLine(metadata);
            textContent.AppendLine();
            textContent.AppendLine(new string('=', 80));
            textContent.AppendLine();
            textContent.AppendLine(GetTextBody(message));

            await File.WriteAllTextAsync(textFilePath, textContent.ToString());

            _logger.LogInformation(
                "Email saved to filesystem: {HtmlPath} and {TextPath}",
                htmlFilePath,
                textFilePath
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email to filesystem");
            throw;
        }
    }

    private string BuildEmailMetadata(MimeMessage message)
    {
        var metadata = new StringBuilder();
        metadata.AppendLine("EMAIL METADATA");
        metadata.AppendLine(new string('=', 80));
        metadata.AppendLine($"Date: {message.Date:yyyy-MM-dd HH:mm:ss}");
        metadata.AppendLine($"From: {message.From}");
        metadata.AppendLine($"To: {message.To}");
        metadata.AppendLine($"Subject: {message.Subject}");
        metadata.AppendLine(new string('=', 80));
        return metadata.ToString();
    }

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "No-Subject";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("", fileName.Select(c =>
            invalidChars.Contains(c) ? '-' : c
        ));

        sanitized = Regex.Replace(sanitized, @"\s+", "-");
        sanitized = Regex.Replace(sanitized, @"-+", "-");
        sanitized = sanitized.Trim('-');

        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50).TrimEnd('-');
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "No-Subject" : sanitized;
    }

    private string GetHtmlBody(MimeMessage message)
    {
        if (message.Body is Multipart multipart)
        {
            var htmlPart = multipart.OfType<TextPart>().FirstOrDefault(p => p.ContentType.MimeType == "text/html");
            return htmlPart?.Text ?? string.Empty;
        }

        if (message.Body is TextPart htmlTextPart && htmlTextPart.ContentType.MimeType == "text/html")
        {
            return htmlTextPart.Text;
        }

        return string.Empty;
    }

    private string GetTextBody(MimeMessage message)
    {
        if (message.Body is Multipart multipart)
        {
            var textPart = multipart.OfType<TextPart>().FirstOrDefault(p => p.ContentType.MimeType == "text/plain");
            return textPart?.Text ?? string.Empty;
        }

        if (message.Body is TextPart plainTextPart && plainTextPart.ContentType.MimeType == "text/plain")
        {
            return plainTextPart.Text;
        }

        return string.Empty;
    }

    private string ConvertHtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</p>", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</div>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"^\s+", string.Empty, RegexOptions.Multiline);
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim();
    }

    private async Task<string> RenderViewToStringAsync(string viewPath, object model)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());

        using var sw = new StringWriter();
        var viewResult = _razorViewEngine.GetView(null, viewPath, false);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Could not find view {viewPath}");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }
}
