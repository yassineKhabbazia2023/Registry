CREATE TABLE [alx].[Role](
    [ContactId] [uniqueidentifier] NOT NULL,
    [AccountId] [uniqueidentifier] NOT NULL,
    [Onboarded] [bit] NOT NULL,
    [RoleId] [uniqueidentifier] NOT NULL,
    PRIMARY KEY CLUSTERED 
    (
        [RoleId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [alx].[role]  WITH NOCHECK ADD  CONSTRAINT [FK_AlxRoleEntity_Account] FOREIGN KEY([AccountId])
REFERENCES [alx].[account] ([Id])
GO

ALTER TABLE [alx].[role] CHECK CONSTRAINT [FK_AlxRoleEntity_Account]
GO

ALTER TABLE [alx].[role]  WITH NOCHECK ADD  CONSTRAINT [FK_AlxRoleEntity_Contact] FOREIGN KEY([ContactId])
REFERENCES [alx].[Contact] ([Id])
GO

ALTER TABLE [alx].[role] CHECK CONSTRAINT [FK_AlxRoleEntity_Contact]
GO
