WITH MaxWorkIdPerEntry AS (
    SELECT [ObjectId] AS EntryId,
           MAX([WorkId]) AS MaxWorkId
    FROM [dbo].[ecfVersion]
    GROUP BY [ObjectId]
)
SELECT TOP (1000) 
    ev.[WorkId],      
    ev.[ObjectId] AS EntryId,     
    ev.[ObjectTypeId],      
    ev.[CatalogId],      
    ev.[Name],      
    ev.[Code],      
    ev.[LanguageName],      
    ev.[MasterLanguageName],      
    ev.[IsCommonDraft],      
    ev.[StartPublish],      
    ev.[StopPublish]
FROM [dbo].[ecfVersion] ev
INNER JOIN MaxWorkIdPerEntry mwe
    ON ev.[ObjectId] = mwe.EntryId
    AND ev.[WorkId] = mwe.MaxWorkId
WHERE ev.[StopPublish] < GETDATE()
ORDER BY ev.[StopPublish] ASC;
