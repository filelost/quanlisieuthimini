namespace QuanLySieuThi.Models.Entities
{
    public sealed class PurchaseReceipt
    {
        public int PurchaseReceiptId { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Draft";
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
        public ICollection<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
    }
}
