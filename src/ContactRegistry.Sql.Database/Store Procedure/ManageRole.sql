CREATE PROCEDURE [re].[ManageRole]
		
AS
BEGIN
	
	SET NOCOUNT ON;

	DECLARE @Cursor CURSOR; 

	DECLARE
	   @RoleIdIterator INT 
	  ,@ContactEmail NVARCHAR(255)
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
			SET @Cursor = CURSOR FOR  SELECT [RoleId]
											,[ContactEmail]
											,[AccountNumber]
											,[RoleFlagStatus]
											,[Description]
											,[OperationType]
											,[OperationDate]
											FROM [ref].[Role] ORDER BY RoleId ASC
			OPEN @Cursor 
			FETCH NEXT FROM @Cursor INTO @RoleIdIterator
								        ,@ContactEmail
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
								
								IF(@OperationType = 'DELETE')
								BEGIN 
									INSERT INTO [reg].[audit]([EntityId],[Type],[Operation],[Reason],[CreationDate])
									VALUES
									(''+@AccountNumber+';'+@ContactEmail+'','ROLE',@OperationType,'Operation Of Type '+@OperationType+' while the Role for Account '+@AccountNumber+' and Email '+@ContactEmail+' Does not exists',GETDATE())
									GOTO NEXT_ITERATION
								END

								SET @RoleId = NEWID();
								INSERT INTO reg.role (ContactEmail,AccountNumber,RoleId,AccountId,ContactId)
								VALUES (@ContactEmail, @AccountNumber, @RoleId,@AccountId,@ContactId)
							END

							INSERT INTO reg.Operations([Operation],[Type],[EntityId],[Status],[CreationDate]) 
							VALUES (@OperationType , 'ROLE', @RoleId, CASE WHEN (@IsCustomer = 1 AND @OperationType <> 'DELETE') THEN 'PENDING' ELSE 'APPROVED' END, GETDATE())
					END
					ELSE 
						BEGIN
							INSERT INTO [reg].[audit]([EntityId],[Type],[Operation],[Reason],[CreationDate])
									VALUES
									(@RoleIdIterator,'ROLE',@OperationType, 
									CASE WHEN @AccountId IS NULL THEN 'AccountId Not Found' ELSE '' END + 
									CASE WHEN  @ContactId IS NULL THEN 'ContactId Not Found' ELSE '' END
									, GETDATE())
									GOTO NEXT_ITERATION 
						END

					NEXT_ITERATION:
						FETCH NEXT FROM @Cursor INTO 
										 @RoleIdIterator	
								        ,@ContactEmail
										,@AccountNumber
										,@RoleFlagStatus
										,@Description
										,@OperationType
										,@OperationDate
				END
		CLOSE @Cursor;
        DEALLOCATE @Cursor;
		COMMIT TRANSACTION 


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