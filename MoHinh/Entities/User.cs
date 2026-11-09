namespace QuanLySieuThi.Models.Entities
{
    public sealed class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? EmployeeId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public Role Role { get; set; } = null!;
        public Employee? Employee { get; set; }
    }
}
