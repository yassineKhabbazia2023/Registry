CREATE TABLE [reg].[ProcessDeltaTrigger] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Account] BIT NOT NULL DEFAULT 0,
    [Role] BIT NOT NULL DEFAULT 0,
    [Contact] BIT NOT NULL DEFAULT 0
)
GO