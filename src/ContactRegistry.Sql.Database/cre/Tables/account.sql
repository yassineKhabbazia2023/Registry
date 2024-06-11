CREATE TABLE [cre].[Account](
	[Id] [uniqueidentifier] NOT NULL,
	[Updated] [datetime] NULL,
	[LegalName] [nvarchar](255) NOT NULL,
	[AccountNumber] [nvarchar](50) NULL,
	[AccountFlagESCActif] [bit] NOT NULL,
	[Deleted] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

