using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SamMALsurium.Models;
using SamMALsurium.Models.Configuration;

namespace SamMALsurium.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationSettings _applicationSettings;

    public HomeController(ILogger<HomeController> logger, IOptions<ApplicationSettings> applicationSettings)
    {
        _logger = logger;
        _applicationSettings = applicationSettings.Value;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Maintenance()
    {
        ViewBag.MaintenanceMessage = _applicationSettings.MaintenanceMessage;
        return View();
    }

    public IActionResult Overview()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
