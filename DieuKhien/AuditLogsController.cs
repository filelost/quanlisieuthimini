using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;

namespace QuanLySieuThi.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public sealed class AuditLogsController(ApplicationDbContext db) : Controller
{
    private const int PageSize = 50;

    private static readonly IReadOnlyDictionary<string, string> Actions = new Dictionary<string, string>
    {
        ["Product.Created"] = "Thêm sản phẩm",
        ["Product.Updated"] = "Sửa sản phẩm",
        ["Product.Deactivated"] = "Ngừng kinh doanh sản phẩm",
        ["Inventory.Adjusted"] = "Điều chỉnh tồn kho",
        ["Invoice.Cancelled"] = "Hủy hóa đơn",
        ["Invoice.Discarded"] = "Bỏ hóa đơn MoMo",
        ["Employee.RoleChanged"] = "Đổi quyền nhân viên"
    };

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, string? action, string? search, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var vietnamOffset = TimeSpan.FromHours(7);
        var end = to ?? DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(vietnamOffset).DateTime);
        var start = from ?? end.AddDays(-Math.Min(29, end.DayNumber));
        if (!ModelState.IsValid || start == DateOnly.MinValue || end < start
            || end.DayNumber - start.DayNumber > 365 || end == DateOnly.MaxValue)
            return BadRequest("Khoảng ngày phải hợp lệ và tối đa 366 ngày.");

        action = Actions.ContainsKey(action ?? string.Empty) ? action : null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        page = Math.Max(1, page);
        var startUtc = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), vietnamOffset).UtcDateTime;
        var endUtc = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), vietnamOffset).UtcDateTime;
        var query = db.AuditLogs.AsNoTracking().Where(log => log.CreatedAt >= startUtc && log.CreatedAt < endUtc);
        if (action is not null)
            query = query.Where(log => log.ActionType == action);
        if (search is not null)
            query = query.Where(log => log.ActorUsername.Contains(search) || log.TargetName.Contains(search)
                || log.TargetId.Contains(search) || log.Details.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);
        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Min(page, pageCount);
        ViewBag.From = start.ToString("yyyy-MM-dd");
        ViewBag.To = end.ToString("yyyy-MM-dd");
        ViewBag.Action = action;
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageCount = pageCount;
        ViewBag.TotalCount = totalCount;
        ViewBag.Actions = Actions;
        return View(await query.OrderByDescending(log => log.CreatedAt).ThenByDescending(log => log.AuditLogId)
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken));
    }
}
