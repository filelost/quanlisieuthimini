using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;

namespace QuanLySieuThi.Controllers;

[ApiController]
[Route("api")]
public sealed class StoreApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("products")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Cashier + "," + RoleNames.StockStaff)]
    public async Task<IActionResult> Products(string? search, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().Where(product => product.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(product => product.Name.Contains(search) || product.Sku.Contains(search));
        return Ok(await query.OrderBy(product => product.Name).ThenBy(product => product.ProductId)
            .Take(100).Select(product => new { product.ProductId, product.Sku, product.Name, product.SalePrice })
            .ToListAsync(ct));
    }

    [HttpGet("inventory")]
    [Authorize(Roles = RoleNames.StockAccess)]
    public async Task<IActionResult> Inventory(bool lowStock, CancellationToken ct)
    {
        var query = db.Inventory.AsNoTracking().Where(stock => stock.Product.IsActive);
        if (lowStock)
            query = query.Where(stock => stock.QuantityOnHand <= stock.Product.ReorderLevel);
        return Ok(await query.OrderBy(stock => stock.ProductId).Take(100)
            .Select(stock => new { stock.ProductId, stock.Product.Name, stock.QuantityOnHand, stock.Product.ReorderLevel })
            .ToListAsync(ct));
    }

    [HttpGet("reports/sales")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> SalesReport(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-Math.Min(29, end.DayNumber));
        if (end < start || end.DayNumber - start.DayNumber > 365 || end == DateOnly.MaxValue)
            return BadRequest(new { message = "Khoảng ngày phải hợp lệ và tối đa 366 ngày." });

        var startUtc = start.ToDateTime(TimeOnly.MinValue);
        var endUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var days = await db.SalesInvoices.AsNoTracking()
            .Where(invoice => invoice.Status == "Completed" && invoice.CreatedAt >= startUtc && invoice.CreatedAt < endUtc)
            .GroupBy(invoice => invoice.CreatedAt.Date)
            .Select(day => new { date = day.Key, invoiceCount = day.Count(), revenue = day.Sum(invoice => invoice.TotalAmount) })
            .OrderBy(day => day.date).ToListAsync(ct);
        var lowStockCount = await db.Inventory.CountAsync(stock =>
            stock.Product.IsActive && stock.QuantityOnHand <= stock.Product.ReorderLevel, ct);
        return Ok(new { from = start, to = end, lowStockCount, days });
    }
}
