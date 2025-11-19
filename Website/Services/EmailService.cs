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
    private readonly ILogger<EmailService> _logger;
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        IRazorViewEngine razorViewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider)
    {
        _emailSettings = emailSettings.Value;
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
            UserId = userId
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
            UserFirstName = userFirstName
        };

        var htmlBody = await RenderViewToStringAsync("~/Views/Emails/AccountReactivated.cshtml", model);

        await SendEmailAsync(
            userEmail,
            "Ihr SamMALsurium-Konto wurde reaktiviert",
            htmlBody
        );
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

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
            throw;
        }
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
