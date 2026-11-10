SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.PurchaseShortageActions', N'U') IS NULL
BEGIN
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
END;
