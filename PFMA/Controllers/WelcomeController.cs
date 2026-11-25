using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PFMA.Controllers
{
    public class WelcomeController : Controller
    {
        // GET: Welcome/Index -> first (animated) page of the app, then it opens the Login page
        public IActionResult Index()
        {
            // Already logged in: go straight to the right dashboard
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (role == "Admin") return RedirectToAction("Index", "Admin");
                if (role == "Advisor") return RedirectToAction("Index", "Advisor");
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
