namespace QuanLySieuThi.Models
{
    public sealed class AdminDashboardViewModel
    {
        public int ProductCount { get; init; }
        public int EmployeeCount { get; init; }
        public int CustomerCount { get; init; }
        public int LowStockCount { get; init; }
        public int TodayInvoiceCount { get; init; }
        public decimal TodayRevenue { get; init; }
        public IReadOnlyList<RecentInvoiceViewModel> RecentInvoices { get; init; } = [];
    }

    public sealed class RecentInvoiceViewModel
    {
        public long SalesInvoiceId { get; init; }
        public string InvoiceNo { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public string CashierName { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
