-- Compare the result 
SELECT TOP (1000) String01, StoreName, Boolean01, DateTime01
FROM [dbo].[tblBigTable]
WHere StoreName like '%Step%'
