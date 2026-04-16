CREATE TABLE [reg].[Operations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Operation] [varchar](10) NOT NULL,
	[Type] [varchar](10) NULL,
	[PublishedAt] [datetime2](7) NULL,
	[EntityId] [uniqueidentifier] NOT NULL,
	[ApprovalStatus] NVARCHAR(20) NULL,
	[ProcessStatus] NVARCHAR(20) NULL,
    [LastStatusApprovalDate] DATETIME2 NULL, 
    [LastStatusApprovalBy] [varchar](50) NULL, 
    [LastStatusProcessedDate] DATETIME2 NULL, 
    [CreationDate] DATETIME2 NULL,
	[CreatedBySystem] BIT NULL,
	[OldContactEmail] NVARCHAR(255) NULL,
    PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Operations_Type_Operation_ProcessStatus]
    ON [reg].[Operations] ([Operation], [Type], [EntityId], [ProcessStatus], [ApprovalStatus])
    INCLUDE ([PublishedAt], [CreationDate], [CreatedBySystem], [OldContactEmail], [LastStatusApprovalBy], [LastStatusApprovalDate], [LastStatusProcessedDate])
GO

