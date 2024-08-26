SELECT m.[ExistingContentId]
FROM [dbo].[tblMappedIdentity] m
LEFT JOIN [dbo].[tblContent] c ON m.[ExistingContentId] = c.[pkID]
WHERE c.[pkID] IS NULL
