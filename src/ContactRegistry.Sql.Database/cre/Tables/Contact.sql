CREATE TABLE [cre].[Contact](
	[Id] [uniqueidentifier] NOT NULL,
	[OfficeId] [uniqueidentifier] NULL,
	[IsCustomer] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Updated] [datetime2](7) NULL,
	[Deleted] [datetime2](7) NULL,
	[FirstName] [nvarchar](255) NOT NULL,
	[LastName] [nvarchar](255) NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[LandPhone] [nvarchar](255) NULL,
	[MobilePhone] [nvarchar](255) NULL,
	[JobDescription] [nvarchar](255) NULL,
	[Source] [nvarchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

