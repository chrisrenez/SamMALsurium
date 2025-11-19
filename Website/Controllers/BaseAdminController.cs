using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SamMALsurium.Controllers;

[Authorize(Roles = "Admin")]
public abstract class BaseAdminController : Controller
{
}
