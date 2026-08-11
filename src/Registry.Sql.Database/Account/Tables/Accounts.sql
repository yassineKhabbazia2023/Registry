CREATE TABLE [Account].[Accounts] (
    [AccountId]               INT  NOT NULL,
    [AccountGlobalUniqueId]   UNIQUEIDENTIFIER NULL,
    [AccountNumber]           VARCHAR (100)    NOT NULL,
    [ModifiedBy]              VARCHAR (100) NULL,
    [CreatedBy]                VARCHAR (100) NULL,
    [LegalName]               NVARCHAR (255)   NOT NULL default '',
    -- Provisional NVARCHAR(255) until Akuiteo confirms the definitive field types and lengths.
    [AccountRoutingCode]          NVARCHAR (255) NULL,
    [AccountRoutingLabel]         NVARCHAR (255) NULL,
    [AccountLegalFormLabel]       NVARCHAR (255) NULL,
    [AccountElectronicAddressId]  NVARCHAR (255) NULL,
    CONSTRAINT [C_Account_PK] PRIMARY KEY CLUSTERED ([AccountId] ASC),
    CONSTRAINT [UQ_Account_AccountGlobalUniqueId] UNIQUE NONCLUSTERED ([AccountGlobalUniqueId] ASC)
);
GO
CREATE NONCLUSTERED INDEX [IX_Accounts_AccountNumber]
    ON [Account].[Accounts] ([AccountNumber])
    INCLUDE ([AccountId], [LegalName])
GO
