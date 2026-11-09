namespace QuanLySieuThi.Models.Entities
{
    public sealed class SalesDetail
    {
        public long SalesDetailId { get; set; }
        public long SalesInvoiceId { get; set; }
        public int ProductId { get; set; }
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public string SkuSnapshot { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
        public SalesInvoice SalesInvoice { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
