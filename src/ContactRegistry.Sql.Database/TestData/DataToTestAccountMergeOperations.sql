-- Case: Activate account
INSERT INTO [alx].[account]
           ([Id]
           ,[AccountFlagEscActif]
           ,[LegalName]
           ,[DeploymentStatus]
           ,[AccountNumber])
     VALUES
           ('07f8a524-2142-42a8-a1cc-ee0f49507b64', -- Generate a new unique identifier
            1, -- AccountFlagEscActif is 1 (active)
            N'Active Account Legal Name', -- LegalName indicating active status
            N'Deployed', -- Example DeploymentStatus
            N'ACTIVE-001' -- AccountNumber indicating active status
           )
GO

INSERT INTO [cre].[account]
           ([Id]
           ,[Updated]
           ,[LegalName]
           ,[DeploymentStatus]
           ,[AccountNumber]
           ,[AccountFlagEscActif]
           ,[Deleted])
     VALUES
           ('07f8a524-2142-42a8-a1cc-ee0f49507b64', -- Generate a new unique identifier
            NULL, -- Updated
            N'Active Account Legal Name', -- LegalName indicating active status
            N'Deployed', -- Example DeploymentStatus
            N'ACTIVE-001', -- AccountNumber indicating active status
            0, -- AccountFlagEscActif
            NULL -- Deleted
           )
GO

-- Case: Revoke account
INSERT INTO [alx].[account]
           ([Id]
           ,[AccountFlagEscActif]
           ,[LegalName]
           ,[DeploymentStatus]
           ,[AccountNumber])
     VALUES
           ('f0016238-90c3-40b7-8866-c4d14c283189', -- Generate a new unique identifier
            0, -- AccountFlagEscActif is 0 (inactive)
            N'Inactive Account Legal Name', -- LegalName indicating inactive status
            N'Deployment Pending', -- Example DeploymentStatus
            N'INACTIVE-001' -- AccountNumber indicating inactive status
           )
GO

INSERT INTO [cre].[account]
           ([Id]
           ,[Updated]
           ,[LegalName]
           ,[DeploymentStatus]
           ,[AccountNumber]
           ,[AccountFlagEscActif]
           ,[Deleted])
     VALUES
           ('f0016238-90c3-40b7-8866-c4d14c283189', -- Generate a new unique identifier
            NULL, -- Updated
            N'Inactive Account Legal Name', -- LegalName indicating inactive status
            N'Deployment Pending', -- Example DeploymentStatus
            N'INACTIVE-001', -- AccountNumber indicating inactive status
            1, -- AccountFlagEscActif
            NULL -- Deleted
           )
GO

-- Case: Insert an active account that does not exist in another table
INSERT INTO [alx].[account]
           ([Id]
           ,[AccountFlagEscActif]
           ,[LegalName]
           ,[DeploymentStatus]
           ,[AccountNumber])
     VALUES
           ('1ba3a60b-063a-4cb3-ba93-b67d2076a68e', -- Generate a new unique identifier
            1, -- AccountFlagEscActif is 1 (active)
            N'Nonexistent Account Legal Name', -- LegalName indicating nonexistence in another table
            N'Deployed', -- Example DeploymentStatus
            N'NONEXISTENT-001' -- AccountNumber indicating nonexistence in another table
           )
GO
