SELECT 
    cl.[URLSegment],
    COUNT(*) AS [Count]
FROM 
    [dbo].[tblContent] c
JOIN 
    [dbo].[tblContentType] ct ON c.fkContentTypeID = ct.pkID
JOIN 
    [dbo].[tblContentLanguage] cl ON cl.fkContentID = c.pkID
WHERE 
    ct.[Name] = 'ImageMediaData'
GROUP BY 
    cl.[URLSegment]
HAVING 
    COUNT(*) > 1;
