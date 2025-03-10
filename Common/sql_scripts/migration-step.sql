-- Compare the result between environment to check if all migration steps are run successfully
SELECT TOP (1000) String01, StoreName, Boolean01, DateTime01
FROM [dbo].[tblBigTable]
WHere StoreName like '%Step%'
ORDER BY DateTime01 desc
