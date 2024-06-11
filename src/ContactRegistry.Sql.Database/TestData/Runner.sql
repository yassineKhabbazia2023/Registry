-- Test Runner

-- Run the script DataToTestAccountMergeOperations.sql
-- Run the script DataToTestContactMergeOperations.sql
-- Run the script DataToTestRoleMergeOperations.sql

-- Clean up the tables
DELETE FROM [alx].[role];
DELETE FROM [cre].[role];

DELETE FROM [alx].[Contact];
DELETE FROM [cre].[Contact];

DELETE FROM [alx].[account];
DELETE FROM [cre].[account];

DELETE FROM [cre].[Operations];

-- Execute stored procedures
EXECUTE [dbo].[ManageContactDelta];
GO

EXECUTE [dbo].[ManageAccountDelta];
GO

EXECUTE [dbo].[ManageRoleDelta];
GO

-- Select statements to verify results
SELECT * FROM [alx].[Contact];
SELECT * FROM [cre].[Operations] WHERE [Type] = 'CONTACT';
SELECT * FROM [cre].[Contact];

SELECT * FROM [alx].[account];
SELECT * FROM [cre].[Operations] WHERE [Type] = 'ACCOUNT';
SELECT * FROM [cre].[account];

SELECT * FROM [alx].[role];
SELECT * FROM [cre].[Operations] WHERE [Type] = 'ROLE';
SELECT * FROM [cre].[role];
