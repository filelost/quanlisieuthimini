using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuanLySieuThi.Models;

namespace QuanLySieuThi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Auth");

            return User.FindFirstValue(ClaimTypes.Role) switch
            {
                RoleNames.Admin => RedirectToAction("Dashboard", "Admin"),
                RoleNames.Cashier => RedirectToAction("Sales", "Screens"),
                RoleNames.StockStaff => RedirectToAction("Inventory", "Screens"),
                RoleNames.Customer => RedirectToAction("CustomerPortal", "Screens"),
                _ => RedirectToAction("Denied", "Auth")
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
