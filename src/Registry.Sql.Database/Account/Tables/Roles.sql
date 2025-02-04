CREATE TABLE [Account].[Roles]
(
	[AccountId]			INT					NOT NULL,
	[ContactId]			INT					NOT NULL,
	[IsFavorite]		BIT					NULL,
	[IsSignatory]		BIT					NULL,
    [IsDelegation]      BIT                 NULL,
	[RoleDuplicatesCounter] INT				NULL,
	[AccountGlobalUniqueId] UNIQUEIDENTIFIER NULL,
	[ContactGlobalUniqueId] UNIQUEIDENTIFIER NULL,
	[DelegatorContactId]	INT NULL,
	[AccountNumber]			NVARCHAR(50) NULL,
	[ContactEmail]			NVARCHAR(50) NULL,
	CONSTRAINT [C_Role_PK] PRIMARY KEY CLUSTERED ([ContactId] ASC, [AccountId] ASC),
	CONSTRAINT [C_Account_Role_FK] FOREIGN KEY ([AccountId]) REFERENCES [Account].[Accounts] ([AccountId]),
	CONSTRAINT [C_Account_Contact_FK] FOREIGN KEY ([ContactId]) REFERENCES [Contact].[Contacts] ([ContactId]), 
    CONSTRAINT [C_Role_AccountId_ContactId] UNIQUE ([AccountId], [ContactId])
)