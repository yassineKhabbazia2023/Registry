CREATE TABLE [ref].[Contact](
	[EntityId]			UNIQUEIDENTIFIER   NOT NULL,
	[ContactFlagStatus] INT				   NULL,
	[Email]				NVARCHAR(255)	   NOT NULL,
	[FirstName]			NVARCHAR(255)	   NULL,
	[LastName]			NVARCHAR(255)	   NULL,
	[IsCustomer]		BIT				   NULL,
	[LandPhone]			NVARCHAR(255)	   NULL,
	[MobilePhone]		NVARCHAR(255)	   NULL,
	[JobDescription]	NVARCHAR(255)	   NULL,
	[OperationType]		NVARCHAR(20)	   NOT NULL,
	[OperationDate]		DATETIME2		   NOT NULL,
	[OfficeCode]		NVARCHAR(50) NULL, 
	[ValidationDate]	DATETIME2	 NULL,
    CONSTRAINT [PK_Contact] PRIMARY KEY CLUSTERED ([EntityId] ASC),
    CONSTRAINT [CHK_ContactOperation] CHECK ([OperationType] = 'INSERT' OR [OperationType] = 'UPDATE' OR [OperationType] = 'DELETE')
)
GO

