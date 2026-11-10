using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;

namespace QuanLySieuThi.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public sealed class ReportsController(ApplicationDbContext db) : Controller
{
    private const int InvoiceRowLimit = 100_000;
    private const int BestSellerLimit = 500;
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet]
    public async Task<IActionResult> ExportExcel(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-Math.Min(29, end.DayNumber));
        if (end < start || end.DayNumber - start.DayNumber > 365 || end == DateOnly.MaxValue)
            return BadRequest(new { message = "Khoảng ngày phải hợp lệ và tối đa 366 ngày." });

        var startUtc = start.ToDateTime(TimeOnly.MinValue);
        var endUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var dailySales = await db.SalesInvoices.AsNoTracking()
            .Where(invoice => invoice.Status == "Completed"
                && invoice.CreatedAt >= startUtc && invoice.CreatedAt < endUtc)
            .GroupBy(invoice => invoice.CreatedAt.Date)
            .Select(day => new RevenueDayRow(day.Key, day.Count(), day.Sum(invoice => invoice.TotalAmount)))
            .OrderBy(day => day.Date)
            .ToListAsync(cancellationToken);

        var invoices = await db.SalesInvoices.AsNoTracking()
            .Where(invoice => invoice.CreatedAt >= startUtc && invoice.CreatedAt < endUtc)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ThenByDescending(invoice => invoice.SalesInvoiceId)
            .Take(InvoiceRowLimit + 1)
            .Select(invoice => new InvoiceExportRow(
                invoice.InvoiceNo,
                invoice.CreatedAt,
                invoice.Customer == null ? "Khách lẻ" : invoice.Customer.FullName,
                invoice.CashierUser.Employee == null
                    ? invoice.CashierUser.Username : invoice.CashierUser.Employee.FullName,
                invoice.PaymentMethod,
                invoice.Status,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.TotalAmount))
            .ToListAsync(cancellationToken);
        var invoicesTruncated = invoices.Count > InvoiceRowLimit;
        if (invoicesTruncated)
            invoices.RemoveAt(invoices.Count - 1);

        var inventory = await db.Products.AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .Select(product => new InventoryExportRow(
                product.Sku,
                product.Name,
                product.Category.Name,
                product.Unit,
                product.SalePrice,
                product.Inventory == null ? 0m : product.Inventory.QuantityOnHand,
                product.ReorderLevel))
            .ToListAsync(cancellationToken);

        var bestSellers = await db.SalesDetails.AsNoTracking()
            .Where(line => line.SalesInvoice.Status == "Completed"
                && line.SalesInvoice.CreatedAt >= startUtc && line.SalesInvoice.CreatedAt < endUtc)
            .GroupBy(line => new { line.SkuSnapshot, line.ProductNameSnapshot })
            .Select(group => new BestSellerExportRow(group.Key.SkuSnapshot, group.Key.ProductNameSnapshot,
                group.Sum(line => line.Quantity), group.Sum(line => line.LineTotal)))
            .OrderByDescending(product => product.QuantitySold)
            .ThenByDescending(product => product.LineRevenue)
            .ThenBy(product => product.Sku)
            .Take(BestSellerLimit)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        AddRevenueSheet(workbook, dailySales, start, end);
        AddInvoiceSheet(workbook, invoices, invoicesTruncated, start, end);
        AddInventorySheet(workbook, inventory);
        AddBestSellerSheet(workbook, bestSellers, start, end);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"BaoCaoSieuThi_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), ExcelContentType, fileName);
    }

    private static void AddRevenueSheet(XLWorkbook workbook, IEnumerable<RevenueDayRow> rows, DateOnly start, DateOnly end)
    {
        var sheet = CreateSheet(workbook, "Doanh thu", "Báo cáo doanh thu theo ngày UTC",
            $"Khoảng ngày: {start:dd/MM/yyyy} – {end:dd/MM/yyyy}", ["Ngày UTC", "Số hóa đơn", "Doanh thu"]);
        var rowNumber = 5;
        decimal totalRevenue = 0;
        var totalInvoices = 0;
        foreach (var day in rows)
        {
            sheet.Cell(rowNumber, 1).Value = day.Date;
            sheet.Cell(rowNumber, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            sheet.Cell(rowNumber, 2).Value = day.InvoiceCount;
            sheet.Cell(rowNumber, 3).Value = day.Revenue;
            totalInvoices += day.InvoiceCount;
            totalRevenue += day.Revenue;
            rowNumber++;
        }

        var lastDataRow = rowNumber - 1;
        sheet.Cell(rowNumber, 1).Value = "Tổng cộng";
        sheet.Cell(rowNumber, 2).Value = totalInvoices;
        sheet.Cell(rowNumber, 3).Value = totalRevenue;
        sheet.Range(rowNumber, 1, rowNumber, 3).Style.Font.Bold = true;
        FinishSheet(sheet, 4, lastDataRow, [20, 20, 24], [3]);
    }

    private static void AddInvoiceSheet(XLWorkbook workbook, IEnumerable<InvoiceExportRow> rows, bool truncated,
        DateOnly start, DateOnly end)
    {
        var note = $"Khoảng ngày: {start:dd/MM/yyyy} – {end:dd/MM/yyyy} (UTC)."
            + (truncated ? $" Chỉ xuất {InvoiceRowLimit:N0} hóa đơn mới nhất." : string.Empty);
        var sheet = CreateSheet(workbook, "Hóa đơn", "Danh sách hóa đơn trong kỳ", note,
            ["Mã hóa đơn", "Ngày giờ (UTC+7)", "Khách hàng", "Thu ngân", "Thanh toán", "Trạng thái",
                "Tạm tính", "Giảm giá", "Tổng tiền"]);
        var rowNumber = 5;
        foreach (var invoice in rows)
        {
            sheet.Cell(rowNumber, 1).Value = invoice.InvoiceNo;
            sheet.Cell(rowNumber, 2).Value = DateTime.SpecifyKind(invoice.CreatedAt, DateTimeKind.Utc).AddHours(7);
            sheet.Cell(rowNumber, 2).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
            sheet.Cell(rowNumber, 3).Value = invoice.Customer;
            sheet.Cell(rowNumber, 4).Value = invoice.Cashier;
            sheet.Cell(rowNumber, 5).Value = PaymentLabel(invoice.PaymentMethod);
            sheet.Cell(rowNumber, 6).Value = StatusLabel(invoice.Status);
            sheet.Cell(rowNumber, 7).Value = invoice.Subtotal;
            sheet.Cell(rowNumber, 8).Value = invoice.DiscountAmount;
            sheet.Cell(rowNumber, 9).Value = invoice.TotalAmount;
            rowNumber++;
        }

        FinishSheet(sheet, 4, rowNumber - 1, [24, 22, 30, 26, 22, 25, 20, 20, 20], [7, 8, 9]);
    }

    private static void AddInventorySheet(XLWorkbook workbook, IEnumerable<InventoryExportRow> rows)
    {
        var sheet = CreateSheet(workbook, "Tồn kho", "Tồn kho hiện tại của sản phẩm đang hoạt động",
            "Số lượng bằng 0 nếu sản phẩm chưa có bản ghi tồn kho.",
            ["SKU", "Sản phẩm", "Danh mục", "Đơn vị", "Giá bán", "Tồn hiện tại", "Ngưỡng cảnh báo", "Trạng thái"]);
        var rowNumber = 5;
        foreach (var product in rows)
        {
            var quantity = product.QuantityOnHand;
            var reorderLevel = product.ReorderLevel;
            sheet.Cell(rowNumber, 1).Value = product.Sku;
            sheet.Cell(rowNumber, 2).Value = product.Name;
            sheet.Cell(rowNumber, 3).Value = product.Category;
            sheet.Cell(rowNumber, 4).Value = product.Unit;
            sheet.Cell(rowNumber, 5).Value = product.SalePrice;
            sheet.Cell(rowNumber, 6).Value = quantity;
            sheet.Cell(rowNumber, 7).Value = reorderLevel;
            sheet.Cell(rowNumber, 8).Value = quantity <= reorderLevel ? "Tồn thấp" : "Đủ hàng";
            rowNumber++;
        }

        FinishSheet(sheet, 4, rowNumber - 1, [18, 38, 24, 14, 20, 20, 22, 18], [5]);
        sheet.Column(6).Style.NumberFormat.Format = "0.###";
        sheet.Column(7).Style.NumberFormat.Format = "0.###";
    }

    private static void AddBestSellerSheet(XLWorkbook workbook, IEnumerable<BestSellerExportRow> rows,
        DateOnly start, DateOnly end)
    {
        var sheet = CreateSheet(workbook, "Hàng bán chạy", "Sắp xếp theo số lượng bán giảm dần",
            $"Khoảng ngày: {start:dd/MM/yyyy} – {end:dd/MM/yyyy} (UTC). Doanh thu là tổng thành tiền dòng, chưa phân bổ giảm giá hóa đơn.",
            ["SKU", "Sản phẩm", "Số lượng bán", "Doanh thu dòng"]);
        var rowNumber = 5;
        foreach (var product in rows)
        {
            sheet.Cell(rowNumber, 1).Value = product.Sku;
            sheet.Cell(rowNumber, 2).Value = product.Name;
            sheet.Cell(rowNumber, 3).Value = product.QuantitySold;
            sheet.Cell(rowNumber, 4).Value = product.LineRevenue;
            rowNumber++;
        }

        FinishSheet(sheet, 4, rowNumber - 1, [18, 38, 22, 24], [4]);
        sheet.Column(3).Style.NumberFormat.Format = "0.###";
    }

    private static IXLWorksheet CreateSheet(XLWorkbook workbook, string name, string title, string subtitle,
        string[] headers)
    {
        var sheet = workbook.Worksheets.Add(name);
        sheet.Cell(1, 1).Value = title;
        sheet.Range(1, 1, 1, headers.Length).Merge();
        sheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, headers.Length).Style.Font.FontSize = 16;
        sheet.Range(1, 1, 1, headers.Length).Style.Font.FontColor = XLColor.White;
        sheet.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#176B50");
        sheet.Cell(2, 1).Value = subtitle;
        sheet.Range(2, 1, 2, headers.Length).Merge();
        sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.FromHtml("#52616B");

        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(4, column + 1).Value = headers[column];
        sheet.Range(4, 1, 4, headers.Length).Style.Font.Bold = true;
        sheet.Range(4, 1, 4, headers.Length).Style.Font.FontColor = XLColor.White;
        sheet.Range(4, 1, 4, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#297E63");
        sheet.Range(4, 1, 4, headers.Length).Style.Alignment.WrapText = true;
        return sheet;
    }

    private static void FinishSheet(IXLWorksheet sheet, int headerRow, int lastRow, double[] widths,
        int[]? currencyColumns = null)
    {
        for (var column = 0; column < widths.Length; column++)
            sheet.Column(column + 1).Width = widths[column];
        sheet.SheetView.FreezeRows(headerRow);
        sheet.Range(headerRow, 1, Math.Max(headerRow, lastRow), widths.Length).SetAutoFilter();
        sheet.Range(headerRow + 1, 1, Math.Max(headerRow + 1, lastRow), widths.Length)
            .Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        if (currencyColumns is not null)
        {
            foreach (var column in currencyColumns)
                sheet.Column(column).Style.NumberFormat.Format = "#,##0.00\" ₫\"";
        }
    }

    private static string PaymentLabel(string paymentMethod) => paymentMethod switch
    {
        "Cash" => "Tiền mặt",
        "MoMo" => "MoMo",
        _ => paymentMethod
    };

    private static string StatusLabel(string status) => status switch
    {
        "Completed" => "Đã thanh toán",
        "AwaitingMoMo" => "Chờ mở thanh toán MoMo",
        "Pending" => "Chờ xác nhận thanh toán",
        "Cancelled" => "Đã hủy",
        "Failed" => "Thanh toán thất bại",
        _ => status
    };

    private sealed record RevenueDayRow(DateTime Date, int InvoiceCount, decimal Revenue);
    private sealed record InvoiceExportRow(string InvoiceNo, DateTime CreatedAt, string Customer, string Cashier,
        string PaymentMethod, string Status, decimal Subtotal, decimal DiscountAmount, decimal TotalAmount);
    private sealed record InventoryExportRow(string Sku, string Name, string Category, string Unit,
        decimal SalePrice, decimal QuantityOnHand, decimal ReorderLevel);
    private sealed record BestSellerExportRow(string Sku, string Name, decimal QuantitySold, decimal LineRevenue);
}
