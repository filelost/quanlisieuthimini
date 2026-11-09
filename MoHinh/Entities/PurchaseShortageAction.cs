namespace QuanLySieuThi.Models.Entities
{
    public sealed class PurchaseShortageAction
    {
        public int PurchaseShortageActionId { get; set; }
        public int PurchaseDetailId { get; set; }
        public string Action { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? SupplierReference { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public PurchaseDetail PurchaseDetail { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
    }
}
