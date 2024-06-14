CREATE TABLE [cre].[role](
	[ContactId] [uniqueidentifier] NOT NULL,
	[AccountId] [uniqueidentifier] NOT NULL,
	[Deleted] [datetime2](7) NULL,
	[RoleId] [uniqueidentifier] NOT NULL,
	[Onboarded] [bit] NULL,
    [RoleDelegataireEmail] [nvarchar](200),
    [RoleSignatory] [BIT] NULL,
    [IsFavorite] [BIT] NULL
PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [cre].[role]  WITH NOCHECK ADD  CONSTRAINT [FK_CreRoleEntity_Account] FOREIGN KEY([AccountId])
REFERENCES [cre].[account] ([Id])
GO

ALTER TABLE [cre].[role] CHECK CONSTRAINT [FK_CreRoleEntity_Account]
GO

ALTER TABLE [cre].[role]  WITH NOCHECK ADD  CONSTRAINT [FK_CreRoleEntity_Contact] FOREIGN KEY([ContactId])
REFERENCES [cre].[Contact] ([Id])
GO

ALTER TABLE [cre].[role] CHECK CONSTRAINT [FK_CreRoleEntity_Contact]
GO

