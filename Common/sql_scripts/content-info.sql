SELECT TOP (1000) c.[pkID], lb.[LanguageID], lb.[Name]
	,cl.[Created],cl.[Name] ,ct.[Name] as TypeName
	,cl.[URLSegment]
	,[fkParentID]
  	,[fkContentTypeID]
	,mi.[Provider] ,mi.[ProviderUniqueID]
FROM [dbo].[tblContent] c
  JOIN [dbo].[tblContentType] ct on c.fkContentTypeID = ct.pkID
  JOIN [dbo].[tblContentLanguage] cl on cl.fkContentID = c.pkID
  JOIN [dbo].[tblLanguageBranch] lb on lb.pkID =  cl.[fkLanguageBranchID]
  LEFT JOIN [dbo].[tblMappedIdentity] mi on mi.[ExistingContentId] = c.pkID
WHERE cl.[URLSegment] like '%kkk%'
