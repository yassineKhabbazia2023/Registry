CREATE PROCEDURE [re].[ManageRole]
		
AS
BEGIN
	
	SET NOCOUNT ON;

	DECLARE @Cursor CURSOR; 

	DECLARE  
	   @ContactEmail NVARCHAR(255)
      ,@AccountNumber NVARCHAR(50)
      ,@RoleFlagStatus INT
      ,@Description NVARCHAR(100)
      ,@OperationType NVARCHAR(20)
      ,@OperationDate DATETIME2(7);

	DECLARE 
		@AccountId UNIQUEIDENTIFIER,
		@ContactId UNIQUEIDENTIFIER,
		@RoleId UNIQUEIDENTIFIER,
		@IsCustomer BIT;

	BEGIN TRY 
		BEGIN TRANSACTION
			SET @Cursor = CURSOR FOR  SELECT 
											 [ContactEmail]
											,[AccountNumber]
											,[RoleFlagStatus]
											,[Description]
											,[OperationType]
											,[OperationDate]
											FROM [ref].[Role] ORDER BY RoleId ASC
			OPEN @Cursor 
			FETCH NEXT FROM @Cursor INTO 
								         @ContactEmail
										,@AccountNumber
										,@RoleFlagStatus
										,@Description
										,@OperationType
										,@OperationDate

			WHILE @@FETCH_STATUS = 0 
				BEGIN
					set @AccountId = (SELECT TOP 1 Id FROM reg.Account where AccountNumber = @AccountNumber)
					set @ContactId = (SELECT TOP 1 Id FROM reg.Contact where Email = @ContactEmail)
					set @IsCustomer = (SELECT TOP 1 IsCustomer FROM reg.Contact where Email = @ContactEmail)

					IF (@AccountId IS NOT NULL AND @ContactId IS NOT NULL)
					BEGIN 
						Set @RoleId = (SELECT TOP 1 RoleId FROM reg.Role WHERE AccountNumber = @AccountNumber AND ContactEmail = @ContactEmail)
							
							IF (@RoleId IS NULL)
							BEGIN
								SET @RoleId = NEWID();
								INSERT INTO reg.role (ContactEmail,AccountNumber,RoleId,AccountId,ContactId)
								VALUES (@ContactEmail, @AccountNumber, @RoleId,@AccountId,@ContactId)
							END

							INSERT INTO reg.Operations([Operation],[Type],[EntityId],[Status],[CreationDate]) 
							VALUES (@OperationType , 'ROLE', @RoleId, CASE WHEN (@IsCustomer = 1 AND @OperationType <> 'DELETE') THEN 'PENDING' ELSE 'APPROVED' END, GETDATE())

					END

					FETCH NEXT FROM @Cursor INTO 
								         @ContactEmail
										,@AccountNumber
										,@RoleFlagStatus
										,@Description
										,@OperationType
										,@OperationDate
				END
		CLOSE @Cursor;
        DEALLOCATE @Cursor;
		COMMIT TRANSACTION 

		-- truncate table only if the transaction is commited 
		TRUNCATE TABLE [ref].[role]

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
END
