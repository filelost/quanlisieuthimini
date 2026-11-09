using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Models;
using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.CanConnectAsync();

            var roles = await db.Roles.ToListAsync();
            await RenameOrMergeAsync(db, roles, "Manager", RoleNames.Admin);



            await EnsureRoleAsync(db, roles, RoleNames.Admin, "Quản trị toàn bộ hệ thống");
            await EnsureRoleAsync(db, roles, RoleNames.Cashier, "Nhân viên bán hàng");
            await EnsureRoleAsync(db, roles, RoleNames.StockStaff, "Nhân viên kho");
            await EnsureRoleAsync(db, roles, RoleNames.Customer, "Khách hàng");
            await db.SaveChangesAsync();

            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Đồ uống", Description = "Nước uống và sữa" },
                    new Category { Name = "Thực phẩm", Description = "Thực phẩm đóng gói và chế biến" },
                    new Category { Name = "Bánh kẹo", Description = "Bánh, kẹo và đồ ăn nhẹ" },
                    new Category { Name = "Gia dụng", Description = "Sản phẩm sử dụng trong gia đình" });
                await db.SaveChangesAsync();
            }

            var username = configuration["BootstrapAdmin:Username"]?.Trim();
            var password = configuration["BootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return;

            var adminRole = await db.Roles.SingleAsync(x => x.Name == RoleNames.Admin);
            if (await db.Users.AnyAsync(x => x.RoleId == adminRole.RoleId))
                return;

            var admin = new User { Username = username, RoleId = adminRole.RoleId, IsActive = true };
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        private static async Task RenameOrMergeAsync(ApplicationDbContext db, List<Role> roles, string oldName, string newName)
        {
            var oldRole = roles.FirstOrDefault(x => x.Name == oldName);
            if (oldRole is null) return;

            var target = roles.FirstOrDefault(x => x.Name == newName);
            if (target is null)
            {
                oldRole.Name = newName;
                oldRole.Description = newName switch
                {
                    RoleNames.Admin => "Quản trị toàn bộ hệ thống",

                    _ => oldRole.Description
                };
                return;
            }

            var users = await db.Users.Where(x => x.RoleId == oldRole.RoleId).ToListAsync();
            foreach (var user in users) user.RoleId = target.RoleId;
            db.Roles.Remove(oldRole);
            roles.Remove(oldRole);
        }

        private static async Task EnsureRoleAsync(ApplicationDbContext db, List<Role> roles, string name, string description)
        {
            if (roles.Any(x => x.Name == name)) return;
            var role = new Role { Name = name, Description = description, IsActive = true };
            db.Roles.Add(role);
            roles.Add(role);
            await Task.CompletedTask;
        }
    }
}
