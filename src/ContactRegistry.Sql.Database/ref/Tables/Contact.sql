CREATE TABLE [ref].[Contact](
	[ContactId]			INT IDENTITY (1,1) NOT NULL,
	[ContactFlagStatus] INT				   NOT NULL,
	[Email]				NVARCHAR(255)	   NOT NULL,
	[FirstName]			NVARCHAR(255)	   NOT NULL,
	[LastName]			NVARCHAR(255)	   NOT NULL,
	[IsCustomer]		BIT				   NOT NULL,
	[LandPhone]			NVARCHAR(255)	   NULL,
	[MobilePhone]		NVARCHAR(255)	   NULL,
	[JobDescription]	NVARCHAR(255)	   NULL,
	[OfficeId]			UNIQUEIDENTIFIER   NULL,
	[OperationType]		NVARCHAR(20)	   NOT NULL,
	[OperationDate]		DATETIME2		   NOT NULL,
	CONSTRAINT [PK_Contact] PRIMARY KEY CLUSTERED ([ContactId] ASC),
    CONSTRAINT [CHK_ContactOperation] CHECK ([OperationType] = 'INSERT' OR [OperationType] = 'UPDATE' OR [OperationType] = 'DELETE')
)
GO

