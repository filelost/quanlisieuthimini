/* Run with SQL Server Management Studio or sqlcmd. Creates the initial schema. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF DB_ID(N'QuanLySieuThiDb') IS NULL
    CREATE DATABASE [QuanLySieuThiDb];
GO
USE [QuanLySieuThiDb];
GO

CREATE TABLE dbo.Roles (
    RoleId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Name nvarchar(50) NOT NULL,
    Description nvarchar(200) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1)
);
CREATE UNIQUE INDEX UX_Roles_Name ON dbo.Roles(Name);

CREATE TABLE dbo.Employees (
    EmployeeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employees PRIMARY KEY,
    EmployeeCode nvarchar(30) NOT NULL,
    FullName nvarchar(150) NOT NULL,
    Phone nvarchar(30) NULL,
    Email nvarchar(150) NULL,
    Position nvarchar(80) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE UNIQUE INDEX UX_Employees_EmployeeCode ON dbo.Employees(EmployeeCode);

CREATE TABLE dbo.Users (
    UserId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username nvarchar(60) NOT NULL,
    PasswordHash nvarchar(500) NOT NULL,
    RoleId int NOT NULL,
    EmployeeId int NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    LastLoginAt datetime2 NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
    CONSTRAINT FK_Users_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId)
);
CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
CREATE UNIQUE INDEX UX_Users_EmployeeId ON dbo.Users(EmployeeId) WHERE EmployeeId IS NOT NULL;

CREATE TABLE dbo.Categories (
    CategoryId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(300) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1)
);
CREATE UNIQUE INDEX UX_Categories_Name ON dbo.Categories(Name);

CREATE TABLE dbo.Products (
    ProductId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Sku nvarchar(50) NOT NULL,
    Barcode nvarchar(80) NULL,
    Name nvarchar(180) NOT NULL,
    CategoryId int NOT NULL,
    Unit nvarchar(30) NOT NULL,
    SalePrice decimal(18,2) NOT NULL,
    ReorderLevel decimal(18,3) NOT NULL CONSTRAINT DF_Products_ReorderLevel DEFAULT (0),
    IsActive bit NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_Products_SalePrice CHECK (SalePrice >= 0),
    CONSTRAINT CK_Products_ReorderLevel CHECK (ReorderLevel >= 0),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
);
CREATE UNIQUE INDEX UX_Products_Sku ON dbo.Products(Sku);
CREATE UNIQUE INDEX UX_Products_Barcode ON dbo.Products(Barcode) WHERE Barcode IS NOT NULL;

CREATE TABLE dbo.ProductImages (
    ProductId int NOT NULL CONSTRAINT PK_ProductImages PRIMARY KEY,
    ContentType nvarchar(20) NOT NULL,
    Content varbinary(max) NOT NULL,
    CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId) ON DELETE CASCADE,
    CONSTRAINT CK_ProductImages_ContentType CHECK (ContentType IN (N'image/jpeg', N'image/png')),
    CONSTRAINT CK_ProductImages_Size CHECK (DATALENGTH(Content) BETWEEN 1 AND 2097152)
);

CREATE TABLE dbo.Suppliers (
    SupplierId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    SupplierCode nvarchar(30) NOT NULL,
    Name nvarchar(180) NOT NULL,
    Phone nvarchar(30) NULL,
    Email nvarchar(150) NULL,
    Address nvarchar(300) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1)
);
CREATE UNIQUE INDEX UX_Suppliers_SupplierCode ON dbo.Suppliers(SupplierCode);

CREATE TABLE dbo.PurchaseReceipts (
    PurchaseReceiptId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseReceipts PRIMARY KEY,
    ReceiptNo nvarchar(40) NOT NULL,
    SupplierId int NOT NULL,
    CreatedByUserId int NOT NULL,
    ReceivedAt datetime2 NOT NULL CONSTRAINT DF_PurchaseReceipts_ReceivedAt DEFAULT (SYSUTCDATETIME()),
    Status nvarchar(20) NOT NULL CONSTRAINT DF_PurchaseReceipts_Status DEFAULT (N'Draft'),
    TotalAmount decimal(18,2) NOT NULL CONSTRAINT DF_PurchaseReceipts_TotalAmount DEFAULT (0),
    Note nvarchar(500) NULL,
    CONSTRAINT CK_PurchaseReceipts_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT FK_PurchaseReceipts_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(SupplierId),
    CONSTRAINT FK_PurchaseReceipts_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId)
);
CREATE UNIQUE INDEX UX_PurchaseReceipts_ReceiptNo ON dbo.PurchaseReceipts(ReceiptNo);
CREATE INDEX IX_PurchaseReceipts_ReceivedAt_Status ON dbo.PurchaseReceipts(ReceivedAt, Status);

CREATE TABLE dbo.PurchaseDetails (
    PurchaseDetailId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseDetails PRIMARY KEY,
    PurchaseReceiptId int NOT NULL,
    ProductId int NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    UnitCost decimal(18,2) NOT NULL,
    LineTotal decimal(18,2) NOT NULL,
    CONSTRAINT CK_PurchaseDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_PurchaseDetails_UnitCost CHECK (UnitCost >= 0),
    CONSTRAINT FK_PurchaseDetails_PurchaseReceipts FOREIGN KEY (PurchaseReceiptId) REFERENCES dbo.PurchaseReceipts(PurchaseReceiptId) ON DELETE CASCADE,
    CONSTRAINT FK_PurchaseDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
);

CREATE TABLE dbo.PurchaseShortageActions (
    PurchaseShortageActionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseShortageActions PRIMARY KEY,
    PurchaseDetailId int NOT NULL,
    Action nvarchar(20) NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    Reason nvarchar(300) NOT NULL,
    SupplierReference nvarchar(150) NULL,
    CreatedByUserId int NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_PurchaseShortageActions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_PurchaseShortageActions_PurchaseDetails FOREIGN KEY (PurchaseDetailId) REFERENCES dbo.PurchaseDetails(PurchaseDetailId) ON DELETE CASCADE,
    CONSTRAINT FK_PurchaseShortageActions_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_PurchaseShortageActions_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_PurchaseShortageActions_Action CHECK (Action = N'FollowUp' OR (Action = N'Close' AND SupplierReference IS NOT NULL))
);
CREATE INDEX IX_PurchaseShortageActions_PurchaseDetailId ON dbo.PurchaseShortageActions(PurchaseDetailId);
CREATE UNIQUE INDEX UX_PurchaseShortageActions_Close ON dbo.PurchaseShortageActions(PurchaseDetailId) WHERE Action = N'Close';

CREATE TABLE dbo.Inventory (
    ProductId int NOT NULL CONSTRAINT PK_Inventory PRIMARY KEY,
    QuantityOnHand decimal(18,3) NOT NULL CONSTRAINT DF_Inventory_Quantity DEFAULT (0),
    UpdatedAt datetime2 NOT NULL CONSTRAINT DF_Inventory_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    RowVersion rowversion NOT NULL,
    CONSTRAINT CK_Inventory_QuantityOnHand CHECK (QuantityOnHand >= 0),
    CONSTRAINT FK_Inventory_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
);

CREATE TABLE dbo.StockTransactions (
    StockTransactionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransactions PRIMARY KEY,
    ProductId int NOT NULL,
    Type nvarchar(20) NOT NULL,
    QuantityChange decimal(18,3) NOT NULL,
    QuantityBefore decimal(18,3) NOT NULL,
    QuantityAfter decimal(18,3) NOT NULL,
    ReferenceType nvarchar(30) NOT NULL,
    ReferenceId bigint NULL,
    Reason nvarchar(300) NULL,
    CreatedByUserId int NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_StockTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_StockTransactions_QuantityChange CHECK (QuantityChange <> 0),
    CONSTRAINT CK_StockTransactions_Balance CHECK (QuantityBefore >= 0 AND QuantityAfter >= 0),
    CONSTRAINT FK_StockTransactions_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
    CONSTRAINT FK_StockTransactions_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId)
);
CREATE INDEX IX_StockTransactions_ProductId_CreatedAt ON dbo.StockTransactions(ProductId, CreatedAt);

CREATE TABLE dbo.Customers (
    CustomerId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerCode nvarchar(30) NOT NULL,
    FullName nvarchar(150) NOT NULL,
    Phone nvarchar(30) NULL,
    Email nvarchar(150) NULL,
    Address nvarchar(300) NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    IsActive bit NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1)
);
CREATE UNIQUE INDEX UX_Customers_CustomerCode ON dbo.Customers(CustomerCode);
CREATE INDEX IX_Customers_Phone ON dbo.Customers(Phone);

CREATE TABLE dbo.SalesInvoices (
    SalesInvoiceId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoices PRIMARY KEY,
    InvoiceNo nvarchar(40) NOT NULL,
    CustomerId int NULL,
    CashierUserId int NOT NULL,
    CreatedAt datetime2 NOT NULL CONSTRAINT DF_SalesInvoices_CreatedAt DEFAULT (SYSUTCDATETIME()),
    Status nvarchar(20) NOT NULL CONSTRAINT DF_SalesInvoices_Status DEFAULT (N'Completed'),
    Subtotal decimal(18,2) NOT NULL,
    DiscountAmount decimal(18,2) NOT NULL CONSTRAINT DF_SalesInvoices_DiscountAmount DEFAULT (0),
    TotalAmount decimal(18,2) NOT NULL,
    PaymentMethod nvarchar(30) NOT NULL,
    Note nvarchar(500) NULL,
    CONSTRAINT CK_SalesInvoices_Subtotal CHECK (Subtotal >= 0),
    CONSTRAINT CK_SalesInvoices_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_SalesInvoices_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT FK_SalesInvoices_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
    CONSTRAINT FK_SalesInvoices_Users FOREIGN KEY (CashierUserId) REFERENCES dbo.Users(UserId)
);
CREATE UNIQUE INDEX UX_SalesInvoices_InvoiceNo ON dbo.SalesInvoices(InvoiceNo);
CREATE INDEX IX_SalesInvoices_CreatedAt_Status ON dbo.SalesInvoices(CreatedAt, Status);

CREATE TABLE dbo.SalesDetails (
    SalesDetailId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesDetails PRIMARY KEY,
    SalesInvoiceId bigint NOT NULL,
    ProductId int NOT NULL,
    ProductNameSnapshot nvarchar(180) NOT NULL,
    SkuSnapshot nvarchar(50) NOT NULL,
    Quantity decimal(18,3) NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    DiscountAmount decimal(18,2) NOT NULL CONSTRAINT DF_SalesDetails_Discount DEFAULT (0),
    LineTotal decimal(18,2) NOT NULL,
    CONSTRAINT CK_SalesDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_SalesDetails_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_SalesDetails_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT FK_SalesDetails_SalesInvoices FOREIGN KEY (SalesInvoiceId) REFERENCES dbo.SalesInvoices(SalesInvoiceId) ON DELETE CASCADE,
    CONSTRAINT FK_SalesDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
);

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'Manager')
    INSERT dbo.Roles(Name, Description) VALUES (N'Manager', N'Quản lý hệ thống và báo cáo');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'Cashier')
    INSERT dbo.Roles(Name, Description) VALUES (N'Cashier', N'Thu ngân và lập hóa đơn');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'StockStaff')
    INSERT dbo.Roles(Name, Description) VALUES (N'StockStaff', N'Nhân viên kho và kiểm kê');
GO
