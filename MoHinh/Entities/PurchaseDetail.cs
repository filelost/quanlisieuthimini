namespace QuanLySieuThi.Models.Entities
{
    public sealed class PurchaseDetail
    {
        public int PurchaseDetailId { get; set; }
        public int PurchaseReceiptId { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public PurchaseReceipt PurchaseReceipt { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
