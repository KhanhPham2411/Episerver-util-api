WITH NodeHierarchy AS (
    SELECT CatalogNodeId as RootNodeId, CatalogNodeId as NodeId, ParentNodeId, 0 as Level
    FROM CatalogNode
    UNION ALL
    SELECT nh.RootNodeId, cn.CatalogNodeId, cn.ParentNodeId, nh.Level + 1
    FROM NodeHierarchy nh
    INNER JOIN CatalogNode cn ON cn.ParentNodeId = nh.NodeId
    WHERE nh.Level < 20
),
NodeEntryCounts AS (
    SELECT nh.RootNodeId, COUNT(DISTINCT ner.CatalogEntryId) as TotalEntryCount
    FROM NodeHierarchy nh
    LEFT JOIN NodeEntryRelation ner ON ner.CatalogNodeId = nh.NodeId
    GROUP BY nh.RootNodeId
),
NodePath AS (
    SELECT 
        cn.CatalogNodeId,
        cn.Name,
        cn.ParentNodeId,
        cn.CatalogId,
        CAST(ISNULL(cat.Name, 'Catalog ' + CAST(cn.CatalogId AS VARCHAR)) + ' > ' + cn.Name AS NVARCHAR(MAX)) as Path,
        0 as Level
    FROM CatalogNode cn
    LEFT JOIN Catalog cat ON cat.CatalogId = cn.CatalogId
    WHERE cn.ParentNodeId = 0
    
    UNION ALL
    
    SELECT 
        cn.CatalogNodeId,
        cn.Name,
        cn.ParentNodeId,
        cn.CatalogId,
        CAST(np.Path + ' > ' + cn.Name AS NVARCHAR(MAX)),
        np.Level + 1
    FROM NodePath np
    INNER JOIN CatalogNode cn ON cn.ParentNodeId = np.CatalogNodeId
    WHERE np.Level < 20
)
SELECT 
    cn.CatalogNodeId, 
    cn.Name, 
    cn.ParentNodeId, 
    cn.CatalogId,
    nec.TotalEntryCount,
    ISNULL(np.Path, cn.Name) as FullPath
FROM NodeEntryCounts nec
INNER JOIN CatalogNode cn ON cn.CatalogNodeId = nec.RootNodeId
LEFT JOIN NodePath np ON np.CatalogNodeId = cn.CatalogNodeId
WHERE nec.TotalEntryCount > 24000
ORDER BY nec.TotalEntryCount DESC
