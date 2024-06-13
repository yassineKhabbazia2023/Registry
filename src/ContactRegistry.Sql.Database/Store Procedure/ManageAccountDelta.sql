CREATE PROCEDURE [cre].[ManageAccountDelta]
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
		--temporay table to persist the operations
		CREATE TABLE #OutputAccountTable (
			[Action] NVARCHAR(10),
			[Id] [uniqueidentifier] NOT NULL,
			[Updated] [datetime] NULL,
			[LegalName] [nvarchar](255) NOT NULL,
			[AccountNumber] [nvarchar](50) NULL,
			[AccountFlagESCActif] [bit] NOT NULL,
			[Deleted] [datetime2](7) NULL,
			[AccountISIN] [nvarchar](100) NULL,
			[AccountCommercialName] [nvarchar](255) NULL,
			[AccountNafIdentifier] [nvarchar](50) NULL,
			[AccountSectorCode] [nvarchar](50) NULL,
			[AccountTaxeValeurAjoutee] [nvarchar](50) NULL,
			[AccountDeliveryEmail] [nvarchar](255) NULL,
			[AccountBillingEmail] [nvarchar](255) NULL,
			[AccountTaxationSystem] [nvarchar](100) NULL,
			[AccountSourceName] [nvarchar](100) NULL,
			[AccountRegisterIdentification1] [nvarchar](50) NULL,
			[AccountStaffSize] [nvarchar](50) NULL,
			[AccountDeliveryFax] [nvarchar](50) NULL,
			[AccountBillingFax] [nvarchar](50) NULL,
			[AccountTurnoverSlice] [nvarchar](50) NULL,
			[AccountRegimeFiscal] [nvarchar](50) NULL,
			[AccountTypeTenueComptable] [nvarchar](50) NULL,
			[AccountType] [nvarchar](50) NULL,
			[AccountFormeJuridique] [nvarchar](100) NULL,
			[AccountStaffSizeSlice] [nvarchar](50) NULL,
			[AccountEscCategory] [nvarchar](50) NULL,
			[AccountCodeFormeJuridique] [nvarchar](50) NULL,
			[AccountEmail] [nvarchar](50) NULL,
			[AccountInsertedDate] [datetime2] NULL,
			[AccountUpdatedDate] [datetime2] NULL,
			[DeliveryAddressLine1] [nvarchar](255) NULL,
			[DeliveryAddressLine2] [nvarchar](255) NULL,
			[DeliveryAddressLine3] [nvarchar](255) NULL,
			[DeliveryCity] [nvarchar](50) NULL,
			[DeliveryZipCode] [nvarchar](20) NULL,
			[DeliveryCountry] [nvarchar](50) NULL,
			[DeliveryState] [nvarchar](50) NULL,
			[BillingAddressLine1] [nvarchar](255) NULL,
			[BillingAddressLine2] [nvarchar](255) NULL,
			[BillingAddressLine3] [nvarchar](255) NULL,
			[BillingCity] [nvarchar](50) NULL,
			[BillingZipCode] [nvarchar](20) NULL,
			[BillingCountry] [nvarchar](50) NULL,
			[BillingState] [nvarchar](50) NULL,
			[DeploymentStatus] [nvarchar](50) NULL,
			[DeploymentDate] [datetime2] NULL,
			[CreatedBy] [nvarchar](100) NULL,
			[ModifiedBy] [nvarchar](100) NULL
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
					AccountFlagESCActif,
					Deleted,
					AccountISIN,
					AccountCommercialName,
					AccountNafIdentifier,
					AccountSectorCode,
					AccountTaxeValeurAjoutee,
					AccountDeliveryEmail,
					AccountBillingEmail,
					AccountTaxationSystem,
					AccountSourceName,
					AccountRegisterIdentification1,
					AccountStaffSize,
					AccountDeliveryFax,
					AccountBillingFax,
					AccountTurnoverSlice,
					AccountRegimeFiscal,
					AccountTypeTenueComptable,
					AccountType,
					AccountFormeJuridique,
					AccountStaffSizeSlice,
					AccountEscCategory,
					AccountCodeFormeJuridique,
					AccountEmail,
					AccountInsertedDate,
					AccountUpdatedDate,
					DeliveryAddressLine1,
					DeliveryAddressLine2,
					DeliveryAddressLine3,
					DeliveryCity,
					DeliveryZipCode,
					DeliveryCountry,
					DeliveryState,
					BillingAddressLine1,
					BillingAddressLine2,
					BillingAddressLine3,
					BillingCity,
					BillingZipCode,
					BillingCountry,
					BillingState,
					DeploymentStatus,
					DeploymentDate,
					CreatedBy,
					ModifiedBy
            )
            VALUES (
					src.AccountGlobalUniqueIdentifier,
					NULL,
					src.LegalName,
					src.AccountNumber,
					src.AccountFlagESCActif,
					NULL,
					src.AccountISIN,
					src.AccountCommercialName,
					src.AccountNafIdentifier,
					src.AccountSectorCode,
					src.AccountTaxeValeurAjoutee,
					src.AccountDeliveryEmail,
					src.AccountBillingEmail,
					src.AccountTaxationSystem,
					src.AccountSourceName,
					src.AccountRegisterIdentification1,
					src.AccountStaffSize,
					src.AccountDeliveryFax,
					src.AccountBillingFax,
					src.AccountTurnoverSlice,
					src.AccountRegimeFiscal,
					src.AccountTypeTenueComptable,
					src.AccountType,
					src.AccountFormeJuridique,
					src.AccountStaffSizeSlice,
					src.AccountEscCategory,
					src.AccountCodeFormeJuridique,
					src.AccountEmail,
					src.AccountInsertedDate,
					src.AccountUpdatedDate,
					src.DeliveryAddressLine1,
					src.DeliveryAddressLine2,
					src.DeliveryAddressLine3,
					src.DeliveryCity,
					src.DeliveryZipCode,
					src.DeliveryCountry,
					src.DeliveryState,
					src.BillingAddressLine1,
					src.BillingAddressLine2,
					src.BillingAddressLine3,
					src.BillingCity,
					src.BillingZipCode,
					src.BillingCountry,
					src.BillingState,
					src.DeploymentStatus,
					src.DeploymentDate,
					src.CreatedBy,
					src.ModifiedBy
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


