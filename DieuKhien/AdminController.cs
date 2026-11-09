using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models;
using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController(ApplicationDbContext db, IPasswordHasher<User> passwordHasher) : Controller
    {
        public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var completedToday = db.SalesInvoices.Where(invoice => invoice.CreatedAt >= today
                && invoice.CreatedAt < tomorrow && invoice.Status == "Completed");
            var model = new AdminDashboardViewModel
            {
                ProductCount = await db.Products.CountAsync(x => x.IsActive, cancellationToken),
                EmployeeCount = await db.Employees.CountAsync(x => x.IsActive, cancellationToken),
                CustomerCount = await db.Customers.CountAsync(x => x.IsActive, cancellationToken),
                LowStockCount = await db.Products.CountAsync(product => product.IsActive
                    && (product.Inventory == null || product.Inventory.QuantityOnHand <= product.ReorderLevel), cancellationToken),
                TodayInvoiceCount = await completedToday.CountAsync(cancellationToken),
                TodayRevenue = await completedToday
                    .SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0,
                RecentInvoices = await db.SalesInvoices.AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.SalesInvoiceId).Take(8)
                    .Select(x => new RecentInvoiceViewModel
                    {
                        SalesInvoiceId = x.SalesInvoiceId,
                        InvoiceNo = x.InvoiceNo,
                        CreatedAt = x.CreatedAt,
                        CashierName = x.CashierUser.Employee == null ? x.CashierUser.Username : x.CashierUser.Employee.FullName,
                        CustomerName = x.Customer == null ? "Khách lẻ" : x.Customer.FullName,
                        TotalAmount = x.TotalAmount,
                        Status = x.Status
                    }).ToListAsync(cancellationToken)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Products(string? search, int? categoryId, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = db.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Inventory).AsQueryable();
            if (!includeInactive) query = query.Where(x => x.IsActive);
            if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Name.Contains(term) || x.Sku.Contains(term) || (x.Barcode != null && x.Barcode.Contains(term)));
            }

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.IncludeInactive = includeInactive;
            ViewBag.Categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
            var products = await query.OrderBy(x => x.Name).Select(x => new ProductListItemViewModel
            {
                ProductId = x.ProductId,
                Sku = x.Sku,
                Name = x.Name,
                CategoryName = x.Category.Name,
                SalePrice = x.SalePrice,
                QuantityOnHand = x.Inventory == null ? 0 : x.Inventory.QuantityOnHand,
                Unit = x.Unit,
                IsActive = x.IsActive,
                HasImage = x.Image != null
            }).ToListAsync(cancellationToken);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> ProductCreate(CancellationToken cancellationToken)
        {
            var model = new ProductFormViewModel();
            await LoadCategoriesAsync(model, cancellationToken);
            return View("ProductForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> ProductCreate(ProductFormViewModel model, CancellationToken cancellationToken)
        {
            await ValidateProductKeysAsync(model, null, cancellationToken);
            var image = await ReadProductImageAsync(model.ImageFile, cancellationToken);
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model, cancellationToken);
                return View("ProductForm", model);
            }

            var product = new Models.Entities.Product
            {
                Sku = model.Sku.Trim(),
                Barcode = Normalize(model.Barcode),
                Name = model.Name.Trim(),
                CategoryId = model.CategoryId,
                Unit = model.Unit.Trim(),
                SalePrice = model.SalePrice,
                ReorderLevel = model.ReorderLevel,
                IsActive = model.IsActive,
                Image = image
            };
            db.Products.Add(product);
            db.Inventory.Add(new Models.Entities.Inventory { Product = product, QuantityOnHand = 0, UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã thêm sản phẩm mới.";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> ProductEdit(int id, CancellationToken cancellationToken)
        {
            var product = await db.Products.FindAsync([id], cancellationToken);
            if (product is null) return NotFound();
            var model = new ProductFormViewModel
            {
                ProductId = product.ProductId,
                Sku = product.Sku,
                Barcode = product.Barcode,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Unit = product.Unit,
                SalePrice = product.SalePrice,
                ReorderLevel = product.ReorderLevel,
                IsActive = product.IsActive,
                HasImage = await db.ProductImages.AnyAsync(image => image.ProductId == id, cancellationToken)
            };
            await LoadCategoriesAsync(model, cancellationToken);
            return View("ProductForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> ProductEdit(int id, ProductFormViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.ProductId) return BadRequest();
            var product = await db.Products.Include(item => item.Image)
                .SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);
            if (product is null) return NotFound();
            await ValidateProductKeysAsync(model, id, cancellationToken);
            var image = await ReadProductImageAsync(model.ImageFile, cancellationToken);
            model.HasImage = product.Image is not null;
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(model, cancellationToken);
                return View("ProductForm", model);
            }

            product.Sku = model.Sku.Trim(); product.Barcode = Normalize(model.Barcode); product.Name = model.Name.Trim();
            product.CategoryId = model.CategoryId; product.Unit = model.Unit.Trim(); product.SalePrice = model.SalePrice;
            product.ReorderLevel = model.ReorderLevel; product.IsActive = model.IsActive;
            if (image is not null)
            {
                if (product.Image is null)
                    product.Image = image;
                else
                {
                    product.Image.Content = image.Content;
                    product.Image.ContentType = image.ContentType;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã cập nhật sản phẩm.";
            return RedirectToAction(nameof(Products));
        }

        private async Task<ProductImage?> ReadProductImageAsync(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null) return null;
            if (file.Length is <= 0 or > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(ProductFormViewModel.ImageFile), "Ảnh phải có dung lượng tối đa 2 MB.");
                return null;
            }

            using var buffer = new MemoryStream((int)file.Length);
            await file.CopyToAsync(buffer, cancellationToken);
            var content = buffer.ToArray();
            var contentType = content.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                ? "image/png"
                : content.AsSpan().StartsWith(new byte[] { 255, 216, 255 }) ? "image/jpeg" : null;
            if (contentType is null)
            {
                ModelState.AddModelError(nameof(ProductFormViewModel.ImageFile), "Chọn ảnh PNG hoặc JPEG hợp lệ.");
                return null;
            }

            return new ProductImage { Content = content, ContentType = contentType };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductDeactivate(int id, CancellationToken cancellationToken)
        {
            var product = await db.Products.FindAsync([id], cancellationToken);
            if (product is null) return NotFound();
            product.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Sản phẩm đã được ngừng kinh doanh; dữ liệu giao dịch được giữ nguyên.";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> Employees(CancellationToken cancellationToken)
        {
            var employees = await db.Employees.AsNoTracking().Include(x => x.User).OrderBy(x => x.FullName)
                .Select(x => new EmployeeAccountListItemViewModel
                {
                    EmployeeId = x.EmployeeId,
                    EmployeeCode = x.EmployeeCode,
                    FullName = x.FullName,
                    Phone = x.Phone,
                    Position = x.Position,
                    Username = x.User == null ? null : x.User.Username,
                    RoleName = x.User == null ? null : x.User.Role.Name,
                    IsActive = x.IsActive && (x.User == null || x.User.IsActive)
                }).ToListAsync(cancellationToken);
            return View(employees);
        }

        [HttpGet]
        public IActionResult EmployeeCreate() => View(new EmployeeAccountFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeCreate(EmployeeAccountFormViewModel model, CancellationToken cancellationToken)
        {
            if (!RoleNames.IsStaffRole(model.RoleName))
                ModelState.AddModelError(nameof(model.RoleName), "Chọn vai trò bán hàng hoặc kho.");
            if (!ModelState.IsValid) return View(model);

            var employeeCode = model.EmployeeCode.Trim();
            var username = model.Username.Trim();
            if (await db.Employees.AnyAsync(x => x.EmployeeCode == employeeCode, cancellationToken))
                ModelState.AddModelError(nameof(model.EmployeeCode), "Mã nhân viên đã tồn tại.");
            if (await db.Users.AnyAsync(x => x.Username == username, cancellationToken))
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã được sử dụng.");
            if (!ModelState.IsValid) return View(model);

            var role = await db.Roles.SingleAsync(x => x.Name == model.RoleName, cancellationToken);
            var employee = new Employee
            {
                EmployeeCode = employeeCode,
                FullName = model.FullName.Trim(),
                Phone = Normalize(model.Phone),
                Email = Normalize(model.Email),
                Position = Normalize(model.Position),
                IsActive = true
            };
            var user = new User { Username = username, RoleId = role.RoleId, Employee = employee, IsActive = true };
            user.PasswordHash = passwordHasher.HashPassword(user, model.Password);
            db.Employees.Add(employee);
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã tạo hồ sơ nhân viên và tài khoản đăng nhập.";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeChangeRole(int id, string roleName, CancellationToken cancellationToken)
        {
            if (!RoleNames.IsStaffRole(roleName))
                return BadRequest();

            var user = await db.Users.Include(user => user.Role).SingleOrDefaultAsync(
                user => user.EmployeeId == id && (user.Role.Name == RoleNames.Employee
                    || user.Role.Name == RoleNames.Cashier || user.Role.Name == RoleNames.StockStaff), cancellationToken);
            if (user is null)
                return NotFound();

            user.Role = await db.Roles.SingleAsync(role => role.Name == roleName, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã cập nhật vai trò nhân viên.";
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeDeactivate(int id, CancellationToken cancellationToken)
        {
            var employee = await db.Employees.Include(x => x.User).FirstOrDefaultAsync(x => x.EmployeeId == id, cancellationToken);
            if (employee is null) return NotFound();
            employee.IsActive = false;
            if (employee.User is not null) employee.User.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã ngừng hoạt động nhân viên và khóa tài khoản; lịch sử được giữ nguyên.";
            return RedirectToAction(nameof(Employees));
        }

        [HttpGet]
        public async Task<IActionResult> Suppliers(string? search, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = db.Suppliers.AsNoTracking().AsQueryable();
            if (!includeInactive) query = query.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Name.Contains(term) || x.SupplierCode.Contains(term) || (x.Phone != null && x.Phone.Contains(term)));
            }
            ViewBag.Search = search;
            ViewBag.IncludeInactive = includeInactive;
            var suppliers = await query.OrderBy(x => x.Name).Select(x => new SupplierListItemViewModel
            {
                SupplierId = x.SupplierId,
                SupplierCode = x.SupplierCode,
                Name = x.Name,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                IsActive = x.IsActive,
                PurchaseCount = x.PurchaseReceipts.Count
            }).ToListAsync(cancellationToken);
            return View(suppliers);
        }

        [HttpGet]
        public IActionResult SupplierCreate() => View("SupplierForm", new SupplierFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupplierCreate(SupplierFormViewModel model, CancellationToken cancellationToken)
        {
            var code = model.SupplierCode.Trim();
            if (await db.Suppliers.AnyAsync(x => x.SupplierCode == code, cancellationToken))
                ModelState.AddModelError(nameof(model.SupplierCode), "Mã nhà cung cấp đã tồn tại.");
            if (!ModelState.IsValid) return View("SupplierForm", model);
            db.Suppliers.Add(new Supplier
            {
                SupplierCode = code,
                Name = model.Name.Trim(),
                Phone = Normalize(model.Phone),
                Email = Normalize(model.Email),
                Address = Normalize(model.Address),
                IsActive = true
            });
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã thêm nhà cung cấp.";
            return RedirectToAction(nameof(Suppliers));
        }

        [HttpGet]
        public async Task<IActionResult> SupplierEdit(int id, CancellationToken cancellationToken)
        {
            var supplier = await db.Suppliers.FindAsync([id], cancellationToken);
            if (supplier is null) return NotFound();
            return View("SupplierForm", new SupplierFormViewModel
            {
                SupplierId = supplier.SupplierId,
                SupplierCode = supplier.SupplierCode,
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                IsActive = supplier.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupplierEdit(int id, SupplierFormViewModel model, CancellationToken cancellationToken)
        {
            if (id != model.SupplierId) return BadRequest();
            var supplier = await db.Suppliers.FindAsync([id], cancellationToken);
            if (supplier is null) return NotFound();
            var code = model.SupplierCode.Trim();
            if (await db.Suppliers.AnyAsync(x => x.SupplierCode == code && x.SupplierId != id, cancellationToken))
                ModelState.AddModelError(nameof(model.SupplierCode), "Mã nhà cung cấp đã tồn tại.");
            if (!ModelState.IsValid) return View("SupplierForm", model);
            supplier.SupplierCode = code; supplier.Name = model.Name.Trim(); supplier.Phone = Normalize(model.Phone);
            supplier.Email = Normalize(model.Email); supplier.Address = Normalize(model.Address); supplier.IsActive = model.IsActive;
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã cập nhật nhà cung cấp.";
            return RedirectToAction(nameof(Suppliers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SupplierDeactivate(int id, CancellationToken cancellationToken)
        {
            var supplier = await db.Suppliers.FindAsync([id], cancellationToken);
            if (supplier is null) return NotFound();
            supplier.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            TempData["Success"] = "Đã ngừng hoạt động nhà cung cấp; lịch sử nhập hàng được giữ nguyên.";
            return RedirectToAction(nameof(Suppliers));
        }

        private async Task LoadCategoriesAsync(ProductFormViewModel model, CancellationToken cancellationToken)
        {
            model.Categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.CategoryId.ToString(), x.CategoryId == model.CategoryId)).ToListAsync(cancellationToken);
        }

        private async Task ValidateProductKeysAsync(ProductFormViewModel model, int? exceptId, CancellationToken cancellationToken)
        {
            var sku = model.Sku.Trim();
            var duplicateSku = await db.Products.AnyAsync(x => x.Sku == sku && (!exceptId.HasValue || x.ProductId != exceptId.Value), cancellationToken);
            if (duplicateSku) ModelState.AddModelError(nameof(model.Sku), "Mã sản phẩm đã tồn tại.");
            var barcode = Normalize(model.Barcode);
            if (barcode is not null && await db.Products.AnyAsync(x => x.Barcode == barcode && (!exceptId.HasValue || x.ProductId != exceptId.Value), cancellationToken))
                ModelState.AddModelError(nameof(model.Barcode), "Mã vạch đã được sử dụng.");
            if (!await db.Categories.AnyAsync(x => x.CategoryId == model.CategoryId && x.IsActive, cancellationToken))
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không tồn tại hoặc đã ngừng hoạt động.");
        }

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
