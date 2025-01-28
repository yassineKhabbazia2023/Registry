CREATE TABLE [Account].[Accounts] (
    [AccountId]               INT  NOT NULL,
    [AccountGlobalUniqueId]   UNIQUEIDENTIFIER NOT NULL,
    [AccountNumber]           VARCHAR (100)    NOT NULL,
    [LegalName]               NVARCHAR (255)   NOT NULL,
    [IsActive]                BIT              NOT NULL,
    [Siret]                   VARCHAR (150)    NULL,
    [CreationDate]            DATETIME2 (7)    NOT NULL,
    [UpdatedDate]             DATETIME2 (7)    NULL,
    [Status]                  NVARCHAR(50)     NOT NULL
    CONSTRAINT [C_Account_PK] PRIMARY KEY CLUSTERED ([AccountId] ASC),
    CONSTRAINT [UQ_Account_AccountGlobalUniqueId] UNIQUE NONCLUSTERED ([AccountGlobalUniqueId] ASC)
);