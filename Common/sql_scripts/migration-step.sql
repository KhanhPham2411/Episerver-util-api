-- Compare the result between the environment to check if all migration steps are run successfully
SELECT TOP (1000) String01, StoreName, Boolean01, DateTime01
FROM [dbo].[tblBigTable]
WHere StoreName like '%Step%'
ORDER BY DateTime01 desc


-- Testing to trigger the migration
UPDATE [dbo].[tblBigTable]
   SET Boolean01 = 0
 WHERE String01 = 'EPiServer.Commerce.Internal.Migration.Steps.RemoveOrphanedMetaKeysStep'
GO


