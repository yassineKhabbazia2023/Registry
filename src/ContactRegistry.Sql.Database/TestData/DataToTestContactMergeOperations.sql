INSERT INTO [alx].[Contact]
           ([Id]
           ,[OfficeId]
           ,[IsCustomer]
           ,[IsActive]
           ,[FirstName]
           ,[LastName]
           ,[Email]
           ,[LandPhone]
           ,[MobilePhone]
           ,[JobDescription])
     VALUES
     -- Case: Update office
     ('2e315ad9-0233-4968-9be1-8ce56b562fca', -- Id
      '421d926d-c3f2-48c7-95f7-f2db615a54f7', -- OfficeId
      1, -- IsCustomer
      1, -- IsActive
      'My Office', -- FirstName
      'Changed', -- LastName
      'john.doe@example.com', -- Email
      '123-456-7890', -- LandPhone
      '098-765-4321', -- MobilePhone
      'Software Developer' -- JobDescription
     ),
     -- Case: Delete contact
     ('49614535-87f9-414e-b055-0af3dcec7025', -- Id
      '421d926d-c3f2-48c7-95f7-f2db615a54f7', -- OfficeId
      1, -- IsCustomer
      0, -- IsActive
      'No longer', -- FirstName
      'Active', -- LastName
      'tom.doe@example.com', -- Email
      '123-456-7890', -- LandPhone
      '098-765-4321', -- MobilePhone
      'Software Developer' -- JobDescription
     ),
     -- Case: Add contact
     ('073b948b-ce45-4863-973d-d1faab994dc6', -- Id
      '421d926d-c3f2-48c7-95f7-f2db615a54f7', -- OfficeId
      1, -- IsCustomer
      1, -- IsActive
      'I AM', -- FirstName
      'New', -- LastName
      'jane.doe@example.com', -- Email
      '123-456-7890', -- LandPhone
      '098-765-4321', -- MobilePhone
      'Software Developer' -- JobDescription
     )
GO

INSERT INTO [cre].[Contact]
           ([Id]
           ,[OfficeId]
           ,[IsCustomer]
           ,[IsActive]
           ,[Updated]
           ,[Deleted]
           ,[FirstName]
           ,[LastName]
           ,[Email]
           ,[LandPhone]
           ,[MobilePhone]
           ,[JobDescription]
           ,[Source])
     VALUES
     ('2e315ad9-0233-4968-9be1-8ce56b562fca', -- Id
      '421d926d-c3f2-48c7-95f7-f2db615a54f8', -- OfficeId
      1, -- IsCustomer
      1, -- IsActive
      NULL, -- Updated
      NULL, -- Deleted
      'My Office', -- FirstName
      'Changed', -- LastName
      'john.doe@example.com', -- Email
      '123-456-7890', -- LandPhone
      '098-765-4321', -- MobilePhone
      'Software Developer', -- JobDescription
      'Reg' -- Source
     ),
     ('49614535-87f9-414e-b055-0af3dcec7025', -- Id
      '421d926d-c3f2-48c7-95f7-f2db615a54f7', -- OfficeId
      1, -- IsCustomer
      1, -- IsActive
      NULL, -- Updated
      NULL, -- Deleted
      'No longer', -- FirstName
      'Active', -- LastName
      'tom.doe@example.com', -- Email
      '123-456-7890', -- LandPhone
      '098-765-4321', -- MobilePhone
      'Software Developer', -- JobDescription
      'Reg' -- Source
     )
GO
