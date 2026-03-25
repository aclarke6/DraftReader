using Microsoft.AspNetCore.Mvc;
using DraftReader.Domain.Interfaces.Repositories;

namespace DraftReader.Web.Controllers;

public class HomeController(IUserRepository userRepo) : BaseController(userRepo)
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("Login", "Account");

        return await IsAuthorAsync()
            ? RedirectToAction("Dashboard", "Author")
            : RedirectToAction("Dashboard", "Reader");
    }
}
