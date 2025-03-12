-- Compare the result between the environment to check if all migration steps are run successfully
SELECT TOP (1000) String01, StoreName, Boolean01, DateTime01
FROM [dbo].[tblBigTable]
WHERE StoreName LIKE '%Step%'
ORDER BY DateTime01 DESC


-- Testing to trigger the migration
UPDATE [dbo].[tblBigTable]
   SET Boolean01 = 0
 WHERE String01 = 'EPiServer.Commerce.Internal.Migration.Steps.RemoveOrphanedMetaKeysStep'
GO

UPDATE [dbo].[tblBigTable]
   SET Boolean01 = 1
 WHERE Boolean01 = 0
GO

