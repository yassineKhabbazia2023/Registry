CREATE PROCEDURE [cre].[ManageContactDelta]
AS
BEGIN

BEGIN TRY
    BEGIN TRANSACTION;
    
    -- Temporary table to persist the operations
    CREATE TABLE #OutputContactTable (
        Action NVARCHAR(10),
        Id UNIQUEIDENTIFIER,
        OfficeId UNIQUEIDENTIFIER,
        IsCustomer BIT,
        IsActive BIT,
        Updated DATETIME,
        Deleted DATETIME,
        FirstName NVARCHAR(255),
        LastName NVARCHAR(255),
        Email NVARCHAR(255),
        LandPhone NVARCHAR(255),
        MobilePhone NVARCHAR(255),
        JobDescription NVARCHAR(255),
        Source NVARCHAR(50)
    );

    MERGE INTO cre.[Contact] AS dest
    USING alx.[Contact] AS src
    ON (dest.Email = src.Email AND src.IsActive = 1)
    WHEN MATCHED  AND 
	(
			ISNULL(dest.OfficeId,'00000000-0000-0000-0000-000000000000') <> ISNULL(src.OfficeId,'00000000-0000-0000-0000-000000000000')
	)
	OR (dest.IsActive = 0)
	THEN 
	
        -- Update the office of the cre contact if the OfficeIds are different
		-- Update the isActive of the cre contact if the IsActive from the source is TRUE
		UPDATE SET 
            dest.OfficeId = src.OfficeId ,
			dest.IsActive = src.IsActive,
            dest.Updated = GETDATE(),
			dest.Deleted = NULL

    WHEN NOT MATCHED BY SOURCE
        AND EXISTS (
            SELECT 1 
            FROM cre.[Contact] AS cre
            WHERE cre.Email = dest.Email AND dest.IsActive = 1
        ) THEN
        -- Soft delete contact from cre
        UPDATE SET 
            dest.IsActive = 0, 
            dest.Deleted = GETDATE()
    
    WHEN NOT MATCHED BY TARGET AND src.IsActive = 1 THEN
        -- Add contact
        INSERT (
            Id, 
            OfficeId, 
            IsCustomer, 
            IsActive, 
            Updated, 
            Deleted, 
            FirstName, 
            LastName, 
            Email, 
            LandPhone, 
            MobilePhone, 
            JobDescription, 
            Source
        )
        VALUES (
            src.Id, 
            src.OfficeId, 
            src.IsCustomer, 
            src.IsActive, 
            NULL,  
            NULL,
            src.FirstName, 
            src.LastName, 
            src.Email, 
            src.LandPhone, 
            src.MobilePhone, 
            src.JobDescription, 
            'registry' 
        )
    OUTPUT $action, INSERTED.* INTO #OutputContactTable;

    COMMIT TRANSACTION;

    -- Insert into the ged operations table
    INSERT INTO [cre].[Operations]
    (
        [Operation],
        [Type],
        [PublishedAt],
        [EntityId]
    )
    SELECT 
        CASE 
            WHEN Action = 'UPDATE' AND Updated IS NOT NULL THEN 'UPDATE' 
            WHEN Action = 'UPDATE' AND Deleted IS NOT NULL THEN 'DELETE'
            WHEN Action = 'INSERT' THEN 'INSERT'
            ELSE NULL 
        END,
        'CONTACT', 
        NULL, 
        Id
    FROM 
        #OutputContactTable; 

    DROP TABLE #OutputContactTable;

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
END;