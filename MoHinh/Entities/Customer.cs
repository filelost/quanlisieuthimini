namespace QuanLySieuThi.Models.Entities
{
    public sealed class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    }
}
