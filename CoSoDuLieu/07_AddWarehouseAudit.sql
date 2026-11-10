SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.PurchaseReceipts', N'CreatedAt') IS NULL
    ALTER TABLE dbo.PurchaseReceipts ADD CreatedAt datetime2 NULL;

IF COL_LENGTH(N'dbo.PurchaseDetails', N'CreatedAt') IS NULL
    ALTER TABLE dbo.PurchaseDetails ADD CreatedAt datetime2 NULL;

IF COL_LENGTH(N'dbo.PurchaseDetails', N'CreatedByUserId') IS NULL
    ALTER TABLE dbo.PurchaseDetails ADD CreatedByUserId int NULL
        CONSTRAINT FK_PurchaseDetails_Users REFERENCES dbo.Users(UserId);

IF COL_LENGTH(N'dbo.StockTransactions', N'SupplierDocumentNo') IS NULL
    ALTER TABLE dbo.StockTransactions ADD SupplierDocumentNo nvarchar(150) NULL;

COMMIT TRANSACTION;
