using Microsoft.EntityFrameworkCore;
using QuanLySieuThi.Models.Entities;

namespace QuanLySieuThi.Data
{
    public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
        public DbSet<PurchaseDetail> PurchaseDetails => Set<PurchaseDetail>();
        public DbSet<Inventory> Inventory => Set<Inventory>();
        public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
        public DbSet<SalesDetail> SalesDetails => Set<SalesDetail>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("Roles"); e.HasKey(x => x.RoleId);
                e.Property(x => x.Name).HasMaxLength(50).IsRequired();
                e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Description).HasMaxLength(200);
            });
            modelBuilder.Entity<Employee>(e =>
            {
                e.ToTable("Employees"); e.HasKey(x => x.EmployeeId);
                e.Property(x => x.EmployeeCode).HasMaxLength(30).IsRequired();
                e.HasIndex(x => x.EmployeeCode).IsUnique();
                e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
                e.Property(x => x.Phone).HasMaxLength(30);
                e.Property(x => x.Email).HasMaxLength(150);
                e.Property(x => x.Position).HasMaxLength(80);
            });
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users"); e.HasKey(x => x.UserId);
                e.Property(x => x.Username).HasMaxLength(60).IsRequired();
                e.HasIndex(x => x.Username).IsUnique();
                e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
                e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Employee).WithOne(x => x.User).HasForeignKey<User>(x => x.EmployeeId)
                    .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("Categories"); e.HasKey(x => x.CategoryId);
                e.Property(x => x.Name).HasMaxLength(100).IsRequired(); e.HasIndex(x => x.Name).IsUnique();
                e.Property(x => x.Description).HasMaxLength(300);
            });
            modelBuilder.Entity<Product>(e =>
            {
                e.ToTable("Products", t =>
                {
                    t.HasCheckConstraint("CK_Products_SalePrice", "[SalePrice] >= 0");
                    t.HasCheckConstraint("CK_Products_ReorderLevel", "[ReorderLevel] >= 0");
                });
                e.HasKey(x => x.ProductId); e.Property(x => x.Sku).HasMaxLength(50).IsRequired();
                e.HasIndex(x => x.Sku).IsUnique(); e.Property(x => x.Barcode).HasMaxLength(80);
                e.HasIndex(x => x.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
                e.Property(x => x.Name).HasMaxLength(180).IsRequired(); e.Property(x => x.Unit).HasMaxLength(30).IsRequired();
                e.Property(x => x.SalePrice).HasPrecision(18, 2); e.Property(x => x.ReorderLevel).HasPrecision(18, 3);
                e.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ProductImage>(e =>
            {
                e.ToTable("ProductImages");
                e.HasKey(x => x.ProductId);
                e.Property(x => x.ContentType).HasMaxLength(20);
                e.HasOne<Product>().WithOne(product => product.Image)
                    .HasForeignKey<ProductImage>(image => image.ProductId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Supplier>(e =>
            {
                e.ToTable("Suppliers"); e.HasKey(x => x.SupplierId);
                e.Property(x => x.SupplierCode).HasMaxLength(30).IsRequired(); e.HasIndex(x => x.SupplierCode).IsUnique();
                e.Property(x => x.Name).HasMaxLength(180).IsRequired(); e.Property(x => x.Phone).HasMaxLength(30);
                e.Property(x => x.Email).HasMaxLength(150); e.Property(x => x.Address).HasMaxLength(300);
            });
            modelBuilder.Entity<PurchaseReceipt>(e =>
            {
                e.ToTable("PurchaseReceipts", t => t.HasCheckConstraint("CK_PurchaseReceipts_TotalAmount", "[TotalAmount] >= 0"));
                e.HasKey(x => x.PurchaseReceiptId); e.Property(x => x.ReceiptNo).HasMaxLength(40).IsRequired();
                e.HasIndex(x => x.ReceiptNo).IsUnique(); e.Property(x => x.Status).HasMaxLength(20).IsRequired();
                e.Property(x => x.TotalAmount).HasPrecision(18, 2); e.Property(x => x.Note).HasMaxLength(500);
                e.HasIndex(x => new { x.ReceivedAt, x.Status });
                e.HasOne(x => x.Supplier).WithMany(x => x.PurchaseReceipts).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PurchaseDetail>(e =>
            {
                e.ToTable("PurchaseDetails", t =>
                {
                    t.HasCheckConstraint("CK_PurchaseDetails_Quantity", "[Quantity] > 0");
                    t.HasCheckConstraint("CK_PurchaseDetails_UnitCost", "[UnitCost] >= 0");
                });
                e.HasKey(x => x.PurchaseDetailId); e.Property(x => x.Quantity).HasPrecision(18, 3);
                e.Property(x => x.UnitCost).HasPrecision(18, 2); e.Property(x => x.LineTotal).HasPrecision(18, 2);
                e.HasOne(x => x.PurchaseReceipt).WithMany(x => x.Details).HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Product).WithMany(x => x.PurchaseDetails).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Inventory>(e =>
            {
                e.ToTable("Inventory", t => t.HasCheckConstraint("CK_Inventory_QuantityOnHand", "[QuantityOnHand] >= 0"));
                e.HasKey(x => x.ProductId); e.Property(x => x.QuantityOnHand).HasPrecision(18, 3);
                e.Property(x => x.RowVersion).IsRowVersion();
                e.HasOne(x => x.Product).WithOne(x => x.Inventory).HasForeignKey<Inventory>(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<StockTransaction>(e =>
            {
                e.ToTable("StockTransactions", t =>
                {
                    t.HasCheckConstraint("CK_StockTransactions_QuantityChange", "[QuantityChange] <> 0");
                    t.HasCheckConstraint("CK_StockTransactions_Balance", "[QuantityBefore] >= 0 AND [QuantityAfter] >= 0");
                });
                e.HasKey(x => x.StockTransactionId); e.Property(x => x.Type).HasMaxLength(20).IsRequired();
                e.Property(x => x.ReferenceType).HasMaxLength(30).IsRequired(); e.Property(x => x.Reason).HasMaxLength(300);
                e.Property(x => x.QuantityChange).HasPrecision(18, 3); e.Property(x => x.QuantityBefore).HasPrecision(18, 3);
                e.Property(x => x.QuantityAfter).HasPrecision(18, 3); e.HasIndex(x => new { x.ProductId, x.CreatedAt });
                e.HasOne(x => x.Product).WithMany(x => x.StockTransactions).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Customer>(e =>
            {
                e.ToTable("Customers"); e.HasKey(x => x.CustomerId);
                e.Property(x => x.CustomerCode).HasMaxLength(30).IsRequired(); e.HasIndex(x => x.CustomerCode).IsUnique();
                e.Property(x => x.FullName).HasMaxLength(150).IsRequired(); e.Property(x => x.Phone).HasMaxLength(30);
                e.Property(x => x.Email).HasMaxLength(150); e.Property(x => x.Address).HasMaxLength(300);
                e.HasIndex(x => x.Phone);
            });
            modelBuilder.Entity<SalesInvoice>(e =>
            {
                e.ToTable("SalesInvoices", t =>
                {
                    t.HasCheckConstraint("CK_SalesInvoices_Subtotal", "[Subtotal] >= 0");
                    t.HasCheckConstraint("CK_SalesInvoices_DiscountAmount", "[DiscountAmount] >= 0");
                    t.HasCheckConstraint("CK_SalesInvoices_TotalAmount", "[TotalAmount] >= 0");
                });
                e.HasKey(x => x.SalesInvoiceId); e.Property(x => x.InvoiceNo).HasMaxLength(40).IsRequired();
                e.HasIndex(x => x.InvoiceNo).IsUnique(); e.Property(x => x.Status).HasMaxLength(20).IsRequired();
                e.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired(); e.Property(x => x.Note).HasMaxLength(500);
                e.Property(x => x.Subtotal).HasPrecision(18, 2); e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                e.Property(x => x.TotalAmount).HasPrecision(18, 2); e.HasIndex(x => new { x.CreatedAt, x.Status });
                e.HasOne(x => x.Customer).WithMany(x => x.SalesInvoices).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.CashierUser).WithMany().HasForeignKey(x => x.CashierUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<SalesDetail>(e =>
            {
                e.ToTable("SalesDetails", t =>
                {
                    t.HasCheckConstraint("CK_SalesDetails_Quantity", "[Quantity] > 0");
                    t.HasCheckConstraint("CK_SalesDetails_UnitPrice", "[UnitPrice] >= 0");
                    t.HasCheckConstraint("CK_SalesDetails_DiscountAmount", "[DiscountAmount] >= 0");
                });
                e.HasKey(x => x.SalesDetailId); e.Property(x => x.ProductNameSnapshot).HasMaxLength(180).IsRequired();
                e.Property(x => x.SkuSnapshot).HasMaxLength(50).IsRequired(); e.Property(x => x.Quantity).HasPrecision(18, 3);
                e.Property(x => x.UnitPrice).HasPrecision(18, 2); e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                e.Property(x => x.LineTotal).HasPrecision(18, 2);
                e.HasOne(x => x.SalesInvoice).WithMany(x => x.Details).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Product).WithMany(x => x.SalesDetails).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
