CREATE PROCEDURE [reg].[ManageContact]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cursor CURSOR;
	DECLARE @Id UNIQUEIDENTIFIER;
    DECLARE @ContactIdIterator int,
			@ContactFlagStatus int, 
            @OfficeId UNIQUEIDENTIFIER, 
            @IsCustomer BIT, 
            --@IsActive BIT,
            @FirstName NVARCHAR(255),
            @LastName NVARCHAR(255),
            @Email NVARCHAR(255),
            @LandPhone NVARCHAR(255),
            @MobilePhone NVARCHAR(255),
            @JobDescription NVARCHAR(255),
            @Operation NVARCHAR(50);

    BEGIN TRY
		BEGIN TRANSACTION 
			SET @Cursor = CURSOR FOR
			SELECT 
			 [ContactId]
			,[ContactFlagStatus]
			,[Email]
			,[FirstName]
			,[LastName]
			,[IsCustomer]
			,[LandPhone]
			,[MobilePhone]
			,[JobDescription]
			,[OfficeId]
			,[OperationType] FROM [ref].[contact] ORDER BY ContactId ASC;

			OPEN @Cursor;
			FETCH NEXT FROM @Cursor INTO 
				@ContactIdIterator,@ContactFlagStatus, @Email, @FirstName,@LastName, @IsCustomer, @LandPhone, @MobilePhone,@JobDescription, @OfficeId, @Operation ;

        WHILE @@FETCH_STATUS = 0
        BEGIN

			

			-- check if contact does not exists by email 
			SET @Id = (SELECT TOP 1 Id FROM reg.contact r WHERE r.Email= @Email)
            
			IF (@Id is NULL)
            BEGIN

                -- check if Contact can be accepted 
			    IF(
			    ISNULL(@FirstName,'') = '' OR @FirstName= 'NO_VALUE' OR
			    ISNULL(@LastName,'') ='' OR @LastName = 'NO_VALUE' OR 
			    ISNULL(@MobilePhone,'') = '' OR @MobilePhone = 'NO_VALUE'
			    )
			    BEGIN 
                    INSERT INTO [reg].[audit]([Type],[Operation],[EntityId],[Reason],[CreationDate])
					VALUES
					('CONTACT',@Operation,@ContactIdIterator,
        CASE WHEN ISNULL(@FirstName,'') = '' THEN 'FirstName is NULL or NO_VALUE, ' ELSE '' END +
        CASE WHEN ISNULL(@LastName,'') = '' THEN 'LastName is NULL or NO_VALUE, ' ELSE '' END +
        CASE WHEN ISNULL(@MobilePhone,'') = '' THEN 'MobilePhone is NULL or NO_VALUE' ELSE '' END,
		GETDATE())

				    GOTO NEXT_ITERATION;
			    END

			-- check if contact does not  exists by first name , last name and phone number or If it is collab
                IF NOT EXISTS (SELECT 1 FROM reg.contact r WHERE r.FirstName = @FirstName AND r.LastName = @LastName AND r.MobilePhone = @MobilePhone and r.IsCustomer = 1) OR @IsCustomer = 0
                BEGIN

				IF(@Operation <> 'INSERT')
				BEGIN 
				 INSERT INTO [reg].[audit]([Type],[Operation],[EntityId],[Reason],[CreationDate])
					VALUES
										  ('CONTACT',@Operation,@ContactIdIterator,'Operation of type '+@Operation+' contact with email '+@Email+' that does not exists', GETDATE())
					GOTO NEXT_ITERATION;
				END

				set @Id = NEWID()
                    INSERT INTO reg.Contact(Id, OfficeId, IsCustomer, IsActive, FirstName, LastName, Email, LandPhone, MobilePhone, JobDescription)
                    VALUES (@Id , @OfficeId, IsNULL(@IsCustomer,0), 1, IsNULL(@FirstName,''), IsNull(@LastName,''), @Email, @LandPhone, @MobilePhone, @JobDescription);
                END
				
                ELSE
                BEGIN
				-- if he has different email but same first name , last name and phone number this means we need to update the contact 
                    SET @Id = (SELECT TOP 1 Id FROM reg.contact WHERE FirstName = @FirstName AND LastName = @LastName AND MobilePhone = @MobilePhone)
				
					UPDATE reg.Contact
                    SET Email = @Email
                    WHERE Id = @Id
                END
        END
            ELSE
            BEGIN
				IF(@Operation = 'UPDATE')
				BEGIN 
						-- if contact exists do the regular update 
					UPDATE reg
					SET 
						reg.OfficeId = @OfficeId,
						reg.IsCustomer = @IsCustomer,
						reg.IsActive = 1,
						reg.FirstName = @FirstName,
						reg.LastName = @LastName,
						reg.LandPhone = @LandPhone,
						reg.MobilePhone = @MobilePhone,
						reg.JobDescription = @JobDescription
					FROM reg.Contact reg
					WHERE reg.Email = @Email
				END

			
            END


			insert into reg.Operations(Operation, Type, EntityId, Status,  CreationDate)
			values (
			case 
			WHEN @Operation = 'INSERT' THEN 'INSERT'
			WHEN @Operation = 'DELETE' THEN 'DELETE'
			WHEN @Operation = 'UPDATE' THEN 'UPDATE' 
			ELSE 'UNKNOWN'
			END,
			 'CONTACT', @Id, 'APPROVED', GETDATE())
			
           NEXT_ITERATION:
             FETCH NEXT FROM @Cursor INTO 
            @ContactIdIterator, @ContactFlagStatus, @Email, @FirstName,@LastName, @IsCustomer, @LandPhone, @MobilePhone,@JobDescription, @OfficeId, @Operation;
        
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