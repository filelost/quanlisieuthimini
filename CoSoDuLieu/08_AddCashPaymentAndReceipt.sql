-- Nâng cấp cơ sở dữ liệu hiện có: lưu tiền khách đưa và tiền thừa cho hóa đơn.
IF COL_LENGTH(N'dbo.SalesInvoices', N'CashReceived') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoices
    ADD CashReceived decimal(18,2) NOT NULL
        CONSTRAINT DF_SalesInvoices_CashReceived DEFAULT (0);
END;
GO

IF COL_LENGTH(N'dbo.SalesInvoices', N'ChangeAmount') IS NULL
BEGIN
    ALTER TABLE dbo.SalesInvoices
    ADD ChangeAmount decimal(18,2) NOT NULL
        CONSTRAINT DF_SalesInvoices_ChangeAmount DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SalesInvoices_CashReceived')
    ALTER TABLE dbo.SalesInvoices WITH CHECK
    ADD CONSTRAINT CK_SalesInvoices_CashReceived CHECK (CashReceived >= 0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SalesInvoices_ChangeAmount')
    ALTER TABLE dbo.SalesInvoices WITH CHECK
    ADD CONSTRAINT CK_SalesInvoices_ChangeAmount CHECK (ChangeAmount >= 0);
GO

-- Hóa đơn được tạo trước khi có trường tiền khách đưa không lưu dữ liệu này.
-- Gán số tiền phải trả làm giá trị mặc định để hóa đơn cũ không hiển thị 0 ₫.
UPDATE dbo.SalesInvoices
SET CashReceived = TotalAmount, ChangeAmount = 0
WHERE CashReceived = 0 AND ChangeAmount = 0;
GO
