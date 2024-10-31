CREATE TABLE [ref].[Role](
    [RoleId]         INT IDENTITY (1,1) NOT NULL,
    [ContactEmail]   NVARCHAR(255)      NOT NULL,
    [AccountNumber]  NVARCHAR(50)       NOT NULL,
    [RoleFlagStatus] INT                NOT NULL,
    [Description]    NVARCHAR(1000)     NULL,
    [OperationType]  NVARCHAR(20)       NOT NULL,
    [OperationDate]  DATETIME2		    NOT NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([RoleId] ASC),
    CONSTRAINT [CHK_RoleOperation] CHECK ([OperationType] = 'INSERT' OR [OperationType] = 'UPDATE' OR [OperationType] = 'DELETE')
)
GO
