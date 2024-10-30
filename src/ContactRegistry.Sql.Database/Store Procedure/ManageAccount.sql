CREATE PROCEDURE [re].[ManageAccount]
	AS
BEGIN
	
	SET NOCOUNT ON;

	DECLARE @Cursor CURSOR; 
	DECLARE @Id UNIQUEIDENTIFIER;
	
	DECLARE 
	@AccountFlagStatus int,
	@LegalName nvarchar(255) ,
	@AccountNumber nvarchar(50) ,
	@AccountCommercialName nvarchar(255) ,
	@AccountType nvarchar(50) ,
	@AccountEmail nvarchar(100) ,
	@AccountNafIdentifier nvarchar(50) ,
	@AccountSectorCode nvarchar(50) ,
	@AccountTaxeValeurAjoutee nvarchar(50) ,
	@AccountDeliveryEmail nvarchar(255) ,
	@AccountBillingEmail nvarchar(255) ,
	@AccountTaxationSystem nvarchar(100) ,
	@AccountSourceName nvarchar(100) ,
	@AccountISIN nvarchar(100) ,
	@AccountRegisterIdentification1 nvarchar(50) ,
	@AccountStaffSize nvarchar(50) ,
	@AccountDeliveryFax nvarchar(50) ,
	@AccountBillingFax nvarchar(50) ,
	@AccountTurnover nvarchar(50) ,
	@AccountRegimeFiscal nvarchar(50) ,
	@AccountTypeTenueComptable nvarchar(50) ,
	@AccountFormeJuridique nvarchar(255) ,
	@AccountStaffSizeSlice nvarchar(50) ,
	@AccountEscCategory nvarchar(50) ,
	@AccountCodeFormeJuridique nvarchar(50) ,
	@AccountInsertedDate datetime2(7) ,
	@AccountUpdatedDate datetime2(7) ,
	@DeliveryAddressLine1 nvarchar(255) ,
	@DeliveryAddressLine2 nvarchar(255) ,
	@DeliveryAddressLine3 nvarchar(255) ,
	@DeliveryCity nvarchar(50) ,
	@DeliveryZipCode nvarchar(20) ,
	@DeliveryCountry nvarchar(50) ,
	@DeliveryState nvarchar(50) ,
	@BillingAddressLine1 nvarchar(255) ,
	@BillingAddressLine2 nvarchar(255) ,
	@BillingAddressLine3 nvarchar(255) ,
	@BillingCity nvarchar(50) ,
	@BillingZipCode nvarchar(20) ,
	@BillingCountry nvarchar(50) ,
	@BillingState nvarchar(50) ,
	@DeploymentStatus nvarchar(50) ,
	@DeploymentDate datetime2(7) ,
	@AccountDeliveryPhone nvarchar(50) ,
	@AccountBillingPhone nvarchar(50) ,
	@OperationType nvarchar(20); 

	SET @Cursor = CURSOR FOR SELECT 
	   [AccountFlagStatus]
      ,[LegalName]
      ,[AccountNumber]
      ,[AccountCommercialName]
      ,[AccountType]
      ,[AccountEmail]
      ,[AccountNafIdentifier]
      ,[AccountSectorCode]
      ,[AccountTaxeValeurAjoutee]
      ,[AccountDeliveryEmail]
      ,[AccountBillingEmail]
      ,[AccountTaxationSystem]
      ,[AccountSourceName]
      ,[AccountISIN]
      ,[AccountRegisterIdentification1]
      ,[AccountStaffSize]
      ,[AccountDeliveryFax]
      ,[AccountBillingFax]
      ,[AccountTurnover]
      ,[AccountRegimeFiscal]
      ,[AccountTypeTenueComptable]
      ,[AccountFormeJuridique]
      ,[AccountStaffSizeSlice]
      ,[AccountEscCategory]
      ,[AccountCodeFormeJuridique]
      ,[AccountInsertedDate]
      ,[AccountUpdatedDate]
      ,[DeliveryAddressLine1]
      ,[DeliveryAddressLine2]
      ,[DeliveryAddressLine3]
      ,[DeliveryCity]
      ,[DeliveryZipCode]
      ,[DeliveryCountry]
      ,[DeliveryState]
      ,[BillingAddressLine1]
      ,[BillingAddressLine2]
      ,[BillingAddressLine3]
      ,[BillingCity]
      ,[BillingZipCode]
      ,[BillingCountry]
      ,[BillingState]
      ,[DeploymentStatus]
      ,[DeploymentDate]
      ,[AccountDeliveryPhone]
      ,[AccountBillingPhone]
      ,[OperationType]
	  FROM ref.Account 
	BEGIN TRY 
		BEGIN TRANSACTION 
	  OPEN @Cursor
	  FETCH NEXT FROM @Cursor INTO 
		@AccountFlagStatus,
		@LegalName  ,
		@AccountNumber  ,
		@AccountCommercialName ,
		@AccountType ,
		@AccountEmail  ,
		@AccountNafIdentifier ,
		@AccountSectorCode,
		@AccountTaxeValeurAjoutee ,
		@AccountDeliveryEmail ,
		@AccountBillingEmail ,
		@AccountTaxationSystem ,
		@AccountSourceName ,
		@AccountISIN ,
		@AccountRegisterIdentification1,
		@AccountStaffSize,
		@AccountDeliveryFax,
		@AccountBillingFax,
		@AccountTurnover,
		@AccountRegimeFiscal,
		@AccountTypeTenueComptable,
		@AccountFormeJuridique,
		@AccountStaffSizeSlice,
		@AccountEscCategory,
		@AccountCodeFormeJuridique,
		@AccountInsertedDate,
		@AccountUpdatedDate,
		@DeliveryAddressLine1,
		@DeliveryAddressLine2,
		@DeliveryAddressLine3,
		@DeliveryCity,
		@DeliveryZipCode,
		@DeliveryCountry,
		@DeliveryState,
		@BillingAddressLine1,
		@BillingAddressLine2,
		@BillingAddressLine3,
		@BillingCity,
		@BillingZipCode,
		@BillingCountry,
		@BillingState,
		@DeploymentStatus,
		@DeploymentDate,
		@AccountDeliveryPhone ,
		@AccountBillingPhone,
		@OperationType;
		

		WHILE @@FETCH_STATUS = 0 
		BEGIN
			set @Id = (SELECT TOP 1 Id FROM reg.Account WHERE AccountNumber = @AccountNumber)
			
			IF(@Id Is Null)
			BEGIN
			SET @Id = NEWID()
				INSERT INTO reg.Account(
				   [Id]
				  --,[AccountFlagStatus]
				  ,[LegalName]
				  ,[AccountNumber]
				  ,[AccountCommercialName]
				  ,[AccountType]
				  ,[AccountEmail]
				  ,[AccountNafIdentifier]
				  ,[AccountSectorCode]
				  ,[AccountTaxeValeurAjoutee]
				  ,[AccountDeliveryEmail]
				  ,[AccountBillingEmail]
				  ,[AccountTaxationSystem]
				  ,[AccountSourceName]
				  ,[AccountISIN]
				  ,[AccountRegisterIdentification1]
				  ,[AccountStaffSize]
				  ,[AccountDeliveryFax]
				  ,[AccountBillingFax]
				  ,[AccountTurnover]
				  ,[AccountRegimeFiscal]
				  ,[AccountTypeTenueComptable]
				  ,[AccountFormeJuridique]
				  ,[AccountStaffSizeSlice]
				  ,[AccountEscCategory]
				  ,[AccountCodeFormeJuridique]
				  ,[AccountInsertedDate]
				  ,[AccountUpdatedDate]
				  ,[DeliveryAddressLine1]
				  ,[DeliveryAddressLine2]
				  ,[DeliveryAddressLine3]
				  ,[DeliveryCity]
				  ,[DeliveryZipCode]
				  ,[DeliveryCountry]
				  ,[DeliveryState]
				  ,[BillingAddressLine1]
				  ,[BillingAddressLine2]
				  ,[BillingAddressLine3]
				  ,[BillingCity]
				  ,[BillingZipCode]
				  ,[BillingCountry]
				  ,[BillingState]
				  ,[DeploymentStatus]
				  ,[DeploymentDate],
				  AccountFlagEscActif
				  )
				  --,[AccountDeliveryPhone]
				  --,[AccountBillingPhone]) 
				  VALUES
				  (
					@Id,
					--@AccountFlagStatus,
					@LegalName  ,
					@AccountNumber  ,
					@AccountCommercialName ,
					@AccountType ,
					@AccountEmail  ,
					@AccountNafIdentifier ,
					@AccountSectorCode,
					@AccountTaxeValeurAjoutee ,
					@AccountDeliveryEmail ,
					@AccountBillingEmail ,
					@AccountTaxationSystem ,
					@AccountSourceName ,
					@AccountISIN ,
					@AccountRegisterIdentification1,
					@AccountStaffSize,
					@AccountDeliveryFax,
					@AccountBillingFax,
					@AccountTurnover,
					@AccountRegimeFiscal,
					@AccountTypeTenueComptable,
					@AccountFormeJuridique,
					@AccountStaffSizeSlice,
					@AccountEscCategory,
					@AccountCodeFormeJuridique,
					@AccountInsertedDate,
					@AccountUpdatedDate,
					@DeliveryAddressLine1,
					@DeliveryAddressLine2,
					@DeliveryAddressLine3,
					@DeliveryCity,
					@DeliveryZipCode,
					@DeliveryCountry,
					@DeliveryState,
					@BillingAddressLine1,
					@BillingAddressLine2,
					@BillingAddressLine3,
					@BillingCity,
					@BillingZipCode,
					@BillingCountry,
					@BillingState,
					@DeploymentStatus,
					@DeploymentDate, 
					1
					--@AccountDeliveryPhone ,
					--@AccountBillingPhone
				  )
			END

			ELSE 
			BEGIN  
			UPDATE reg.Account 
			set 
				   --[AccountFlagStatus] = @AccountFlagStatus
				   [LegalName]=@LegalName
				  ,[AccountCommercialName] = @AccountCommercialName
				  ,[AccountType] = @AccountType
				  ,[AccountEmail]= @AccountEmail
				  ,[AccountNafIdentifier] = @AccountNafIdentifier
				  ,[AccountSectorCode] = @AccountSectorCode
				  ,[AccountTaxeValeurAjoutee] = @AccountTaxeValeurAjoutee
				  ,[AccountDeliveryEmail] = @AccountDeliveryEmail
				  ,[AccountBillingEmail] = @AccountBillingEmail
				  ,[AccountTaxationSystem] = @AccountTaxationSystem
				  ,[AccountSourceName] = @AccountSourceName
				  ,[AccountISIN]  = @AccountISIN
				  ,[AccountRegisterIdentification1] = @AccountRegisterIdentification1
				  ,[AccountStaffSize] = @AccountStaffSize
				  ,[AccountDeliveryFax] = @AccountDeliveryFax
				  ,[AccountBillingFax] = @AccountBillingFax
				  ,[AccountTurnover] = @AccountTurnover
				  ,[AccountRegimeFiscal] = @AccountRegimeFiscal
				  ,[AccountTypeTenueComptable] = @AccountTypeTenueComptable
				  ,[AccountFormeJuridique] = @AccountFormeJuridique
				  ,[AccountStaffSizeSlice] = @AccountStaffSizeSlice
				  ,[AccountEscCategory] = @AccountEscCategory
				  ,[AccountCodeFormeJuridique] = @AccountCodeFormeJuridique
				  ,[AccountInsertedDate] = @AccountInsertedDate
				  ,[AccountUpdatedDate] = @AccountUpdatedDate
				  ,[DeliveryAddressLine1] = @DeliveryAddressLine1
				  ,[DeliveryAddressLine2] = @DeliveryAddressLine2
				  ,[DeliveryAddressLine3] = @DeliveryAddressLine3
				  ,[DeliveryCity] = @DeliveryCity
				  ,[DeliveryZipCode] = @DeliveryZipCode
				  ,[DeliveryCountry] = @DeliveryCountry
				  ,[DeliveryState] = @DeliveryState
				  ,[BillingAddressLine1] = @BillingAddressLine1
				  ,[BillingAddressLine2] = @BillingAddressLine2
				  ,[BillingAddressLine3] = @BillingAddressLine3
				  ,[BillingCity] = @BillingCity
				  ,[BillingZipCode] = @BillingZipCode
				  ,[BillingCountry] = @BillingCountry
				  ,[BillingState] = @BillingState
				  ,[DeploymentStatus] = @DeploymentStatus
				  ,[DeploymentDate] = @DeploymentDate
				  --,[AccountDeliveryPhone] = @AccountDeliveryPhone
				  --,[AccountBillingPhone] = @AccountBillingPhone
			WHERE AccountNumber = @AccountNumber

			END
			select @Id 

			INSERT INTO reg.Operations 
			([Operation], [Type], [EntityId] , [Status] , [CreationDate])
			VALUES
			(@OperationType, 'ACCOUNT', @Id ,'APPROVED', GETDATE() )


			 FETCH NEXT FROM @Cursor INTO 
		@AccountFlagStatus,
		@LegalName  ,
		@AccountNumber  ,
		@AccountCommercialName ,
		@AccountType ,
		@AccountEmail  ,
		@AccountNafIdentifier ,
		@AccountSectorCode,
		@AccountTaxeValeurAjoutee ,
		@AccountDeliveryEmail ,
		@AccountBillingEmail ,
		@AccountTaxationSystem ,
		@AccountSourceName ,
		@AccountISIN ,
		@AccountRegisterIdentification1,
		@AccountStaffSize,
		@AccountDeliveryFax,
		@AccountBillingFax,
		@AccountTurnover,
		@AccountRegimeFiscal,
		@AccountTypeTenueComptable,
		@AccountFormeJuridique,
		@AccountStaffSizeSlice,
		@AccountEscCategory,
		@AccountCodeFormeJuridique,
		@AccountInsertedDate,
		@AccountUpdatedDate,
		@DeliveryAddressLine1,
		@DeliveryAddressLine2,
		@DeliveryAddressLine3,
		@DeliveryCity,
		@DeliveryZipCode,
		@DeliveryCountry,
		@DeliveryState,
		@BillingAddressLine1,
		@BillingAddressLine2,
		@BillingAddressLine3,
		@BillingCity,
		@BillingZipCode,
		@BillingCountry,
		@BillingState,
		@DeploymentStatus,
		@DeploymentDate,
		@AccountDeliveryPhone ,
		@AccountBillingPhone,
		@OperationType;


		END

		CLOSE @Cursor 
		DEALLOCATE @Cursor 

		COMMIT TRANSACTION  
	  END TRY 
	  BEGIN CATCH 

	  IF(@@TRANCOUNT > 0 )
	  BEGIN
		ROLLBACK TRANSACTION 
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



	

END

