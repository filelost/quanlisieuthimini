namespace QuanLySieuThi.Models
{
    public sealed record CashierReportViewModel(string Period, DateOnly Date, DateTime Start, DateTime End,
        IReadOnlyList<CashierPaymentTotal> Payments, int PendingCount, decimal PendingAmount,
        IReadOnlyList<CashierReportInvoice> Invoices);

    public sealed record CashierPaymentTotal(string Method, int InvoiceCount, decimal Amount);

    public sealed record CashierReportInvoice(long Id, string Number, DateTime CreatedAt,
        string PaymentMethod, string Status, decimal Amount);
}
