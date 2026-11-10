-- Money is stored in hundredths of VND; quantities in thousandths of one unit.
-- Inventory RowVersion is an integer. Every inventory write must increment it.

CREATE TABLE Roles (
    RoleId INTEGER CONSTRAINT PK_Roles PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT NULL,
    IsActive INTEGER NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1)
) STRICT;
CREATE UNIQUE INDEX UX_Roles_Name ON Roles(Name);

CREATE TABLE Employees (
    EmployeeId INTEGER CONSTRAINT PK_Employees PRIMARY KEY AUTOINCREMENT,
    EmployeeCode TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Phone TEXT NULL,
    Email TEXT NULL,
    Position TEXT NULL,
    IsActive INTEGER NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT (1),
    CreatedAt TEXT NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
) STRICT;
CREATE UNIQUE INDEX UX_Employees_EmployeeCode ON Employees(EmployeeCode);

CREATE TABLE Users (
    UserId INTEGER CONSTRAINT PK_Users PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    RoleId INTEGER NOT NULL,
    EmployeeId INTEGER NULL,
    IsActive INTEGER NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt TEXT NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    LastLoginAt TEXT NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    CONSTRAINT FK_Users_Employees FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId)
) STRICT;
CREATE UNIQUE INDEX UX_Users_Username ON Users(Username);
CREATE UNIQUE INDEX UX_Users_EmployeeId ON Users(EmployeeId) WHERE EmployeeId IS NOT NULL;

CREATE TABLE Categories (
    CategoryId INTEGER CONSTRAINT PK_Categories PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT NULL,
    IsActive INTEGER NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1)
) STRICT;
CREATE UNIQUE INDEX UX_Categories_Name ON Categories(Name);

CREATE TABLE Products (
    ProductId INTEGER CONSTRAINT PK_Products PRIMARY KEY AUTOINCREMENT,
    Sku TEXT NOT NULL,
    Barcode TEXT NULL,
    Name TEXT NOT NULL,
    CategoryId INTEGER NOT NULL,
    Unit TEXT NOT NULL,
    SalePrice INTEGER NOT NULL,
    ReorderLevel INTEGER NOT NULL CONSTRAINT DF_Products_ReorderLevel DEFAULT (0),
    IsActive INTEGER NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CreatedAt TEXT NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    CONSTRAINT CK_Products_SalePrice CHECK (SalePrice >= 0),
    CONSTRAINT CK_Products_ReorderLevel CHECK (ReorderLevel >= 0),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
) STRICT;
CREATE UNIQUE INDEX UX_Products_Sku ON Products(Sku);
CREATE UNIQUE INDEX UX_Products_Barcode ON Products(Barcode) WHERE Barcode IS NOT NULL;

CREATE TABLE Suppliers (
    SupplierId INTEGER CONSTRAINT PK_Suppliers PRIMARY KEY AUTOINCREMENT,
    SupplierCode TEXT NOT NULL,
    Name TEXT NOT NULL,
    Phone TEXT NULL,
    Email TEXT NULL,
    Address TEXT NULL,
    IsActive INTEGER NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1)
) STRICT;
CREATE UNIQUE INDEX UX_Suppliers_SupplierCode ON Suppliers(SupplierCode);

CREATE TABLE PurchaseReceipts (
    PurchaseReceiptId INTEGER CONSTRAINT PK_PurchaseReceipts PRIMARY KEY AUTOINCREMENT,
    ReceiptNo TEXT NOT NULL,
    SupplierId INTEGER NOT NULL,
    CreatedByUserId INTEGER NOT NULL,
    ReceivedAt TEXT NOT NULL CONSTRAINT DF_PurchaseReceipts_ReceivedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    Status TEXT NOT NULL CONSTRAINT DF_PurchaseReceipts_Status DEFAULT ('Draft'),
    TotalAmount INTEGER NOT NULL CONSTRAINT DF_PurchaseReceipts_TotalAmount DEFAULT (0),
    Note TEXT NULL,
    CONSTRAINT CK_PurchaseReceipts_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT FK_PurchaseReceipts_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers(SupplierId),
    CONSTRAINT FK_PurchaseReceipts_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
) STRICT;
CREATE UNIQUE INDEX UX_PurchaseReceipts_ReceiptNo ON PurchaseReceipts(ReceiptNo);
CREATE INDEX IX_PurchaseReceipts_ReceivedAt_Status ON PurchaseReceipts(ReceivedAt, Status);

CREATE TABLE PurchaseDetails (
    PurchaseDetailId INTEGER CONSTRAINT PK_PurchaseDetails PRIMARY KEY AUTOINCREMENT,
    PurchaseReceiptId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitCost INTEGER NOT NULL,
    LineTotal INTEGER NOT NULL,
    CONSTRAINT CK_PurchaseDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_PurchaseDetails_UnitCost CHECK (UnitCost >= 0),
    CONSTRAINT FK_PurchaseDetails_PurchaseReceipts FOREIGN KEY (PurchaseReceiptId) REFERENCES PurchaseReceipts(PurchaseReceiptId) ON DELETE CASCADE,
    CONSTRAINT FK_PurchaseDetails_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
) STRICT;

