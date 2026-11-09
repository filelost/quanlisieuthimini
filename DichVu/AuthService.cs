using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Data;
using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Service
{
    public sealed class AuthService(ApplicationDbContext db, IPasswordHasher<User> passwordHasher) : IAuthService
    {
        private IQueryable<User> ActiveAccounts() => db.Users.Where(user =>
            user.IsActive && user.Role.IsActive && (user.Employee == null || user.Employee.IsActive));

        public Task<bool> IsSessionValidAsync(int userId, string? roleName, CancellationToken cancellationToken)
            => ActiveAccounts().AnyAsync(user => user.UserId == userId && user.Role.Name == roleName, cancellationToken);

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword,
            CancellationToken cancellationToken)
        {
            var eligibleUsers = ActiveAccounts().Where(user => user.UserId == userId);
            var user = await eligibleUsers.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword)
                == PasswordVerificationResult.Failed)
                return false;

            var newHash = passwordHasher.HashPassword(user, newPassword);
            var updated = await eligibleUsers.Where(candidate => candidate.PasswordHash == user.PasswordHash)
                .ExecuteUpdateAsync(update => update.SetProperty(candidate => candidate.PasswordHash, newHash),
                    cancellationToken);
            return updated == 1;
        }

        public async Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var normalizedUsername = username.Trim();
            var user = await ActiveAccounts().Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);

            if (user is null)
                return null;

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return null;

            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return user;
        }
    }
}
