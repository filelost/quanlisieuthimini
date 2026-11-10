namespace QuanLySieuThi.Models.Entities
{
    public sealed class SalesInvoice
    {
        public long SalesInvoiceId { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public int CashierUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Completed";
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Note { get; set; }
        public Customer? Customer { get; set; }
        public User CashierUser { get; set; } = null!;
        public ICollection<SalesDetail> Details { get; set; } = new List<SalesDetail>();
    }
}
