SELECT TOP (1000) c.[pkID]
	,cl.[Name]
	,cl.[URLSegment]
	,ct.[Name] as TypeName
	,cl.[Created]
	,[fkParentID]
  ,[fkContentTypeID]

  FROM [dbo].[tblContent] c
  JOIN [dbo].[tblContentType] ct on c.fkContentTypeID = ct.pkID
  JOIN [dbo].[tblContentLanguage] cl on cl.fkContentID = c.pkID
  where c.pkID in (87665, 88181)

-- Add mapped identity info
SELECT TOP (1000) c.[pkID]
	,cl.[Created],cl.[Name] ,ct.[Name] as TypeName
	,mi.[Provider] ,mi.[ProviderUniqueID]
	,cl.[URLSegment]
	,[fkParentID]
  	,[fkContentTypeID]
FROM [dbo].[tblContent] c
  JOIN [dbo].[tblContentType] ct on c.fkContentTypeID = ct.pkID
  JOIN [dbo].[tblContentLanguage] cl on cl.fkContentID = c.pkID
  LEFT JOIN [dbo].[tblMappedIdentity] mi on mi.[ExistingContentId] = c.pkID
WHERE c.pkID in (88387, 18971)

