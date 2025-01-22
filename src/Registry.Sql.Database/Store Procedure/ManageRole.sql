CREATE PROCEDURE [reg].[ManageRole]
		
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
		@IsCustomer BIT,
		@NumberOfOnboardedRoles INT;

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

					-- make sure that account and contact exists in reg tables
					IF (@AccountId IS NOT NULL AND @ContactId IS NOT NULL)
					BEGIN 
						-- get role Id from reg.role to check if this role already exists
						Set @RoleId = (SELECT TOP 1 RoleId FROM reg.Role WHERE AccountNumber = @AccountNumber AND ContactEmail = @ContactEmail AND [Description] = @Description)
							
							-- if role does not exists 
							IF (@RoleId IS NULL)
							BEGIN

								--insert a new role and put OnBoarded = (1) if operation is INSERT and (0) if delete
								SET @RoleId = NEWID();
								INSERT INTO reg.role (ContactEmail,AccountNumber,RoleId,AccountId,ContactId,[Description], Onboarded)
								VALUES (@ContactEmail, @AccountNumber, @RoleId,@AccountId,@ContactId,@Description, (CASE WHEN @OperationType = 'DELETE' THEN 0 ELSE 1 END))
							END

							-- if role already exists update the reg.role [Onboarded] based on operation type Delete=>0 and Insert=>1
							ELSE 
							BEGIN 
								UPDATE reg.Role set Onboarded= (CASE WHEN @OperationType = 'DELETE' THEN 0 ELSE 1 END) WHERE RoleId = @RoleId
							END

							--Get Number of onboarded roles for this accountId contactId
							SET @NumberOfOnboardedRoles = (select count(1) from reg.role where AccountNumber = @AccountNumber and ContactEmail = @ContactEmail AND OnBoarded = 1)
							
							-- if number of onboarded = 0 and operation requested is delete this means we have to create DELETE operation
							IF(@NumberOfOnboardedRoles = 0 AND @OperationType = 'DELETE')
							BEGIN
								INSERT INTO reg.Operations([Operation],[Type],[EntityId],[Status],[CreationDate]) 
								VALUES (@OperationType , 'ROLE', @RoleId, 'APPROVED', GETDATE())
							END

							-- if number of onboarded = 1 and operation requested is insert this means we have to create INSERT operation
							IF(@NumberOfOnboardedRoles = 1 and @OperationType ='INSERT' )
								BEGIN
									INSERT INTO reg.Operations([Operation],[Type],[EntityId],[Status],[CreationDate]) 
									VALUES (@OperationType, 'ROLE', @RoleId, (CASE WHEN @IsCustomer = 1 THEN 'PENDING' ELSE 'APPROVED' END), GETDATE())
								END
							
							-- else we do not have to create operations, they are adding/removing new Role DESCRIPTIONS for a role already existed
							
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