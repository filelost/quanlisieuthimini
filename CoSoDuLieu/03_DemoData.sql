-- Run after schema creation. Intended for demo databases.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Demo siêu thị')
    INSERT dbo.Categories (Name) VALUES (N'Demo siêu thị');

DECLARE @category int = (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Demo siêu thị');
INSERT dbo.Products (Sku, Name, CategoryId, Unit, SalePrice, ReorderLevel)
SELECT item.Sku, item.Name, @category, item.Unit, item.Price, 5
FROM (VALUES (N'DEMO-001', N'Nước suối 500 ml', N'Chai', 7000),
             (N'DEMO-002', N'Bánh quy bơ', N'Hộp', 25000)) item(Sku, Name, Unit, Price)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Sku = item.Sku);

INSERT dbo.Inventory (ProductId, QuantityOnHand)
SELECT ProductId, CASE WHEN Sku = N'DEMO-001' THEN 30 ELSE 4 END
FROM dbo.Products p WHERE Sku IN (N'DEMO-001', N'DEMO-002')
    AND NOT EXISTS (SELECT 1 FROM dbo.Inventory i WHERE i.ProductId = p.ProductId);

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE SupplierCode = N'DEMO-NCC')
    INSERT dbo.Suppliers (SupplierCode, Name) VALUES (N'DEMO-NCC', N'Nhà cung cấp mẫu');
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'DEMO-KH')
    INSERT dbo.Customers (CustomerCode, FullName, Phone) VALUES (N'DEMO-KH', N'Khách hàng mẫu', N'0900000000');
COMMIT;
