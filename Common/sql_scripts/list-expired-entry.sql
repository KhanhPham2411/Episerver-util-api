SELECT TOP (1000) [WorkId]
      ,[ObjectId] as EntryId
      ,[ObjectTypeId]
      ,[CatalogId]
      ,[Name]
      ,[Code]
      ,[LanguageName]
      ,[MasterLanguageName]
      ,[IsCommonDraft]
      ,[StartPublish]
      ,[StopPublish]
FROM [dbo].[ecfVersion]
WHERE [StopPublish] < GETDATE()
ORDER BY [StopPublish] ASC
