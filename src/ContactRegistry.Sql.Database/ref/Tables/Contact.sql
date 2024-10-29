CREATE TABLE [ref].[Contact](
	[ContactFlagStatus] [int] NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[FirstName] [nvarchar](255) NOT NULL,
	[LastName] [nvarchar](255) NOT NULL,
	[IsCustomer] [bit] NOT NULL,
	[LandPhone] [nvarchar](255) NULL,
	[MobilePhone] [nvarchar](255) NULL,
	[JobDescription] [nvarchar](255) NULL,
	[OfficeId] [uniqueidentifier] NULL,
	[Operation] [nvarchar](20) NOT NULL
)
GO

