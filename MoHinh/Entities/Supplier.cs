namespace QuanLySieuThi.Models.Entities
{
    public sealed class Supplier
    {
        public int SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = new List<PurchaseReceipt>();
    }
}
