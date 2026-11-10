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
        var lowStockItems = await db.Products.AsNoTracking()
            .Where(product => product.IsActive
                && (product.Inventory == null || product.Inventory.QuantityOnHand <= product.ReorderLevel))
            .Select(product => new
            {
                product.Sku,
                product.Name,
                product.Unit,
                product.ReorderLevel,
                quantityOnHand = product.Inventory == null ? (decimal?)null : product.Inventory.QuantityOnHand
            })
            .OrderBy(item => item.quantityOnHand ?? 0).ThenBy(item => item.Name).ThenBy(item => item.Sku)
            .ToListAsync(ct);
        return Ok(new { from = start, to = end, lowStockCount = lowStockItems.Count, days, lowStockItems });
    }

    [HttpGet("reports/sales/details")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> SalesReportDetails(DateOnly? from, DateOnly? to, int page = 1, CancellationToken ct = default)
    {
        if (from is not DateOnly start || to is not DateOnly end || end < start || end.DayNumber - start.DayNumber > 365 || end == DateOnly.MaxValue)
            return BadRequest(new { message = "Khoảng ngày phải hợp lệ và tối đa 366 ngày." });
        if (page < 1 || page > 10000)
            return BadRequest(new { message = "Trang báo cáo không hợp lệ." });

        const int pageSize = 50;
        var startUtc = start.ToDateTime(TimeOnly.MinValue);
        var endUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var invoices = db.SalesInvoices.AsNoTracking()
            .Where(invoice => invoice.Status == "Completed" && invoice.CreatedAt >= startUtc && invoice.CreatedAt < endUtc);
        var totalCount = await invoices.CountAsync(ct);
        var rows = await invoices
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ThenByDescending(invoice => invoice.SalesInvoiceId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(invoice => new
            {
                invoice.SalesInvoiceId,
                invoice.InvoiceNo,
                invoice.CreatedAt,
                customerName = invoice.Customer == null ? "Khách lẻ" : invoice.Customer.FullName,
                cashierName = invoice.CashierUser.Employee == null
                    ? invoice.CashierUser.Username
                    : invoice.CashierUser.Employee.FullName,
                invoice.PaymentMethod,
                invoice.Status,
                invoice.TotalAmount,
                details = invoice.Details.OrderBy(line => line.SalesDetailId).Select(line => new
                {
                    line.SkuSnapshot,
                    line.ProductNameSnapshot,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountAmount,
                    line.LineTotal
                })
            })
            .ToListAsync(ct);

        return Ok(new { page, pageSize, totalCount, invoices = rows });
    }
}
