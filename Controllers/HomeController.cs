using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Stalika.Web.Models;

namespace Stalika.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Booking");

            return RedirectToAction("Login", "Auth");
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
