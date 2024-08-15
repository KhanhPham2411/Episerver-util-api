
SELECT TOP (1000) c.[pkID],
  ct.Name
      ,[fkContentTypeID]
      ,[fkParentID]

  FROM [dbo].[tblContent] c
  JOIN [dbo].[tblContentType] ct on c.fkContentTypeID = ct.pkID
  where c.pkID in (326, 173, 3, 1)
