-- Case: Add new Role
INSERT INTO [alx].[role]
           ([ContactId]
           ,[AccountId]
           ,[Onboarded]
           ,[RoleId])
     VALUES
           ('2e315ad9-0233-4968-9be1-8ce56b562fca', -- Generate a new unique identifier for ContactId
            '1ba3a60b-063a-4cb3-ba93-b67d2076a68e', -- Generate a new unique identifier for AccountId
            1, -- Onboarded
            NEWID() -- Generate a new unique identifier for RoleId
           )
GO

-- Case: Remove role
INSERT INTO [alx].[role]
           ([ContactId]
           ,[AccountId]
           ,[Onboarded]
           ,[RoleId])
     VALUES
           ('2e315ad9-0233-4968-9be1-8ce56b562fca', -- Generate a new unique identifier for ContactId
            '07f8a524-2142-42a8-a1cc-ee0f49507b64', -- Generate a new unique identifier for AccountId
            0, -- Onboarded is 0
            NEWID() -- Generate a new unique identifier for RoleId
           )
GO

INSERT INTO [cre].[Role]
           ([ContactId]
           ,[AccountId]
           ,[Onboarded]
           ,[RoleId]
           ,[Deleted])
     VALUES
           ('2e315ad9-0233-4968-9be1-8ce56b562fca', -- ContactId
            '07f8a524-2142-42a8-a1cc-ee0f49507b64', -- AccountId
            1, -- Onboarded
            NEWID(), -- RoleId
            NULL -- Deleted
           )
GO
