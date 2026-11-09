namespace QuanLySieuThi.Models.Entities
{
    public sealed class Inventory
    {
        public int ProductId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public byte[] RowVersion { get; set; } = [];
        public Product Product { get; set; } = null!;
    }
}
