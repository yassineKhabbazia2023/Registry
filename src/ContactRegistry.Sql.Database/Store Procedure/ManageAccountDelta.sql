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
			[AccountNumber] [nvarchar](255) NULL,
			[AccountFlagESCActif] [bit] NOT NULL,
			[Deleted] [datetime2](7) NULL,
			[AccountISIN] [nvarchar](255) NULL,
			[AccountCommercialName] [nvarchar](255) NULL,
			[AccountNafIdentifier] [nvarchar](255) NULL,
			[AccountSectorCode] [nvarchar](255) NULL,
			[AccountTaxeValeurAjoutee] [nvarchar](255) NULL,
			[AccountDeliveryEmail] [nvarchar](255) NULL,
			[AccountBillingEmail] [nvarchar](255) NULL,
			[AccountTaxationSystem] [nvarchar](255) NULL,
			[AccountSourceName] [nvarchar](255) NULL,
			[AccountRegisterIdentification1] [nvarchar](255) NULL,
			[AccountStaffSize] [nvarchar](255) NULL,
			[AccountDeliveryFax] [nvarchar](255) NULL,
			[AccountBillingFax] [nvarchar](255) NULL,
			[AccountTurnoverSlice] [nvarchar](255) NULL,
			[AccountRegimeFiscal] [nvarchar](255) NULL,
			[AccountTypeTenueComptable] [nvarchar](255) NULL,
			[AccountType] [nvarchar](255) NULL,
			[AccountFormeJuridique] [nvarchar](255) NULL,
			[AccountStaffSizeSlice] [nvarchar](255) NULL,
			[AccountEscCategory] [nvarchar](255) NULL,
			[AccountCodeFormeJuridique] [nvarchar](255) NULL,
			[AccountEmail] [nvarchar](255) NULL,
			[AccountInsertedDate] [datetime2] NULL,
			[AccountUpdatedDate] [datetime2] NULL,
			[DeliveryAddressLine1] [nvarchar](255) NULL,
			[DeliveryAddressLine2] [nvarchar](255) NULL,
			[DeliveryAddressLine3] [nvarchar](255) NULL,
			[DeliveryCity] [nvarchar](255) NULL,
			[DeliveryZipCode] [nvarchar](255) NULL,
			[DeliveryCountry] [nvarchar](255) NULL,
			[DeliveryState] [nvarchar](255) NULL,
			[BillingAddressLine1] [nvarchar](255) NULL,
			[BillingAddressLine2] [nvarchar](255) NULL,
			[BillingAddressLine3] [nvarchar](255) NULL,
			[BillingCity] [nvarchar](255) NULL,
			[BillingZipCode] [nvarchar](255) NULL,
			[BillingCountry] [nvarchar](255) NULL,
			[BillingState] [nvarchar](255) NULL,
			[DeploymentStatus] [nvarchar](255) NULL,
			[DeploymentDate] [datetime2] NULL,
			[CreatedBy] [nvarchar](100) NULL,
			[ModifiedBy] [nvarchar](100) NULL,
			[DeliveryPhone] NVARCHAR(255) NULL, 
			[BillingPhone] NVARCHAR(255) NULL
		);

        MERGE INTO cre.[Account] AS dest
        USING alx.[Account] AS src
        ON (dest.Id = src.AccountGlobalUniqueIdentifier AND src.AccountFlagEscActif = 1) 
            WHEN MATCHED  AND (
			dest.AccountFlagESCActif <> src.AccountFlagESCActif OR
            ISNULL(dest.AccountNumber, '') <> ISNULL(src.AccountNumber, '') OR
            dest.LegalName <> src.LegalName OR
            ISNULL(dest.AccountISIN, '') <> ISNULL(src.AccountISIN, '') OR
            ISNULL(dest.AccountCommercialName, '') <> ISNULL(src.AccountCommercialName, '') OR
            ISNULL(dest.AccountNafIdentifier, '') <> ISNULL(src.AccountNafIdentifier, '') OR
            ISNULL(dest.AccountSectorCode, '') <> ISNULL(src.AccountSectorCode, '') OR
            ISNULL(dest.AccountTaxeValeurAjoutee, '') <> ISNULL(src.AccountTaxeValeurAjoutee, '') OR
            ISNULL(dest.AccountDeliveryEmail, '') <> ISNULL(src.AccountDeliveryEmail, '') OR
            ISNULL(dest.AccountBillingEmail, '') <> ISNULL(src.AccountBillingEmail, '') OR
            ISNULL(dest.AccountTaxationSystem, '') <> ISNULL(src.AccountTaxationSystem, '') OR
            ISNULL(dest.AccountSourceName, '') <> ISNULL(src.AccountSourceName, '') OR
            ISNULL(dest.AccountRegisterIdentification1, '') <> ISNULL(src.AccountRegisterIdentification1, '') OR
            ISNULL(dest.AccountStaffSize, '') <> ISNULL(src.AccountStaffSize, '') OR
            ISNULL(dest.AccountDeliveryFax, '') <> ISNULL(src.AccountDeliveryFax, '') OR
            ISNULL(dest.AccountBillingFax, '') <> ISNULL(src.AccountBillingFax, '') OR
            ISNULL(dest.AccountRegimeFiscal, '') <> ISNULL(src.AccountRegimeFiscal, '') OR
            ISNULL(dest.AccountTypeTenueComptable, '') <> ISNULL(src.AccountTypeTenueComptable, '') OR
            ISNULL(dest.AccountType, '') <> ISNULL(src.AccountType, '') OR
            ISNULL(dest.AccountFormeJuridique, '') <> ISNULL(src.AccountFormeJuridique, '') OR
            ISNULL(dest.AccountStaffSizeSlice, '') <> ISNULL(src.AccountStaffSizeSlice, '') OR
            ISNULL(dest.AccountEscCategory, '') <> ISNULL(src.AccountEscCategory, '') OR
            ISNULL(dest.AccountCodeFormeJuridique, '') <> ISNULL(src.AccountCodeFormeJuridique, '') OR
            ISNULL(dest.AccountEmail, '') <> ISNULL(src.AccountEmail, '') OR
            dest.AccountInsertedDate <> src.AccountInsertedDate OR
            dest.AccountUpdatedDate <> src.AccountUpdatedDate OR
            ISNULL(dest.DeliveryAddressLine1, '') <> ISNULL(src.DeliveryAddressLine1, '') OR
            ISNULL(dest.DeliveryAddressLine2, '') <> ISNULL(src.DeliveryAddressLine2, '') OR
            ISNULL(dest.DeliveryAddressLine3, '') <> ISNULL(src.DeliveryAddressLine3, '') OR
            ISNULL(dest.DeliveryCity, '') <> ISNULL(src.DeliveryCity, '') OR
            ISNULL(dest.DeliveryZipCode, '') <> ISNULL(src.DeliveryZipCode, '') OR
            ISNULL(dest.DeliveryCountry, '') <> ISNULL(src.DeliveryCountry, '') OR
            ISNULL(dest.DeliveryState, '') <> ISNULL(src.DeliveryState, '') OR
            ISNULL(dest.BillingAddressLine1, '') <> ISNULL(src.BillingAddressLine1, '') OR
            ISNULL(dest.BillingAddressLine2, '') <> ISNULL(src.BillingAddressLine2, '') OR
            ISNULL(dest.BillingAddressLine3, '') <> ISNULL(src.BillingAddressLine3, '') OR
            ISNULL(dest.BillingCity, '') <> ISNULL(src.BillingCity, '') OR
            ISNULL(dest.BillingZipCode, '') <> ISNULL(src.BillingZipCode, '') OR
            ISNULL(dest.BillingCountry, '') <> ISNULL(src.BillingCountry, '') OR
            ISNULL(dest.BillingState, '') <> ISNULL(src.BillingState, '') OR
            ISNULL(dest.DeploymentStatus, '') <> ISNULL(src.DeploymentStatus, '') OR
            dest.DeploymentDate <> src.DeploymentDate OR
            ISNULL(dest.CreatedBy, '') <> ISNULL(src.CreatedBy, '') OR
            ISNULL(dest.ModifiedBy, '') <> ISNULL(src.ModifiedBy, '') OR
            ISNULL(dest.DeliveryPhone, '') <> ISNULL(src.AccountDeliveryPhone, '') OR
            ISNULL(dest.BillingPhone, '') <> ISNULL(src.AccountBillingPhone, '')
		)   THEN
            -- update the office of the cre contact 
            UPDATE SET 
                dest.AccountFlagEscActif = src.AccountFlagEscActif,
                dest.Updated = GETDATE(),
                dest.LegalName = src.LegalName,
                dest.AccountNumber = src.AccountNumber,
                dest.AccountISIN = src.AccountISIN,
                dest.AccountCommercialName = src.AccountCommercialName,
                dest.AccountNafIdentifier = src.AccountNafIdentifier,
                dest.AccountSectorCode = src.AccountSectorCode,
                dest.AccountTaxeValeurAjoutee = src.AccountTaxeValeurAjoutee,
                dest.AccountDeliveryEmail = src.AccountDeliveryEmail,
                dest.AccountBillingEmail = src.AccountBillingEmail,
                dest.AccountTaxationSystem = src.AccountTaxationSystem,
                dest.AccountSourceName = src.AccountSourceName,
                dest.AccountRegisterIdentification1 = src.AccountRegisterIdentification1,
                dest.AccountStaffSize = src.AccountStaffSize,
                dest.AccountDeliveryFax = src.AccountDeliveryFax,
                dest.AccountBillingFax = src.AccountBillingFax,
                dest.AccountRegimeFiscal = src.AccountRegimeFiscal,
                dest.AccountTypeTenueComptable = src.AccountTypeTenueComptable,
                dest.AccountType = src.AccountType,
                dest.AccountFormeJuridique = src.AccountFormeJuridique,
                dest.AccountStaffSizeSlice = src.AccountStaffSizeSlice,
                dest.AccountEscCategory = src.AccountEscCategory,
                dest.AccountCodeFormeJuridique = src.AccountCodeFormeJuridique,
                dest.AccountEmail = src.AccountEmail,
                dest.AccountInsertedDate = src.AccountInsertedDate,
                dest.AccountUpdatedDate = src.AccountUpdatedDate,
                dest.DeliveryAddressLine1 = src.DeliveryAddressLine1,
                dest.DeliveryAddressLine2 = src.DeliveryAddressLine2,
                dest.DeliveryAddressLine3 = src.DeliveryAddressLine3,
                dest.DeliveryCity = src.DeliveryCity,
                dest.DeliveryZipCode = src.DeliveryZipCode,
                dest.DeliveryCountry = src.DeliveryCountry,
                dest.DeliveryState = src.DeliveryState,
                dest.BillingAddressLine1 = src.BillingAddressLine1,
                dest.BillingAddressLine2 = src.BillingAddressLine2,
                dest.BillingAddressLine3 = src.BillingAddressLine3,
                dest.BillingCity = src.BillingCity,
                dest.BillingZipCode = src.BillingZipCode,
                dest.BillingCountry = src.BillingCountry,
                dest.BillingState = src.BillingState,
                dest.DeploymentStatus = src.DeploymentStatus,
                dest.DeploymentDate = src.DeploymentDate,
                dest.CreatedBy = src.CreatedBy,
                dest.ModifiedBy = src.ModifiedBy,
                dest.DeliveryPhone = src.AccountDeliveryPhone,
                dest.BillingPhone = src.AccountBillingPhone


        WHEN NOT MATCHED BY source
		           AND EXISTS (
					SELECT 1 
					FROM alx.[Account] AS src
					WHERE src.AccountGlobalUniqueIdentifier = dest.Id AND src.AccountFlagEscActif = 0 AND dest.AccountFlagEscActif = 1
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
					AccountTurnover,
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
					ModifiedBy,
					DeliveryPhone, 
					BillingPhone
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
					src.AccountTurnover,
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
					src.ModifiedBy,
					src.AccountDeliveryPhone,
					src.AccountBillingPhone
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


