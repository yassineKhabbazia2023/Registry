/*
 Modèle de script de pré-déploiement							
--------------------------------------------------------------------------------------
 Ce fichier contient des instructions SQL qui seront exécutées avant le script de compilation.	
 Utilisez la syntaxe SQLCMD pour inclure un fichier dans le script de pré-déploiement.			
 Exemple :      :r .\monfichier.sql								
 Utilisez la syntaxe SQLCMD pour référencer une variable dans le script de pré-déploiement.		
 Exemple :      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
-- Drop the procedures if it exists
IF OBJECT_ID('[cre].[ManageAccountDelta]', 'P') IS NOT NULL
DROP PROCEDURE [cre].[ManageAccountDelta];
GO

IF OBJECT_ID('[cre].[ManageContactDelta]', 'P') IS NOT NULL
DROP PROCEDURE [cre].[ManageContactDelta];
GO

IF OBJECT_ID('[cre].[ManageRoleDelta]', 'P') IS NOT NULL
DROP PROCEDURE [cre].[ManageContactDelta];
GO
