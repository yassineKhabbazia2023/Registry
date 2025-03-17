CREATE TABLE [Contact].[Contacts]
(
	[ContactId]				INT	                NOT NULL,
	[ContactGlobalUniqueId]	UNIQUEIDENTIFIER	NULL,
	[Email]     			VARCHAR(250)		NOT NULL,
	[Type]                  VARCHAR(20)         NOT NULL,
	[FirstName]				VARCHAR(250)		NULL,
	[LastName]				VARCHAR(250)		NULL,
    CONSTRAINT [C_Contact_PK] PRIMARY KEY CLUSTERED ([ContactId] ASC)
)