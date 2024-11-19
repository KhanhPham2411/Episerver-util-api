SELECT TOP (1000) mf.* 
FROM [dbo].[MetaClass] mc
JOIN  [dbo].[MetaClassMetaFieldRelation] relation on mc.[MetaClassId] = relation.[MetaClassId]
JOIN  [dbo].[MetaField] mf on mf.[MetaFieldId] = relation.[MetaFieldId]
where mc.[Name] like '%Category%'
