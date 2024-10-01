CREATE TABLE [cre].[Operations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Operation] [varchar](10) NOT NULL,
	[Type] [varchar](10) NULL,
	[PublishedAt] [datetime2](7) NULL,
	[EntityId] [uniqueidentifier] NOT NULL,
	[Status] NVARCHAR(20) NULL, 
    [LastStatusUpdatedDate] DATETIME2 NULL, 
    [LastStatusUpdatedBy] INT NULL, 
    [CreationDate] DATETIME2 NULL, 
    PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

