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
        public PurchaseReceipt PurchaseReceipt { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
