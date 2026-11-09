using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;
using QuanLySieuThi.Service;

namespace QuanLySieuThi.Controllers
{
    [Authorize(Roles = RoleNames.StockAccess)]
    public sealed class InventoryController(ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Index(string? search, bool lowStock, CancellationToken cancellationToken)
        {
            var products = db.Products.AsNoTracking().Include(product => product.Inventory)
                .Where(product => product.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                products = products.Where(product => product.Name.Contains(search) || product.Sku.Contains(search));
            }
            if (lowStock)
                products = products.Where(product => product.Inventory == null
                    || product.Inventory.QuantityOnHand <= product.ReorderLevel);

            ViewData["Search"] = search;
            ViewData["LowStock"] = lowStock;
            return View(await products.OrderBy(product => product.Name).ToListAsync(cancellationToken));
        }

        public async Task<IActionResult> History(int id, CancellationToken cancellationToken)
        {
            var product = await db.Products.FindAsync([id], cancellationToken);
            if (product is null)
                return NotFound();

            ViewData["Product"] = product.Name;
            var movements = await db.StockTransactions.AsNoTracking().Include(movement => movement.CreatedByUser)
                .Where(movement => movement.ProductId == id).OrderByDescending(movement => movement.StockTransactionId)
                .Take(100).ToListAsync(cancellationToken);
            var receiptIds = movements.Where(movement => movement.ReferenceType == "PurchaseReceipt"
                    && movement.ReferenceId is > 0 and <= int.MaxValue)
                .Select(movement => (int)movement.ReferenceId!.Value).Distinct().ToArray();
            ViewData["ReceiptNumbers"] = await db.PurchaseReceipts.AsNoTracking()
                .Where(receipt => receiptIds.Contains(receipt.PurchaseReceiptId))
                .ToDictionaryAsync(receipt => receipt.PurchaseReceiptId, receipt => receipt.ReceiptNo, cancellationToken);
            return View(movements);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, decimal quantity, string? reason, string? version,
            CancellationToken cancellationToken)
        {
            reason = reason?.Trim();
            if (!ModelState.IsValid || quantity < 0 || quantity >= 1_000_000_000_000_000m
                || decimal.Round(quantity, 3) != quantity || string.IsNullOrEmpty(reason) || reason.Length > 300)
            {
                TempData["StockMessage"] = "Nhập số tồn hợp lệ (tối đa 3 chữ số thập phân) và lý do điều chỉnh.";
                return RedirectToAction(nameof(Index));
            }

            var stock = await db.Inventory.Include(item => item.Product)
                .SingleOrDefaultAsync(item => item.ProductId == id && item.Product.IsActive, cancellationToken);
            if (stock is null)
                return NotFound();

            if (version != Convert.ToBase64String(stock.RowVersion))
            {
                TempData["StockMessage"] = "Tồn kho đã thay đổi. Kiểm tra lại số lượng trước khi điều chỉnh.";
                return RedirectToAction(nameof(Index));
            }

            if (stock.QuantityOnHand == quantity)
                return RedirectToAction(nameof(Index));

            db.StockTransactions.Add(StockMovement.Apply(stock, quantity, "Adjustment", "Inventory", id,
                reason, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)));
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                TempData["StockMessage"] = "Đã điều chỉnh tồn kho và lưu lịch sử.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["StockMessage"] = "Tồn kho đã thay đổi. Kiểm tra lại số lượng trước khi điều chỉnh.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
