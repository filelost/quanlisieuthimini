namespace QuanLySieuThi.Models
{
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
        public const string Employee = "Employee";
        public const string Cashier = "Cashier";
        public const string StockStaff = "StockStaff";
        public const string SalesAccess = Admin + "," + Cashier;
        public const string StockAccess = Admin + "," + StockStaff;

        public static bool IsStaffRole(string? role) => role is Cashier or StockStaff;
    }
}
