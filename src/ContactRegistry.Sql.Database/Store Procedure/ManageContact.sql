CREATE PROCEDURE [reg].[ManageContact]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cursor CURSOR;
	DECLARE @Id UNIQUEIDENTIFIER;
	DECLARE @Count int;
    DECLARE @ContactIdIterator int,
			@ContactFlagStatus int, 
            @OfficeCode NVARCHAR(50), 
            @IsCustomer BIT, 
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
			,[OfficeCode]
			,[OperationType] FROM [ref].[contact] ORDER BY ContactId ASC;

			OPEN @Cursor;
			FETCH NEXT FROM @Cursor INTO 
				@ContactIdIterator,@ContactFlagStatus, @Email, @FirstName,@LastName, @IsCustomer, @LandPhone, @MobilePhone,@JobDescription, @OfficeCode, @Operation ;

        WHILE @@FETCH_STATUS = 0
        BEGIN

			

			-- check if contact does not exists by email 
			SET @Id = (SELECT TOP 1 Id FROM reg.contact r WHERE r.Email= @Email)
            
			IF (@Id is NULL)
            BEGIN

                -- check if Contact can be accepted 
			    IF(
				ISNULL(@Email,'') = '' OR
			    ISNULL(@FirstName,'') = '' OR @FirstName= 'NO_VALUE' OR
			    ISNULL(@LastName,'') ='' OR @LastName = 'NO_VALUE' 
			    )
			    BEGIN 
                    INSERT INTO [reg].[audit]([Type],[Operation],[EntityId],[Reason],[CreationDate])
					VALUES
					('CONTACT',@Operation,@ContactIdIterator,
        CASE WHEN ISNULL(@Email,'') = '' THEN 'Email is null, ' ELSE '' END + CASE WHEN ISNULL(@FirstName,'') = '' THEN 'FirstName is NULL or NO_VALUE, ' ELSE '' END +
        CASE WHEN ISNULL(@LastName,'') = '' THEN 'LastName is NULL or NO_VALUE, ' ELSE '' END,
		GETDATE())

				    GOTO NEXT_ITERATION;
			    END

			-- check if contact does not  exists by first name , last name and phone number 
                IF NOT EXISTS (SELECT 1 FROM reg.contact r WHERE r.FirstName = @FirstName AND r.LastName = @LastName AND r.MobilePhone = @MobilePhone)
                BEGIN

				IF(@Operation <> 'INSERT')
				BEGIN 
				 INSERT INTO [reg].[audit]([Type],[Operation],[EntityId],[Reason],[CreationDate])
					VALUES
										  ('CONTACT',@Operation,@ContactIdIterator,'Operation of type '+@Operation+' contact with email '+IsNUll(@Email,'')+' that does not exists', GETDATE())
					GOTO NEXT_ITERATION;
				END

				set @Id = NEWID()
                    INSERT INTO reg.Contact(Id, OfficeCode, IsCustomer, IsActive, FirstName, LastName, Email, LandPhone, MobilePhone, JobDescription)
                    VALUES (@Id , @OfficeCode, IsNULL(@IsCustomer,0), 1, IsNULL(@FirstName,''), IsNull(@LastName,''), @Email, @LandPhone, @MobilePhone, @JobDescription);
                END

				ELSE
                BEGIN
				    -- Récupérer l'Id et compter le nombre de correspondances
				    SELECT @Id = Id, @Count = COUNT(*)
				    FROM reg.contact
				    WHERE FirstName = @FirstName AND LastName = @LastName AND MobilePhone = @MobilePhone
					GROUP BY Id;
				
				    -- Mettre à jour uniquement si @Id a été définie
					IF @Count = 1
					BEGIN
					    UPDATE reg.Contact
					    SET Email = @Email
					    WHERE Id = @Id;
					END
					-- Cas 2 : Plus d'une correspondance, logger l'information
					ELSE IF @Count > 1
					BEGIN
					    INSERT INTO [reg].[audit]([Type],[Operation],[EntityId],[Reason],[CreationDate])
						VALUES
										  ('CONTACT',@Operation,@ContactIdIterator,'The Operation '+@Operation+' contact with change email has aborded beacause the search by FirstName: '+ISNULL(@FirstName,'')+', LastName: '+ISNULL(@LastName,'')+', MobilePhone: '+ISNULL(@MobilePhone,'')+' has returned more than one result ', GETDATE())
						GOTO NEXT_ITERATION;
					END;
				END;
        END
            ELSE
            BEGIN
				IF(@Operation = 'UPDATE')
				BEGIN 
						-- if contact exists do the regular update 
					UPDATE reg
					SET 
						reg.OfficeCode = @OfficeCode,
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
            @ContactIdIterator, @ContactFlagStatus, @Email, @FirstName,@LastName, @IsCustomer, @LandPhone, @MobilePhone,@JobDescription, @OfficeCode, @Operation;
        
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