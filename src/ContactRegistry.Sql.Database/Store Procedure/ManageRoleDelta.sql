CREATE PROCEDURE [cre].[ManageRoleDelta]
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
		--temporay table to persist the operations
		CREATE TABLE #OutputRoleTable (
			Action NVARCHAR(10),
			[RoleId] [uniqueidentifier] NOT NULL,
			[ContactId] [uniqueidentifier] NOT NULL,
			[AccountId] [uniqueidentifier] NOT NULL,
			[Onboarded] [bit] NOT NULL,
			[Deleted] [datetime2] NULL,
		);

        MERGE INTO cre.[Role] AS dest
        USING alx.[Role] AS src
        ON ((dest.ContactId = src.ContactId) AND (dest.AccountId = src.AccountId) AND src.Onboarded = 0) 
        WHEN MATCHED  THEN
            -- update the office of the cre contact
            UPDATE SET
			dest.Deleted = GETDATE()
		WHEN NOT MATCHED AND src.Onboarded = 1  THEN
			-- add contact
	            INSERT (
				[RoleId],
                [ContactId], 
                [AccountId],
				[Onboarded],
                [Deleted]
            )
            VALUES (
				src.RoleId,
                src.ContactId, 
                src.AccountId, 
				src.[Onboarded],
                NULL
            )
		OUTPUT $action,     
	            INSERTED.RoleId,
                INSERTED.ContactId,
                INSERTED.AccountId,
                INSERTED.Onboarded,
                INSERTED.Deleted INTO #OutputRoleTable;
	
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
				WHEN Action = 'UPDATE'  THEN 'DELETE'
				WHEN Action = 'INSERT' THEN 'INSERT'
			END,
			'ROLE', 
			NULL, 
			RoleId
		FROM 
			#OutputRoleTable; 

		DROP TABLE #OutputRoleTable


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

    RETURN 0;
END;
GO


