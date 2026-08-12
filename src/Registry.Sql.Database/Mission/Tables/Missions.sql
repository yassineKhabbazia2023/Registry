CREATE TABLE [Mission].[Missions](
    [MissionId]             INT                NOT NULL IDENTITY(1,1),
    [AccountNumber]         NVARCHAR(50)       NOT NULL,
    [EngagementCode]        NVARCHAR(50)       NOT NULL,
    [OfferCode]             NVARCHAR(100)      NOT NULL,
    [ProductCode]           NVARCHAR(100)      NULL,
    [StartDate]             DATE               NOT NULL,
    [EndDate]               DATE               NOT NULL,
    [CreatedBy]             VARCHAR(100)       NULL,
    [ModifiedBy]            VARCHAR(100)       NULL,
    CONSTRAINT [PK_Missions] PRIMARY KEY CLUSTERED ([MissionId] ASC),
    CONSTRAINT [UQ_Missions_AccountNumber_EngagementCode] UNIQUE NONCLUSTERED ([AccountNumber] ASC, [EngagementCode] ASC)
)
GO
