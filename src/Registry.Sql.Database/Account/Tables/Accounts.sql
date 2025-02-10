CREATE TABLE [Account].[Accounts] (
    [AccountId]               INT  NOT NULL,
    [AccountGlobalUniqueId]   UNIQUEIDENTIFIER NULL,
    [AccountNumber]           VARCHAR (100)    NOT NULL,
    CONSTRAINT [C_Account_PK] PRIMARY KEY CLUSTERED ([AccountId] ASC),
    CONSTRAINT [UQ_Account_AccountGlobalUniqueId] UNIQUE NONCLUSTERED ([AccountGlobalUniqueId] ASC)
);