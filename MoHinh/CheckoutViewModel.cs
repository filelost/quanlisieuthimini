using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Models
{
    public sealed class CheckoutInput
    {
        public Guid RequestId { get; set; }
        public int? CustomerId { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? PromotionCode { get; set; }
        public List<CheckoutLine> Lines { get; set; } = [];
    }

    public sealed class CheckoutLine
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }

    public sealed record CheckoutViewModel(Guid RequestId, IReadOnlyList<Product> Products,
        IReadOnlyList<Customer> Customers, IReadOnlyList<string> PromotionCodes, bool MomoEnabled,
        IReadOnlySet<int> ProductImageIds);
}
