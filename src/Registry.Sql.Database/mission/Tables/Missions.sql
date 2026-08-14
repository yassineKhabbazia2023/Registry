-- <copyright file="Missions.sql" company="Pulse">
-- Copyright (c) Pulse. All rights reserved.
-- </copyright>

-- Landing table of the Akuiteo engagement CSV deposited by the DS2I.
-- One row per CSV line, written once and never updated: every piece of mutable
-- state lives in mission.Processing.
CREATE TABLE [mission].[Missions](
    [RegistryMissionId]     INT                NOT NULL IDENTITY(1,1),
    [AccountNumber]         NVARCHAR(50)       NOT NULL,
    [EngagementCode]        NVARCHAR(50)       NOT NULL,
    [OfferCode]             NVARCHAR(100)      NOT NULL,
    [ProductCode]           NVARCHAR(100)      NULL,
    [StartDate]             DATE               NOT NULL,
    [EndDate]               DATE               NOT NULL,
    [Operation]             NVARCHAR(20)       NOT NULL,
    [CreatedOn]             DATETIME2          NOT NULL,
    CONSTRAINT [PK_Missions] PRIMARY KEY CLUSTERED ([RegistryMissionId] ASC),
    CONSTRAINT [CHK_Missions_Operation] CHECK ([Operation] = 'INSERT' OR [Operation] = 'DELETE'),
    -- Business key, and the key the acknowledgement resolves on: Offer answers with the
    -- engagement code alone and the operation is deduced from the event type.
    -- AccountNumber is deliberately out of it - the same code borne by two accounts is a
    -- flow anomaly we want rejected at ingestion rather than silently accepted.
    CONSTRAINT [UQ_Missions_Operation_EngagementCode] UNIQUE NONCLUSTERED ([Operation] ASC, [EngagementCode] ASC)
)
GO

CREATE NONCLUSTERED INDEX [IX_Missions_AccountNumber_EngagementCode]
    ON [mission].[Missions] ([AccountNumber] ASC, [EngagementCode] ASC)
GO
