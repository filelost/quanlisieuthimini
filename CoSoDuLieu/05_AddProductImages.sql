    IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductImages (
        ProductId int NOT NULL CONSTRAINT PK_ProductImages PRIMARY KEY,
        ContentType nvarchar(20) NOT NULL,
        Content varbinary(max) NOT NULL,
        CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId) ON DELETE CASCADE,
        CONSTRAINT CK_ProductImages_ContentType CHECK (ContentType IN (N'image/jpeg', N'image/png')),
        CONSTRAINT CK_ProductImages_Size CHECK (DATALENGTH(Content) BETWEEN 1 AND 2097152)
    );
END;
