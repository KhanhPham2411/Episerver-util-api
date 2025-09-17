SELECT 
    p.ObjectId,
    p.ObjectTypeId,
    p.MetaFieldId,
    p.MetaFieldName,
    p.LanguageName,
    p.CultureSpecific
FROM CatalogContentProperty p
JOIN (
    SELECT
        ObjectId,
        ObjectTypeId,
        MetaFieldId
    FROM CatalogContentProperty
    -- Optional: restrict to entries only
    -- WHERE ObjectTypeId = 0
    GROUP BY ObjectId, ObjectTypeId, MetaFieldId
    HAVING COUNT(DISTINCT CultureSpecific) > 1
) bad
  ON bad.ObjectId = p.ObjectId
 AND bad.ObjectTypeId = p.ObjectTypeId
 AND bad.MetaFieldId = p.MetaFieldId
ORDER BY p.ObjectId, p.MetaFieldName, p.LanguageName, p.CultureSpecific;
