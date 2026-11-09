using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;

namespace QuanLySieuThi.Controllers
{
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Cashier + "," + RoleNames.StockStaff)]
    public sealed class ProductImagesController(ApplicationDbContext db) : Controller
    {
        [HttpGet("/product-images/{id:int}")]
        public async Task<IActionResult> Show(int id, CancellationToken cancellationToken)
        {
            var products = db.Products.AsNoTracking().Where(product => product.ProductId == id);
            if (!User.IsInRole(RoleNames.Admin))
                products = products.Where(product => product.IsActive);

            var image = await products.Select(product => product.Image).SingleOrDefaultAsync(cancellationToken);
            if (image is null) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(image.Content, image.ContentType);
        }
    }
}
