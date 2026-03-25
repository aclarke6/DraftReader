using Microsoft.AspNetCore.Mvc;

namespace DraftReader.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Author");

        return RedirectToAction("Login", "Account");
    }
}
