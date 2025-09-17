SELECT 
    ObjectId,
    MetaFieldId,
    MetaFieldName,
    LanguageName,
    CultureSpecific
FROM CatalogContentProperty 
WHERE ObjectId = 67348 
  AND ObjectTypeId = 0
  AND MetaFieldId IN (
    SELECT MetaFieldId
    FROM CatalogContentProperty 
    WHERE ObjectId = 67348 AND ObjectTypeId = 0
    GROUP BY MetaFieldId
    HAVING COUNT(DISTINCT CultureSpecific) > 1
  )
ORDER BY MetaFieldName, LanguageName, CultureSpecific;
