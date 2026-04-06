using DraftView.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DraftView.Web.Controllers
{
    [Authorize(Roles = "SystemSupport")]
    public class SupportController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        public IActionResult Dashboard()
        {
            var model = new SupportDashboardViewModel {
                SystemStatus = "Operational",
                ActiveAuthors = 0,
                ActiveReaders = 0
            };

            return View(model);
        }
    }
}