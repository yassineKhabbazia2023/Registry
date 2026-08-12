-- <copyright file="MissionProcessing.sql" company="Pulse">
-- Copyright (c) Pulse. All rights reserved.
-- </copyright>

CREATE TABLE [Mission].[MissionProcessing]
(
    [MissionId]         INT                NOT NULL,
    [Operation]         NVARCHAR(20)       NOT NULL,
    [Status]            NVARCHAR(20)       NOT NULL,
    [Reason]            NVARCHAR(500)      NULL,
    [PublishedOn]       DATETIME2          NULL,
    [ProcessedOn]       DATETIME2          NULL,
    CONSTRAINT [PK_MissionProcessing] PRIMARY KEY CLUSTERED ([MissionId] ASC, [Operation] ASC),
    CONSTRAINT [FK_MissionProcessing_Missions] FOREIGN KEY ([MissionId])
        REFERENCES [Mission].[Missions]([MissionId]),
    CONSTRAINT [CK_MissionProcessing_Operation] CHECK ([Operation] IN (N'INSERT', N'UPDATE', N'DELETE'))
);
GO

CREATE NONCLUSTERED INDEX [IX_Status_PublishedOn]
    ON [Mission].[MissionProcessing]([Status] ASC, [PublishedOn] ASC)
    INCLUDE ([Reason], [ProcessedOn]);
GO
