namespace QuanLySieuThi.Models.Entities
{
    public sealed class Employee
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
    }
}
