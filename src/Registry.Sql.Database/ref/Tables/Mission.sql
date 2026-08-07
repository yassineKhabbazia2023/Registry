CREATE TABLE [ref].[Mission](
    [EntityId]       UNIQUEIDENTIFIER   NOT NULL,
    [AccountNumber]  NVARCHAR(50)       NOT NULL,
    [EngagementCode] NVARCHAR(50)       NOT NULL,
    [OfferCode]      NVARCHAR(100)      NOT NULL,
    [ProductCode]    NVARCHAR(100)      NULL,
    [StartDate]      DATE               NOT NULL,
    [EndDate]        DATE               NOT NULL,
    [OperationType]  NVARCHAR(20)       NOT NULL,
    [OperationDate]  DATETIME2          NOT NULL,
    [ValidationDate] DATETIME2          NULL,
    CONSTRAINT [PK_Mission] PRIMARY KEY CLUSTERED ([EntityId] ASC),
    CONSTRAINT [CHK_MissionOperation] CHECK ([OperationType] = 'INSERT' OR [OperationType] = 'UPDATE' OR [OperationType] = 'DELETE')
)
GO
CREATE NONCLUSTERED INDEX [IX_Mission_AccountNumber_EngagementCode]
    ON [ref].[Mission] ([AccountNumber], [EngagementCode])
GO
