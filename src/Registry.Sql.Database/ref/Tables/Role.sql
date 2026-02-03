CREATE TABLE [ref].[Role](
    [EntityId]       UNIQUEIDENTIFIER   NOT NULL,
    [ContactEmail]   NVARCHAR(255)      NOT NULL,
    [AccountNumber]  NVARCHAR(50)       NOT NULL,
    [RoleFlagStatus] INT                NULL,
    [ContactFlagPortailFactures] BIT    NULL,
    [Description]    NVARCHAR(1000)     NULL,
    [OperationType]  NVARCHAR(20)       NOT NULL,
    [OperationDate]  DATETIME2		    NOT NULL,
    [ValidationDate] DATETIME2          NULL,
    [RoleSource]     VARCHAR(50)        NULL,
    [SubRole]        VARCHAR(50)        NULL, 
    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([EntityId] ASC),
    CONSTRAINT [CHK_RoleOperation] CHECK ([OperationType] = 'INSERT' OR [OperationType] = 'UPDATE' OR [OperationType] = 'DELETE')
)
GO
