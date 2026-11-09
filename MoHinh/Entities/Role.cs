namespace QuanLySieuThi.Models.Entities
{
    public sealed class Role
    {
        public int RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
