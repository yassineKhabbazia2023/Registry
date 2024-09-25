CREATE PROCEDURE [cre].[ManageRoleDelta]
AS
BEGIN
    -- Clean all role due to missmatch contact
    DELETE FROM alx.Role 
	where RoleId in 
	(
		select r.RoleId from alx.Role r 
		left join cre.Contact c on r.ContactId = c.Id
		left join cre.Account a on r.AccountId = a.Id
		where c.Id is NULL or a.Id is NULL
	)
    BEGIN TRY
        BEGIN TRANSACTION;
		--temporay table to persist the operations
		CREATE TABLE #OutputRoleTable (
			[Action] NVARCHAR(10),
	        [ContactId] [uniqueidentifier] NOT NULL,
	        [AccountId] [uniqueidentifier] NOT NULL,
	        [Deleted] [datetime2](7) NULL,
	        [RoleId] [uniqueidentifier] NOT NULL,
	        [Onboarded] [bit] NOT NULL,
            [RoleDelegataireEmail] [nvarchar](200),
            [RoleSignatory] [BIT] NULL,
            [IsFavorite] [BIT] NULL
		);

		-- Merge operation to synchronize roles between alx.Role and cre.Role
        MERGE INTO cre.[Role] AS dest
        USING alx.[Role] AS src
        -- Try to find a Role row that exists in both tables (src and dest) using ContactId and AccountId
        ON ((dest.ContactId = src.ContactId) AND (dest.AccountId = src.AccountId))
        -- If the row exists, skip and do nothing (the matched case exists only to satisfy the MERGE syntax)
        WHEN MATCHED AND (1 <> 1) THEN
            UPDATE SET
                dest.Deleted = dest.Deleted
        -- If the row does not exist in the source (src) but exists in the destination (dest) with Onboarded = 1, perform a soft delete
        WHEN NOT MATCHED BY SOURCE AND EXISTS(SELECT 1 FROM cre.Role cre WHERE cre.ContactId = dest.ContactId AND cre.AccountId = dest.AccountId AND cre.Onboarded = 1) THEN
            UPDATE SET
                dest.Onboarded = 0,
                dest.Deleted = GETDATE()
        -- If the row does not exist in the destination (dest) but exists in the source (src), create the role row
        WHEN NOT MATCHED BY TARGET AND NOT EXISTS(SELECT 1 FROM cre.Role cre WHERE cre.ContactId = src.ContactId AND cre.AccountId = src.AccountId AND cre.Onboarded = 1) THEN
            INSERT (
                [RoleId],
                [ContactId], 
                [AccountId],
                [Onboarded],
                [Deleted],
                [RoleDelegataireEmail],
                [RoleSignatory],
                [IsFavorite]
            )
            VALUES (
                src.RoleId,
                src.ContactId, 
                src.AccountId, 
                src.Onboarded,
                NULL,
                src.RoleDelegataireEmail,
                src.RoleSignatory,
                src.IsFavorite
            )
        -- Output the action and inserted rows into the temporary table
        OUTPUT $action, INSERTED.* INTO #OutputRoleTable;
    
        -- Commit the transaction
        COMMIT TRANSACTION;

        -- Insert the performed operations into the ged operations table
        INSERT INTO [cre].[Operations]
            (
            [Operation],
            [Type],
            [PublishedAt],
            [EntityId]
            )
        SELECT 
            CASE 
                WHEN Action = 'UPDATE' THEN 'DELETE'
                WHEN Action = 'INSERT' THEN 'INSERT'
            END,
            'ROLE', 
            NULL, 
            RoleId
        FROM 
            #OutputRoleTable; 

        -- Drop the temporary table
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