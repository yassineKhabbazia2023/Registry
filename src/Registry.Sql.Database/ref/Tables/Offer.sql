CREATE TABLE [ref].[Offer](
	[Id]					INT				NOT NULL IDENTITY(1,1),
	[AccountNumber]			NVARCHAR(255)	   NOT NULL,
	[ClientEmail]			NVARCHAR(255)	   NOT NULL,
	[CollaboratorEmail]		NVARCHAR(255)	   NULL,
	[MissionLeaderEmail]	NVARCHAR(255)	   NULL,
	[AccountingExpertEmail]	NVARCHAR(255)	   NULL,
	[Offer]					NVARCHAR(255)	   NOT NULL,
	[MigrationStatus]		NVARCHAR(50)	   NOT NULL,
	[BatchId]				UNIQUEIDENTIFIER   NOT NULL,
	CONSTRAINT [PK_Offer] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO
