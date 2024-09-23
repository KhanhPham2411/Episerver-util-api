SELECT TOP (1000) c.[pkID]
	,ct.[Name]
	,cl.[URLSegment]
	,cl.[Created]
	,[fkParentID]
  ,[fkContentTypeID]

  FROM [dbo].[tblContent] c
  JOIN [dbo].[tblContentType] ct on c.fkContentTypeID = ct.pkID
  JOIN [dbo].[tblContentLanguage] cl on cl.fkContentID = c.pkID
  where c.pkID in (87665, 88181)
