CREATE TABLE [reg].[Audit]
(
	[Id] BIGINT NOT NULL PRIMARY KEY IDENTITY, 
    [Type] NVARCHAR(50) NOT NULL, 
    [Operation] NVARCHAR(50) NOT NULL, 
    [EntityId] INT NOT NULL, 
    [Reason] NVARCHAR(500) NOT NULL, 
    [CreationDate] DATETIME2 NOT NULL 
)
