CREATE TABLE [Contact].[Contacts]
(
	[ContactId]				INT	                NOT NULL,
	[ContactGlobalUniqueId]	UNIQUEIDENTIFIER	NULL,
	[Email]     			VARCHAR(250)		NOT NULL,
	[Type]                  VARCHAR(20)         NOT NULL,
	[FirstName]				VARCHAR(250)		NULL,
	[LastName]				VARCHAR(250)		NULL,
	[Title]                 VARCHAR(3)          NULL,
	[MobilePhone]           VARCHAR(255)        NULL,
    CONSTRAINT [C_Contact_PK] PRIMARY KEY CLUSTERED ([ContactId] ASC)
)
GO
CREATE NONCLUSTERED INDEX [IX_Contacts_Email]
    ON [Contact].[Contacts] ([Email])
    INCLUDE ([ContactId], [ContactGlobalUniqueId], [FirstName], [LastName], [Type], [Title], [MobilePhone])
GO