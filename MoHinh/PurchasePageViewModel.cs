using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Models
{
    public sealed record PurchasePageViewModel(
        PurchaseReceipt Receipt, IReadOnlyList<Product> Products, IReadOnlyDictionary<int, decimal> Received,
        IReadOnlyList<StockTransaction> Deliveries, IReadOnlyList<PurchaseShortageAction> ShortageActions);
}
