CREATE PROCEDURE [cre].[ManageAccountDelta]
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
		--temporay table to persist the operations
		CREATE TABLE #OutputAccountTable (
			Action NVARCHAR(10),
			[Id] [uniqueidentifier] NOT NULL,
			[Updated] [datetime] NULL,
			[LegalName] [nvarchar](255) NOT NULL,
			[AccountNumber] [nvarchar](50) NULL,
			[AccountFlagESCActif] [bit] NOT NULL,
			[Deleted] [datetime2](7) NULL,
		);

        MERGE INTO cre.[Account] AS dest
        USING alx.[Account] AS src
        ON (dest.AccountNumber = src.AccountNumber AND src.AccountFlagEscActif = 1) 
        WHEN MATCHED  THEN
            -- update the office of the cre contact 
            UPDATE SET 
				dest.AccountFlagEscActif = 1,
				dest.Updated =  GETDATE()


        WHEN NOT MATCHED BY source
		           AND EXISTS (
					SELECT 1 
					FROM alx.[Account] AS src
					WHERE src.AccountNumber = dest.AccountNumber AND src.AccountFlagEscActif = 0 AND dest.AccountFlagEscActif = 1
					) THEN
			-- soft delete account from cre
            UPDATE SET 
                dest.AccountFlagEscActif = 0, 
                dest.Deleted = GETDATE()

		WHEN NOT MATCHED BY TARGET AND src.AccountFlagEscActif = 1  THEN
			-- add account
	            INSERT (
                Id, 
                Updated, 
                LegalName, 
                AccountNumber, 
                AccountFlagEscActif, 
                Deleted
            )
            VALUES (
                src.Id, 
                NULL, 
                src.LegalName, 
                src.AccountNumber,  
                src.AccountFlagEscActif,
                NULL
            )
		OUTPUT $action, INSERTED.* INTO #OutputAccountTable;
        COMMIT TRANSACTION;

		--Insert into the ged operations table
		INSERT INTO [cre].[Operations]
			   (
			   [Operation]
			   ,[Type]
			   ,[PublishedAt]
			   ,[EntityId])
		SELECT 
			CASE 
				WHEN Action = 'UPDATE' AND Updated IS NOT NULL THEN 'UPDATE' 
				WHEN Action = 'UPDATE' AND Deleted IS NOT NULL  THEN 'DELETE'
				WHEN Action = 'INSERT' THEN 'INSERT'
				ELSE NULL 
			END,
			'ACCOUNT', 
			NULL, 
			Id
		FROM 
			#OutputAccountTable; 

		DROP TABLE #OutputAccountTable

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END
        
        DECLARE @ErrorMessage NVARCHAR(4000);
        DECLARE @ErrorSeverity INT;
        DECLARE @ErrorState INT;

        SELECT 
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH

    RETURN 0
END
GO


