SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditLogId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        ActorUserId int NULL,
        ActorUsername nvarchar(60) NOT NULL,
        ActionType nvarchar(40) NOT NULL,
        TargetType nvarchar(40) NOT NULL,
        TargetId nvarchar(100) NOT NULL,
        TargetName nvarchar(180) NOT NULL,
        Details nvarchar(1000) NOT NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(UserId) ON DELETE SET NULL
    );
    CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt DESC, AuditLogId DESC);
    CREATE INDEX IX_AuditLogs_ActionType_CreatedAt ON dbo.AuditLogs(ActionType, CreatedAt DESC);
END;

COMMIT TRANSACTION;
