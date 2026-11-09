using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;
using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Controllers
{
    [Authorize]
    public class ScreensController(ApplicationDbContext db) : Controller
    {
        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Dashboard() => RedirectToAction("Dashboard", "Admin");

        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Products() => RedirectToAction("Products", "Admin");

        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Suppliers() => RedirectToAction("Suppliers", "Admin");

        [Authorize(Roles = RoleNames.StockAccess)]
        public IActionResult Purchases() => RedirectToAction("Index", "Purchases");

        [Authorize(Roles = RoleNames.StockAccess)]
        public IActionResult Inventory() => RedirectToAction("Index", "Inventory");

        [Authorize(Roles = RoleNames.SalesAccess)]
        public IActionResult Sales() => RedirectToAction("Index", "Sales");

        [Authorize(Roles = RoleNames.SalesAccess)]
        public async Task<IActionResult> Customers(string? search, CancellationToken cancellationToken)
        {
            search = search?.Trim();
            var customers = db.Customers.AsNoTracking().Where(customer => customer.IsActive);
            if (!string.IsNullOrEmpty(search))
            {
                customers = customers.Where(customer => customer.CustomerCode.Contains(search)
                    || customer.FullName.Contains(search)
                    || (customer.Phone != null && customer.Phone.Contains(search)));
            }

            ViewData["Search"] = search;
            return View(await customers.OrderBy(customer => customer.FullName)
                .ThenBy(customer => customer.CustomerId).ToListAsync(cancellationToken));
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.SalesAccess)]
        public async Task<IActionResult> CustomerForm(int? id, CancellationToken cancellationToken)
        {
            if (id is null)
                return View(new CustomerFormViewModel());

            var model = await db.Customers.AsNoTracking()
                .Where(customer => customer.CustomerId == id && customer.IsActive)
                .Select(customer => new CustomerFormViewModel
                {
                    CustomerId = customer.CustomerId,
                    FullName = customer.FullName,
                    Phone = customer.Phone,
                    Email = customer.Email,
                    Address = customer.Address
                }).SingleOrDefaultAsync(cancellationToken);
            return model is null ? NotFound() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.SalesAccess)]
        public async Task<IActionResult> CustomerForm(int? id, CustomerFormViewModel model, CancellationToken cancellationToken)
        {
            model.CustomerId = id ?? 0;
            if (!ModelState.IsValid)
                return View(model);

            Customer customer;
            if (id is null)
            {
                customer = new Customer { CustomerCode = $"KH-{Guid.NewGuid():N}"[..30] };
                db.Customers.Add(customer);
            }
            else
            {
                var existing = await db.Customers.SingleOrDefaultAsync(
                    customer => customer.CustomerId == id && customer.IsActive, cancellationToken);
                if (existing is null)
                    return NotFound();

                customer = existing;
            }

            customer.FullName = model.FullName.Trim();
            customer.Phone = model.Phone?.Trim();
            customer.Email = model.Email?.Trim();
            customer.Address = model.Address?.Trim();
            await db.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Customers));
        }

        [Authorize(Roles = RoleNames.SalesAccess)]
        public IActionResult Invoices() => RedirectToAction("Invoices", "Sales");

        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Employees() => RedirectToAction("Employees", "Admin");

        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Reports() => View();

        [Authorize(Roles = RoleNames.Customer)]
        public IActionResult CustomerPortal() => View();
    }
}
