using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
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
    [Authorize(Roles = RoleNames.SalesAccess)]
    [AutoValidateAntiforgeryToken]
    public sealed class SalesController(ApplicationDbContext db, MomoGateway momo, IConfiguration configuration) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var products = await db.Products.AsNoTracking().Include(product => product.Inventory)
                .Where(product => product.IsActive && product.Inventory != null && product.Inventory.QuantityOnHand > 0)
                .OrderBy(product => product.Name).ToListAsync(cancellationToken);
            var customers = await db.Customers.AsNoTracking().Where(customer => customer.IsActive)
                .OrderBy(customer => customer.FullName).ToListAsync(cancellationToken);
            var productIds = products.Select(product => product.ProductId).ToArray();
            var imageIds = await db.ProductImages.Where(image => productIds.Contains(image.ProductId))
                .Select(image => image.ProductId).ToListAsync(cancellationToken);
            return View(new CheckoutViewModel(Guid.NewGuid(), products, customers,
                configuration.GetSection("Promotions").GetChildren().Select(promotion => promotion.Key).ToList(),
                momo.Configured, imageIds.ToHashSet()));
        }

        [HttpPost]
        public Task<IActionResult> Checkout(CheckoutInput input, CancellationToken cancellationToken)
            => SaveCheckout(input, asJson: false, cancellationToken);

        [HttpGet("/api/sales/checkout-token")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult CheckoutToken([FromServices] IAntiforgery antiforgery)
        {
            var tokens = antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new { headerName = tokens.HeaderName, requestToken = tokens.RequestToken });
        }

        [HttpPost("/api/sales/checkout")]
        public Task<IActionResult> CheckoutJson([FromBody] CheckoutInput? input, CancellationToken cancellationToken)
            => SaveCheckout(input, asJson: true, cancellationToken);

        private async Task<IActionResult> SaveCheckout(CheckoutInput? input, bool asJson, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || input is null || input.RequestId == Guid.Empty || input.Lines is null
                || input.Lines.Count is < 1 or > 100 || input.Lines.Any(line => line is null)
                || input.Lines.Select(line => line.ProductId).Distinct().Count() != input.Lines.Count
                || input.Lines.Any(line => line.Quantity <= 0 || line.Quantity > 1_000_000m
                    || decimal.Round(line.Quantity, 3) != line.Quantity)
                || input.PaymentMethod is not ("Cash" or "MoMo"))
                return BackToSale("Giỏ hàng hoặc phương thức thanh toán không hợp lệ.", asJson);
            if (input.PaymentMethod == "MoMo" && !momo.Configured)
                return BackToSale("Chưa cấu hình MoMo.", asJson);

            var number = $"HD-{input.RequestId:N}";
            var userId = UserId();
            var isMomo = input.PaymentMethod == "MoMo";
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var existing = await db.SalesInvoices.AsNoTracking().SingleOrDefaultAsync(
                    invoice => invoice.InvoiceNo == number, cancellationToken);
                if (existing is not null)
                    return existing.CashierUserId == userId
                        ? CheckoutSaved(existing, asJson) : Conflict();

                if (input.CustomerId is int customerId && !await db.Customers.AnyAsync(
                    customer => customer.CustomerId == customerId && customer.IsActive, cancellationToken))
                    return BackToSale("Khách hàng không hợp lệ.", asJson);

                var ids = input.Lines.Select(line => line.ProductId).ToArray();
                var products = await db.Products.Include(product => product.Inventory)
                    .Where(product => ids.Contains(product.ProductId) && product.IsActive)
                    .ToDictionaryAsync(product => product.ProductId, cancellationToken);
                var invoice = new SalesInvoice
                {
                    InvoiceNo = number,
                    CashierUserId = userId,
                    CustomerId = input.CustomerId,
                    Status = isMomo ? "AwaitingMoMo" : "Completed",
                    PaymentMethod = input.PaymentMethod
                };
                foreach (var line in input.Lines)
                {
                    if (!products.TryGetValue(line.ProductId, out var product)
                        || product.Inventory is null || product.Inventory.QuantityOnHand < line.Quantity)
                        return BackToSale("Có sản phẩm không còn khả dụng hoặc không đủ tồn.", asJson);

                    invoice.Details.Add(new SalesDetail
                    {
                        ProductId = product.ProductId,
                        ProductNameSnapshot = product.Name,
                        SkuSnapshot = product.Sku,
                        Quantity = line.Quantity,
                        UnitPrice = product.SalePrice,
                        LineTotal = Math.Round(product.SalePrice * line.Quantity, isMomo ? 0 : 2, MidpointRounding.AwayFromZero)
                    });
                }

                invoice.Subtotal = invoice.Details.Sum(line => line.LineTotal);
                if (invoice.Subtotal >= 10_000_000_000_000_000m)
                    return BackToSale("Tổng tiền vượt giới hạn hóa đơn.", asJson);
                if (!string.IsNullOrWhiteSpace(input.PromotionCode))
                {
                    var promotion = configuration.GetSection("Promotions").GetChildren().FirstOrDefault(
                        item => item.Key.Equals(input.PromotionCode.Trim(), StringComparison.OrdinalIgnoreCase));
                    var value = promotion?.GetValue<decimal>("Value") ?? 0;
                    var type = promotion?["Type"];
                    if (value <= 0 || type is not ("Fixed" or "Percent") || (type == "Percent" && value > 100))
                        return BackToSale("Mã khuyến mãi không hợp lệ.", asJson);

                    var discount = type == "Percent" ? invoice.Subtotal * value / 100 : value;
                    invoice.DiscountAmount = Math.Min(invoice.Subtotal,
                        Math.Round(discount, isMomo ? 0 : 2, MidpointRounding.AwayFromZero));
                    var code = promotion!.Key;
                    invoice.Note = $"Khuyến mãi: {code[..Math.Min(100, code.Length)]}";
                }
                invoice.TotalAmount = invoice.Subtotal - invoice.DiscountAmount;
                if (!isMomo)
                {
                    var received = input.CashReceived ?? invoice.TotalAmount;
                if (received < invoice.TotalAmount || received > 1_000_000_000_000_000m
               || decimal.Round(received, 2) != received)
              return BackToSale("Số tiền khách đưa phải lớn hơn hoặc bằng tổng thanh toán.", asJson);
            invoice.CashReceived = received;
            invoice.ChangeAmount = received - invoice.TotalAmount;
           }
         else
        {
           invoice.CashReceived = invoice.TotalAmount;
         invoice.ChangeAmount = 0;
         }
                invoice.TotalAmount = invoice.Subtotal - invoice.DiscountAmount;
                if (isMomo && (invoice.TotalAmount < 1_000 || invoice.TotalAmount > 50_000_000
                    || decimal.Truncate(invoice.TotalAmount) != invoice.TotalAmount))
                    return BackToSale("MoMo yêu cầu số tiền nguyên từ 1.000 đến 50.000.000 ₫.", asJson);

                db.SalesInvoices.Add(invoice);
                await db.SaveChangesAsync(cancellationToken);
                foreach (var line in invoice.Details)
                {
                    var stock = products[line.ProductId].Inventory!;
                    db.StockTransactions.Add(StockMovement.Apply(stock, stock.QuantityOnHand - line.Quantity,
                        isMomo ? "Reserve" : "Sale", "SalesInvoice", invoice.SalesInvoiceId, null, userId));
                }
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return CheckoutSaved(invoice, asJson);
            }
            catch (Exception exception) when (IsConcurrentWrite(exception))
            {
                return BackToSale("Dữ liệu vừa thay đổi. Kiểm tra danh sách hóa đơn trước khi tạo lại.", asJson);
            }
        }        
        public async Task<IActionResult> Invoices(string? search, CancellationToken cancellationToken)
        {
            var invoices = VisibleInvoices().AsNoTracking().Include(invoice => invoice.Customer).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                invoices = invoices.Where(invoice => invoice.InvoiceNo.Contains(search.Trim()));

            ViewData["Search"] = search;
            return View(await invoices.OrderByDescending(invoice => invoice.SalesInvoiceId).Take(100).ToListAsync(cancellationToken));
        }

        public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
        {
            var invoice = await VisibleInvoices().AsNoTracking()
                .Include(invoice => invoice.Customer)
                .Include(invoice => invoice.CashierUser).ThenInclude(user => user.Employee)
                .Include(invoice => invoice.Details.OrderBy(line => line.SalesDetailId))
                .SingleOrDefaultAsync(invoice => invoice.SalesInvoiceId == id, cancellationToken);
                 if (invoice is null)
                 return NotFound();
                 if (invoice.Status != "Completed")
                 return BadRequest("Chỉ có thể in hóa đơn đã thanh toán thành công.");
                 return View("Print", invoice);
        }

        [HttpPost]
        public async Task<IActionResult> Pay(long id, CancellationToken cancellationToken)
        {
            var invoice = await VisibleInvoices().AsNoTracking().SingleOrDefaultAsync(
                invoice => invoice.SalesInvoiceId == id, cancellationToken);
            if (invoice is null)
                return NotFound();
            if (!momo.Configured || invoice.PaymentMethod != "MoMo" || invoice.Status is not ("AwaitingMoMo" or "Pending"))
                return BackToInvoice(id, "Hóa đơn chưa thể mở thanh toán MoMo.");

            if (invoice.Status == "AwaitingMoMo")
            {
                var started = await db.SalesInvoices.Where(item => item.SalesInvoiceId == id && item.Status == "AwaitingMoMo")
                    .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, "Pending"), cancellationToken);
                if (started == 0)
                    return BackToInvoice(id, "Trạng thái hóa đơn vừa thay đổi. Hãy kiểm tra lại.");
            }

            try
            {
                var link = await momo.CreateAsync(invoice.InvoiceNo, (long)invoice.TotalAmount, cancellationToken);
                if (link is not null)
                    return Redirect(link);
            }
            catch (Exception exception) when (IsGatewayError(exception, cancellationToken))
            {
                // A timed-out create may already have reached MoMo.
            }
            return BackToInvoice(id, "Chưa nhận được liên kết MoMo. Kiểm tra trạng thái hoặc mở lại trên cùng hóa đơn.");
        }

        [HttpPost]
        public async Task<IActionResult> Reconcile(long id, CancellationToken cancellationToken)
        {
            var invoice = await VisibleInvoices().AsNoTracking().SingleOrDefaultAsync(
                invoice => invoice.SalesInvoiceId == id, cancellationToken);
            if (invoice is null)
                return NotFound();
            if (!momo.Configured || invoice is not { Status: "Pending", PaymentMethod: "MoMo" })
                return RedirectToAction(nameof(Details), new { id });

            try
            {
                var result = await momo.QueryAsync(invoice.InvoiceNo, cancellationToken);
                if (result is not null)
                    await ApplyMomoResult(result, cancellationToken);
            }
            catch (Exception exception) when (IsGatewayError(exception, cancellationToken) || IsConcurrentWrite(exception))
            {
                return BackToInvoice(id, "Chưa đối soát được. Thử lại trên cùng hóa đơn.");
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost("/payments/momo/ipn")]
        public async Task<IActionResult> MomoIpn([FromBody] MomoResult? result, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || result is null || !momo.Verify(result))
                return BadRequest();

            try
            {
                return await ApplyMomoResult(result, cancellationToken) ? NoContent() : BadRequest();
            }
            catch (Exception exception) when (IsConcurrentWrite(exception))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Cancel(long id, string? refundReference, CancellationToken cancellationToken)
        {
            refundReference = refundReference?.Trim();
            if (string.IsNullOrEmpty(refundReference) || refundReference.Length > 150)
                return BackToInvoice(id, "Nhập thông tin xác nhận đã hoàn tiền, tối đa 150 ký tự.");

            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var invoice = await db.SalesInvoices.Include(invoice => invoice.Details)
                    .SingleOrDefaultAsync(invoice => invoice.SalesInvoiceId == id, cancellationToken);
                if (invoice is null)
                    return NotFound();
                if (invoice.Status != "Completed")
                    return BackToInvoice(id, "Chỉ hủy hóa đơn đã hoàn tất.");

                await RestoreStock(invoice, "CancelSale", refundReference, UserId(), cancellationToken);
                invoice.Status = "Cancelled";
                var priorNote = invoice.Note ?? string.Empty;
                invoice.Note = $"{priorNote[..Math.Min(300, priorNote.Length)]} | Hoàn tiền thủ công: {refundReference}".TrimStart(' ', '|');
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return BackToInvoice(id, "Đã hủy hóa đơn và hoàn tồn kho.");
            }
            catch (Exception exception) when (IsConcurrentWrite(exception))
            {
                return BackToInvoice(id, "Dữ liệu vừa thay đổi. Kiểm tra lại hóa đơn.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Discard(long id, CancellationToken cancellationToken)
        {
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var invoice = await VisibleInvoices().Include(item => item.Details)
                    .SingleOrDefaultAsync(item => item.SalesInvoiceId == id, cancellationToken);
                if (invoice is null)
                    return NotFound();
                if (invoice.Status != "AwaitingMoMo")
                    return BackToInvoice(id, "Hóa đơn đã mở thanh toán. Kiểm tra kết quả MoMo trước khi xử lý.");

                await RestoreStock(invoice, "Release", "Hủy trước khi mở thanh toán MoMo.", UserId(), cancellationToken);
                invoice.Status = "Cancelled";
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return BackToInvoice(id, "Đã bỏ hóa đơn chưa gửi sang MoMo và hoàn tồn.");
            }
            catch (Exception exception) when (IsConcurrentWrite(exception))
            {
                return BackToInvoice(id, "Trạng thái hóa đơn vừa thay đổi. Hãy kiểm tra lại.");
            }
        }

        private async Task<bool> ApplyMomoResult(MomoResult result, CancellationToken cancellationToken)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var invoice = await db.SalesInvoices.Include(invoice => invoice.Details).SingleOrDefaultAsync(
                invoice => invoice.InvoiceNo == result.OrderId && invoice.PaymentMethod == "MoMo", cancellationToken);
            if (invoice is null || invoice.TotalAmount != result.Amount)
                return false;
            if (invoice.Status != "Pending")
                return true;

            if (result is { ResultCode: 0, TransId: > 0 })
            {
                invoice.Status = "Completed";
                invoice.Note = $"{invoice.Note} | MoMo: {result.TransId}".TrimStart(' ', '|');
            }
            else if (result.ResultCode is 1001 or 1002 or 1003 or 1004 or 1005 or 1006 or 1007 or 1017 or 1026)
            {
                await RestoreStock(invoice, "Release", $"MoMo: {result.ResultCode}", invoice.CashierUserId, cancellationToken);
                invoice.Status = "Failed";
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        private async Task RestoreStock(SalesInvoice invoice, string type, string reason, int userId, CancellationToken cancellationToken)
        {
            var ids = invoice.Details.Select(line => line.ProductId).ToArray();
            var stocks = await db.Inventory.Where(stock => ids.Contains(stock.ProductId))
                .ToDictionaryAsync(stock => stock.ProductId, cancellationToken);
            foreach (var line in invoice.Details)
            {
                if (!stocks.TryGetValue(line.ProductId, out var stock))
                {
                    stock = new Inventory { ProductId = line.ProductId };
                    stocks.Add(line.ProductId, stock);
                    db.Inventory.Add(stock);
                }
                db.StockTransactions.Add(StockMovement.Apply(stock, stock.QuantityOnHand + line.Quantity, type,
                    "SalesInvoice", invoice.SalesInvoiceId, reason, userId));
            }
        }

        private IQueryable<SalesInvoice> VisibleInvoices()
        {
            if (User.IsInRole(RoleNames.Admin))
                return db.SalesInvoices;

            var id = UserId();
            return db.SalesInvoices.Where(invoice => invoice.CashierUserId == id);
        }

        private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private IActionResult BackToSale(string message, bool asJson)
        {
            if (asJson) return BadRequest(new { message });
            TempData["SaleMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        private IActionResult CheckoutSaved(SalesInvoice invoice, bool asJson)
            => asJson
                ? Ok(new { invoice.SalesInvoiceId, invoice.InvoiceNo, invoice.Status, invoice.TotalAmount })
                : RedirectToAction(nameof(Details), new { id = invoice.SalesInvoiceId });

        private IActionResult BackToInvoice(long id, string message)
        {
            TempData["SaleMessage"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        private static bool IsConcurrentWrite(Exception exception) => exception is DbUpdateConcurrencyException
            || exception is SqlException { Number: 1205 }
            || exception.InnerException is SqlException { Number: 1205 or 2601 or 2627 };

        private static bool IsGatewayError(Exception exception, CancellationToken cancellationToken) =>
            exception is HttpRequestException or JsonException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
    }
}
