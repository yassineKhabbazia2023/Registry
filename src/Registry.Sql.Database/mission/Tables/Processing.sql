-- <copyright file="Processing.sql" company="Pulse">
-- Copyright (c) Pulse. All rights reserved.
-- </copyright>

-- Processing state of an engagement line, kept apart from mission.Missions so that
-- the rows received from the CSV are never rewritten.
CREATE TABLE [mission].[Processing]
(
    [RegistryMissionId] INT                NOT NULL,
    [Status]            NVARCHAR(20)       NOT NULL,
    [Reason]            NVARCHAR(500)      NULL,
    [PublishedOn]       DATETIME2          NULL,
    [ProcessedOn]       DATETIME2          NULL,
    CONSTRAINT [PK_Processing] PRIMARY KEY CLUSTERED ([RegistryMissionId] ASC),
    CONSTRAINT [FK_Processing_Missions] FOREIGN KEY ([RegistryMissionId])
        REFERENCES [mission].[Missions]([RegistryMissionId])
);
GO

CREATE NONCLUSTERED INDEX [IX_Processing_Status_PublishedOn]
    ON [mission].[Processing]([Status] ASC, [PublishedOn] ASC)
    INCLUDE ([Reason], [ProcessedOn]);
GO
