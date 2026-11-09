using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;
using QuanLySieuThi.Models.Entities;
using QuanLySieuThi.Service;

namespace QuanLySieuThi.Controllers
{
    [Authorize(Roles = RoleNames.StockAccess)]
    [AutoValidateAntiforgeryToken]
    public sealed class PurchasesController(ApplicationDbContext db) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewData["Suppliers"] = await db.Suppliers.AsNoTracking().Where(supplier => supplier.IsActive)
                .OrderBy(supplier => supplier.Name).ToListAsync(cancellationToken);
            return View(await db.PurchaseReceipts.AsNoTracking().Include(receipt => receipt.Supplier)
                .Include(receipt => receipt.CreatedByUser)
                .OrderByDescending(receipt => receipt.PurchaseReceiptId).ToListAsync(cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Create(int supplierId, CancellationToken cancellationToken)
        {
            if (!await db.Suppliers.AnyAsync(supplier => supplier.SupplierId == supplierId && supplier.IsActive, cancellationToken))
                return BadRequest("Nhà cung cấp không hợp lệ.");

            var receipt = new PurchaseReceipt
            {
                ReceiptNo = $"PN-{Guid.NewGuid():N}",
                SupplierId = supplierId,
                CreatedByUserId = UserId(),
                CreatedAt = DateTime.UtcNow
            };
            db.PurchaseReceipts.Add(receipt);
            await db.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Details), new { id = receipt.PurchaseReceiptId });
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var receipt = await ReceiptQuery().AsNoTracking().Include(item => item.CreatedByUser)
                .Include(item => item.Details).ThenInclude(line => line.CreatedByUser).SingleOrDefaultAsync(
                receipt => receipt.PurchaseReceiptId == id, cancellationToken);
            if (receipt is null)
                return NotFound();

            var deliveries = await ReceiptMovements(id).AsNoTracking().Include(movement => movement.Product)
                .Include(movement => movement.CreatedByUser)
                .OrderByDescending(movement => movement.StockTransactionId).ToListAsync(cancellationToken);
            var received = deliveries.GroupBy(movement => movement.ProductId)
                .ToDictionary(group => group.Key, group => group.Sum(movement => movement.QuantityChange));
            var shortageActions = await ReceiptShortageActions(id).AsNoTracking().Include(action => action.CreatedByUser)
                .OrderByDescending(action => action.PurchaseShortageActionId).ToListAsync(cancellationToken);
            var products = await db.Products.AsNoTracking().Where(product => product.IsActive)
                .OrderBy(product => product.Name).ToListAsync(cancellationToken);
            return View(new PurchasePageViewModel(receipt, products, received, deliveries, shortageActions));
        }

        [HttpPost]
        public async Task<IActionResult> AddLine(int id, int productId, decimal quantity, decimal unitCost,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || !ValidQuantity(quantity) || unitCost < 0 || unitCost > 1_000_000_000_000m
                || decimal.Round(unitCost, 2) != unitCost)
                return Back(id, "Nhập số lượng và giá nhập hợp lệ.");