CREATE TABLE Inventory (
    ProductId INTEGER NOT NULL CONSTRAINT PK_Inventory PRIMARY KEY,
    QuantityOnHand INTEGER NOT NULL CONSTRAINT DF_Inventory_Quantity DEFAULT (0),
    UpdatedAt TEXT NOT NULL CONSTRAINT DF_Inventory_UpdatedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    RowVersion INTEGER NOT NULL DEFAULT 0 CHECK (RowVersion >= 0),
    CONSTRAINT CK_Inventory_QuantityOnHand CHECK (QuantityOnHand >= 0),
    CONSTRAINT FK_Inventory_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
) STRICT;

CREATE TABLE StockTransactions (
    StockTransactionId INTEGER CONSTRAINT PK_StockTransactions PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    Type TEXT NOT NULL,
    QuantityChange INTEGER NOT NULL,
    QuantityBefore INTEGER NOT NULL,
    QuantityAfter INTEGER NOT NULL,
    ReferenceType TEXT NOT NULL,
    ReferenceId INTEGER NULL,
    Reason TEXT NULL,
    CreatedByUserId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL CONSTRAINT DF_StockTransactions_CreatedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    CONSTRAINT CK_StockTransactions_QuantityChange CHECK (QuantityChange <> 0),
    CONSTRAINT CK_StockTransactions_Balance CHECK (QuantityBefore >= 0 AND QuantityAfter >= 0),
    CONSTRAINT FK_StockTransactions_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    CONSTRAINT FK_StockTransactions_Users FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
) STRICT;
CREATE INDEX IX_StockTransactions_ProductId_CreatedAt ON StockTransactions(ProductId, CreatedAt);

CREATE TABLE Customers (
    CustomerId INTEGER CONSTRAINT PK_Customers PRIMARY KEY AUTOINCREMENT,
    CustomerCode TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Phone TEXT NULL,
    Email TEXT NULL,
    Address TEXT NULL,
    CreatedAt TEXT NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    IsActive INTEGER NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1)
) STRICT;
CREATE UNIQUE INDEX UX_Customers_CustomerCode ON Customers(CustomerCode);
CREATE INDEX IX_Customers_Phone ON Customers(Phone);

CREATE TABLE SalesInvoices (
    SalesInvoiceId INTEGER CONSTRAINT PK_SalesInvoices PRIMARY KEY AUTOINCREMENT,
    InvoiceNo TEXT NOT NULL,
    CustomerId INTEGER NULL,
    CashierUserId INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL CONSTRAINT DF_SalesInvoices_CreatedAt DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    Status TEXT NOT NULL CONSTRAINT DF_SalesInvoices_Status DEFAULT ('Completed'),
    Subtotal INTEGER NOT NULL,
    DiscountAmount INTEGER NOT NULL CONSTRAINT DF_SalesInvoices_DiscountAmount DEFAULT (0),
    TotalAmount INTEGER NOT NULL,
    PaymentMethod TEXT NOT NULL,
    Note TEXT NULL,
    CONSTRAINT CK_SalesInvoices_Subtotal CHECK (Subtotal >= 0),
    CONSTRAINT CK_SalesInvoices_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_SalesInvoices_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT FK_SalesInvoices_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
    CONSTRAINT FK_SalesInvoices_Users FOREIGN KEY (CashierUserId) REFERENCES Users(UserId)
) STRICT;
CREATE UNIQUE INDEX UX_SalesInvoices_InvoiceNo ON SalesInvoices(InvoiceNo);
CREATE INDEX IX_SalesInvoices_CreatedAt_Status ON SalesInvoices(CreatedAt, Status);

CREATE TABLE SalesDetails (
    SalesDetailId INTEGER CONSTRAINT PK_SalesDetails PRIMARY KEY AUTOINCREMENT,
    SalesInvoiceId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    ProductNameSnapshot TEXT NOT NULL,
    SkuSnapshot TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice INTEGER NOT NULL,
    DiscountAmount INTEGER NOT NULL CONSTRAINT DF_SalesDetails_Discount DEFAULT (0),
    LineTotal INTEGER NOT NULL,
    CONSTRAINT CK_SalesDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_SalesDetails_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_SalesDetails_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT FK_SalesDetails_SalesInvoices FOREIGN KEY (SalesInvoiceId) REFERENCES SalesInvoices(SalesInvoiceId) ON DELETE CASCADE,
    CONSTRAINT FK_SalesDetails_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
) STRICT;


INSERT INTO Roles (Name, Description) VALUES
    ('Admin', 'Quản trị toàn bộ hệ thống'),
    ('Cashier', 'Nhân viên bán hàng'),
    ('StockStaff', 'Nhân viên kho'),
    ('Customer', 'Khách hàng');

INSERT INTO Categories (Name, Description) VALUES
    ('Đồ uống', 'Nước uống và sữa'),
    ('Thực phẩm', 'Thực phẩm đóng gói và chế biến'),
    ('Bánh kẹo', 'Bánh, kẹo và đồ ăn nhẹ'),
    ('Gia dụng', 'Sản phẩm sử dụng trong gia đình');
