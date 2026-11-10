SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.SalesInvoices', N'CashReceived') IS NULL
    ALTER TABLE dbo.SalesInvoices ADD CashReceived decimal(18,2) NULL
        CONSTRAINT CK_SalesInvoices_CashReceived CHECK (CashReceived >= 0);

ELSE IF COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesInvoices'), N'CashReceived', 'AllowsNull') = 0
    ALTER TABLE dbo.SalesInvoices ALTER COLUMN CashReceived decimal(18,2) NULL;

IF COL_LENGTH(N'dbo.SalesInvoices', N'ChangeAmount') IS NULL
    ALTER TABLE dbo.SalesInvoices ADD ChangeAmount decimal(18,2) NULL
        CONSTRAINT CK_SalesInvoices_ChangeAmount CHECK (ChangeAmount >= 0);

ELSE IF COLUMNPROPERTY(OBJECT_ID(N'dbo.SalesInvoices'), N'ChangeAmount', 'AllowsNull') = 0
    ALTER TABLE dbo.SalesInvoices ALTER COLUMN ChangeAmount decimal(18,2) NULL;

COMMIT TRANSACTION;