            return await UpdateReceipt(id, async receipt =>
            {
                if (receipt.Status != "Draft" || receipt.Details.Any(line => line.ProductId == productId)
                    || !await db.Products.AnyAsync(product => product.ProductId == productId && product.IsActive, cancellationToken))
                    return "Chỉ thêm sản phẩm đang hoạt động, chưa có trên phiếu nháp.";

                var total = decimal.Round(quantity * unitCost, 2, MidpointRounding.AwayFromZero);
                if (receipt.TotalAmount + total >= 10_000_000_000_000_000m)
                    return "Tổng tiền vượt giới hạn của phiếu.";

                receipt.Details.Add(new PurchaseDetail
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitCost = unitCost,
                    LineTotal = total,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = UserId()
                });
                receipt.TotalAmount += total;
                return "Đã thêm sản phẩm.";
            }, cancellationToken);
        }

        [HttpPost]
        public async Task<IActionResult> Receive(int id, int lineId, decimal quantity, decimal expectedReceived,
            string? reason, string? supplierDocumentNo, CancellationToken cancellationToken)
        {
            reason = reason?.Trim();
            supplierDocumentNo = supplierDocumentNo?.Trim();
            if (!ModelState.IsValid || !ValidQuantity(quantity) || reason?.Length > 300 || supplierDocumentNo?.Length > 150)
                return Back(id, "Nhập số lượng thực nhận, mã chứng từ và ghi chú hợp lệ.");

            return await UpdateReceipt(id, async receipt =>
            {
                var line = receipt.Details.SingleOrDefault(line => line.PurchaseDetailId == lineId);
                if (line is null || receipt.Status is not ("Draft" or "Partial"))
                    return "Dòng hàng hoặc trạng thái phiếu không hợp lệ.";

                var received = await ReceivedQuantities(id, cancellationToken);
                var closed = await ClosedQuantities(id, cancellationToken);
                var previous = received.GetValueOrDefault(line.ProductId);
                if (previous != expectedReceived)
                    return "Phiếu đã thay đổi. Kiểm tra lại số lượng thực nhận.";
                var remaining = line.Quantity - previous - closed.GetValueOrDefault(lineId);
                if (remaining <= 0)
                    return "Dòng hàng đã nhận đủ hoặc đã chốt phần thiếu.";
                if (quantity > remaining)
                    return "Số lượng thực nhận vượt số lượng còn thiếu.";
                if (quantity < remaining && string.IsNullOrEmpty(reason))
                    return "Ghi rõ lý do hàng thiếu hoặc hàng lỗi khi nhận một phần.";

                var stock = await db.Inventory.FindAsync([line.ProductId], cancellationToken);
                if (stock is null)
                {
                    stock = new Inventory { ProductId = line.ProductId };
                    db.Inventory.Add(stock);
                }
                if (stock.QuantityOnHand + quantity >= 1_000_000_000_000_000m)
                    return "Số lượng tồn vượt giới hạn.";

                var movement = StockMovement.Apply(stock, stock.QuantityOnHand + quantity,
                    "Purchase", "PurchaseReceipt", id, reason, UserId());
                movement.SupplierDocumentNo = string.IsNullOrEmpty(supplierDocumentNo) ? null : supplierDocumentNo;
                db.StockTransactions.Add(movement);
                received[line.ProductId] = previous + quantity;
                receipt.ReceivedAt = movement.CreatedAt;
                SetReceiptStatus(receipt, received, closed);
                return quantity < remaining
                    ? $"Đã cập nhật tồn kho theo số lượng thực nhận. Còn thiếu {remaining - quantity:0.###} {line.Product.Unit}. Chọn cách xử lý phần thiếu trên phiếu."
                    : "Đã ghi nhận số lượng thực nhận và cập nhật tồn kho.";
            }, cancellationToken);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveShortage(int id, int lineId, decimal expectedRemaining,
            string? resolution, string? reason, string? supplierReference, CancellationToken cancellationToken)
        {
            if (resolution == "Close" && !User.IsInRole(RoleNames.Admin))
                return Forbid();

            reason = reason?.Trim();
            supplierReference = supplierReference?.Trim();
            if (!ModelState.IsValid || !ValidQuantity(expectedRemaining)
                || resolution is not ("FollowUp" or "Close") || string.IsNullOrEmpty(reason) || reason.Length > 300
                || supplierReference?.Length > 150 || (resolution == "Close" && string.IsNullOrEmpty(supplierReference)))
                return Back(id, "Nhập nội dung xử lý. Khi chốt thiếu, cần có mã hoặc thông tin xác nhận của nhà cung cấp.");

            return await UpdateReceipt(id, async receipt =>
            {
                var line = receipt.Details.SingleOrDefault(item => item.PurchaseDetailId == lineId);
                if (line is null || receipt.Status is not ("Draft" or "Partial"))
                    return "Dòng hàng hoặc trạng thái phiếu không hợp lệ.";

                var received = await ReceivedQuantities(id, cancellationToken);
                var closed = await ClosedQuantities(id, cancellationToken);
                var remaining = line.Quantity - received.GetValueOrDefault(line.ProductId) - closed.GetValueOrDefault(lineId);
                if (remaining <= 0 || remaining != expectedRemaining)
                    return "Số lượng còn thiếu đã thay đổi. Kiểm tra lại phiếu trước khi xử lý.";

                var previous = await ReceiptShortageActions(id).Where(action => action.PurchaseDetailId == lineId)
                    .OrderByDescending(action => action.PurchaseShortageActionId).FirstOrDefaultAsync(cancellationToken);
                if (previous is not null && previous.Action == resolution && previous.Quantity == remaining
                    && previous.Reason == reason && previous.SupplierReference == supplierReference)
                    return "Nội dung xử lý này đã được ghi nhận.";

                db.PurchaseShortageActions.Add(new PurchaseShortageAction
                {
                    PurchaseDetailId = lineId,
                    Action = resolution,
                    Quantity = remaining,
                    Reason = reason,
                    SupplierReference = supplierReference,
                    CreatedByUserId = UserId()
                });
                if (resolution == "Close")
                    closed[lineId] = remaining;
                SetReceiptStatus(receipt, received, closed);

                return resolution == "Close"
                    ? "Đã chốt phần thiếu theo xác nhận của nhà cung cấp. Chỉ hàng thực nhận được tính vào tồn kho."
                    : "Đã ghi vào phiếu việc chờ nhà cung cấp giao bù. Ghi nhận hàng khi đợt giao bù đến.";
            }, cancellationToken);
        }

        private async Task<IActionResult> UpdateReceipt(int id, Func<PurchaseReceipt, Task<string>> update,
            CancellationToken cancellationToken)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var receipt = await ReceiptQuery().SingleOrDefaultAsync(receipt => receipt.PurchaseReceiptId == id, cancellationToken);
                if (receipt is null)
                    return NotFound();

                var message = await update(receipt);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Back(id, message);
            }
            catch (Exception exception) when (exception is DbUpdateConcurrencyException
                || exception is SqlException { Number: 1205 }
                || exception.InnerException is SqlException { Number: 1205 })
            {
                return Back(id, "Dữ liệu vừa thay đổi. Kiểm tra lại phiếu trước khi gửi lại.");
            }
        }

        private IQueryable<PurchaseReceipt> ReceiptQuery() => db.PurchaseReceipts
            .Include(receipt => receipt.Supplier).Include(receipt => receipt.Details).ThenInclude(line => line.Product);

        private IQueryable<StockTransaction> ReceiptMovements(int id) => db.StockTransactions
            .Where(movement => movement.ReferenceType == "PurchaseReceipt" && movement.ReferenceId == id
                && movement.Type == "Purchase");

        private IQueryable<PurchaseShortageAction> ReceiptShortageActions(int id) =>
            db.PurchaseShortageActions.Where(action => action.PurchaseDetail.PurchaseReceiptId == id);

        private Task<Dictionary<int, decimal>> ReceivedQuantities(int id, CancellationToken cancellationToken) =>
            ReceiptMovements(id).GroupBy(movement => movement.ProductId)
                .ToDictionaryAsync(group => group.Key, group => group.Sum(movement => movement.QuantityChange), cancellationToken);

        private Task<Dictionary<int, decimal>> ClosedQuantities(int id, CancellationToken cancellationToken) =>
            ReceiptShortageActions(id).Where(action => action.Action == "Close")
                .ToDictionaryAsync(action => action.PurchaseDetailId, action => action.Quantity, cancellationToken);

        private static void SetReceiptStatus(PurchaseReceipt receipt, Dictionary<int, decimal> received,
            Dictionary<int, decimal> closed)
        {
            var complete = receipt.Details.All(line => received.GetValueOrDefault(line.ProductId)
                + closed.GetValueOrDefault(line.PurchaseDetailId) >= line.Quantity);
            receipt.Status = !complete ? "Partial" : closed.Count > 0 ? "ClosedShort" : "Received";
        }

        private IActionResult Back(int id, string message)
        {
            TempData["PurchaseMessage"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private static bool ValidQuantity(decimal quantity) =>
            quantity > 0 && quantity <= 1_000_000_000m && decimal.Round(quantity, 3) == quantity;
    }
}
