CREATE TABLE [Contact].[Contacts]
(
	[ContactId]				INT	                NOT NULL,
	[ContactGlobalUniqueId]	UNIQUEIDENTIFIER	NULL,
	[FirstName]				VARCHAR(250)		NOT NULL,
	[LastName]				VARCHAR(250)		NOT NULL,
	[Email]     			VARCHAR(250)		NOT NULL,
	[Type]                  VARCHAR(20)         NOT NULL, 
	[Status]                VARCHAR(20)         NULL, 
	[PersonaName]           VARCHAR(50)         NOT NULL, 
	[Office]				VARCHAR(250)		NULL,
	[CreationDate]          DATETIME2           NOT NULL DEFAULT GETDATE(),
    [LastUpdateDate]        DATETIME2           NULL, 
    [IsActive] BIT NOT NULL DEFAULT (1),
	[LandPhone]				NVARCHAR(50)		NULL,
	[MobilePhone]			NVARCHAR(50)		NULL,
	[Source]				NVARCHAR(50)		NULL
    CONSTRAINT [C_Contact_PK] PRIMARY KEY CLUSTERED ([ContactId] ASC)
)