CREATE TABLE [Audit].[AkuiteoContactSyncOperations]
(
    [Id] BIGINT NOT NULL IDENTITY,
    [SourceEventId] UNIQUEIDENTIFIER NOT NULL,
    [AccountId] INT NOT NULL,
    [AccountNumber] VARCHAR(100) NOT NULL,
    [AccountType] VARCHAR(20) NOT NULL,
    [ContactId] INT NOT NULL,
    [ContactEmail] VARCHAR(255) NOT NULL,
    [ContactFlagPortailFactures] BIT NULL,
    [IsSignatory] BIT NULL,
    [Reason] VARCHAR(50) NOT NULL,
    [Status] VARCHAR(20) NOT NULL CONSTRAINT [DF_AkuiteoContactSyncOperations_Status] DEFAULT ('PENDING'),
    [LastError] NVARCHAR(2000) NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AkuiteoContactSyncOperations_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AkuiteoContactSyncOperations_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [SentAt] DATETIME2 NULL,
    [AkuiteoContactId] NVARCHAR(100) NULL,
    CONSTRAINT [PK_AkuiteoContactSyncOperations] PRIMARY KEY CLUSTERED ([Id] ASC)
)

GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_AkuiteoContactSyncOperations_SourceEventId]
    ON [Audit].[AkuiteoContactSyncOperations] ([SourceEventId])

GO

CREATE NONCLUSTERED INDEX [IX_AkuiteoContactSyncOperations_Status_CreatedAt]
    ON [Audit].[AkuiteoContactSyncOperations] ([Status], [CreatedAt])
