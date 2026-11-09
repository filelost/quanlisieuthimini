/* Aligns existing role rows to the application roles without losing user references. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
USE [QuanLySieuThiDb];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @OldRoleId int, @TargetRoleId int;

/* Manager becomes Admin. */
SELECT @OldRoleId = RoleId FROM dbo.Roles WHERE Name = N'Manager';
SELECT @TargetRoleId = RoleId FROM dbo.Roles WHERE Name = N'Admin';
IF @OldRoleId IS NOT NULL
BEGIN
    IF @TargetRoleId IS NULL
        UPDATE dbo.Roles SET Name = N'Admin', Description = N'Quản trị toàn bộ hệ thống' WHERE RoleId = @OldRoleId;
    ELSE
    BEGIN
        UPDATE dbo.Users SET RoleId = @TargetRoleId WHERE RoleId = @OldRoleId;
        DELETE dbo.Roles WHERE RoleId = @OldRoleId;
    END
END

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'Admin')
    INSERT dbo.Roles(Name, Description) VALUES (N'Admin', N'Quản trị toàn bộ hệ thống');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'Cashier')
    INSERT dbo.Roles(Name, Description) VALUES (N'Cashier', N'Nhân viên bán hàng');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'StockStaff')
    INSERT dbo.Roles(Name, Description) VALUES (N'StockStaff', N'Nhân viên kho');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = N'Customer')
    INSERT dbo.Roles(Name, Description) VALUES (N'Customer', N'Khách hàng');

COMMIT TRANSACTION;
GO
