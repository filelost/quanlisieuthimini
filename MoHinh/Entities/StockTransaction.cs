namespace QuanLySieuThi.Models.Entities
{
    public sealed class StockTransaction
    {
        public long StockTransactionId { get; set; }
        public int ProductId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal QuantityChange { get; set; }
        public decimal QuantityBefore { get; set; }
        public decimal QuantityAfter { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public long? ReferenceId { get; set; }
        public string? SupplierDocumentNo { get; set; }
        public string? Reason { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Product Product { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
    }
}
