CREATE TABLE [reg].[role](
	[ContactEmail] [nvarchar](255) NOT NULL,
	[AccountNumber] [nvarchar](50) NOT NULL,
	[AccountId] [uniqueidentifier] NOT NULL,
	[ContactId] [uniqueidentifier] NOT NULL,
	[Deleted] [datetime2](7) NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[Onboarded] [bit] NULL,
    [RoleDelegataireEmail] [nvarchar](200),
    [RoleSignatory] [BIT] NULL,
    [IsFavorite] [BIT] NULL
, 
    [Description] NVARCHAR(50) NOT NULL, 
    CONSTRAINT [PK_role] PRIMARY KEY ([ContactEmail], [AccountNumber], [Description])
) ON [PRIMARY]
GO

ALTER TABLE [reg].[role]  WITH NOCHECK ADD  CONSTRAINT [FK_RegRoleEntity_Account] FOREIGN KEY([AccountNumber])
REFERENCES [reg].[account] ([AccountNumber])
GO

ALTER TABLE [reg].[role] CHECK CONSTRAINT [FK_RegRoleEntity_Account]
GO

ALTER TABLE [reg].[role]  WITH NOCHECK ADD  CONSTRAINT [FK_RegRoleEntity_Contact] FOREIGN KEY([ContactEmail])
REFERENCES [reg].[Contact] ([Email])
GO

ALTER TABLE [reg].[role] CHECK CONSTRAINT [FK_RegRoleEntity_Contact]
GO

