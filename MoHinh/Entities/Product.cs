namespace QuanLySieuThi.Models.Entities
{
    public sealed class Product
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Unit { get; set; } = "cái";
        public decimal SalePrice { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Category Category { get; set; } = null!;
        public Inventory? Inventory { get; set; }
        public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
        public ICollection<SalesDetail> SalesDetails { get; set; } = new List<SalesDetail>();
        public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }
}
