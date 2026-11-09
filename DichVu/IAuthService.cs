using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Service
{
    public interface IAuthService
    {
        Task<bool> IsSessionValidAsync(int userId, string? roleName, CancellationToken cancellationToken);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
        Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    }
}
