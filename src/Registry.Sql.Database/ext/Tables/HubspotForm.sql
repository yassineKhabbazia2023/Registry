CREATE TABLE [ext].[HubspotForm]
(
    [SubmittedBy] NVARCHAR(255) NOT NULL,
    [SubmittedAt] DATETIME NOT NULL,
    [AccountNumber] NVARCHAR(100) NOT NULL,
    [HubSpotDispatchState] BIT NOT NULL CONSTRAINT [DF_HubspotForm_HubSpotDispatchState] DEFAULT (0),
    [FormData] NVARCHAR(MAX) NOT NULL,
    CONSTRAINT [C_HubspotForm_PK] PRIMARY KEY CLUSTERED ([AccountNumber] ASC, [SubmittedBy] ASC)
)
