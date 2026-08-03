CREATE TABLE [ref].[Invoice](
	[InvoiceId]		INT				NOT NULL IDENTITY(1,1),
	[AccountNumber]	NVARCHAR(50)	NOT NULL,
	[InvoiceNumber]	NVARCHAR(100)	NOT NULL,
	[InvoiceDate]	DATE			NOT NULL,
	[DocumentPath]	NVARCHAR(1000)	NOT NULL,
	[Type]			NVARCHAR(50)	NOT NULL,
	[Operation]		NVARCHAR(10)	NOT NULL,
	[Status]		NVARCHAR(20)	NOT NULL,
	[CreatedOn]		DATETIME2		NOT NULL,
	CONSTRAINT [PK_Invoice] PRIMARY KEY CLUSTERED ([InvoiceId] ASC)
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Invoice_Operation_AccountNumber_InvoiceNumber]
	ON [ref].[Invoice] ([Operation] ASC, [AccountNumber] ASC, [InvoiceNumber] ASC)
GO
