using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SamMALsurium.Controllers;

/// <summary>
/// Controller for legal pages (Privacy Policy, Terms of Service)
/// </summary>
[AllowAnonymous]
public class LegalController : Controller
{
    /// <summary>
    /// Display the Privacy Policy page
    /// </summary>
    [HttpGet("/privacy-policy")]
    public IActionResult PrivacyPolicy()
    {
        return View();
    }

    /// <summary>
    /// Display the Terms of Service page
    /// </summary>
    [HttpGet("/terms")]
    public IActionResult Terms()
    {
        return View();
    }

    /// <summary>
    /// Display the Impressum page
    /// </summary>
    [HttpGet("/impressum")]
    public IActionResult Impressum()
    {
        return View();
    }

    /// <summary>
    /// Display the Contact page
    /// </summary>
    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        return View();
    }

    /// <summary>
    /// Display the Report Issue page
    /// </summary>
    [HttpGet("/report-issue")]
    public IActionResult ReportIssue()
    {
        return View();
    }
}
