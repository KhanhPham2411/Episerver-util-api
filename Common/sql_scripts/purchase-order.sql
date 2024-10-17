/****** Script for SelectTopNRows command from SSMS  ******/
SELECT TOP (1000) [OrderGroupId]
      ,[CustomerId]
      ,[CustomerName]
      ,[CustomerEmail]
      ,[AddressId]

      ,[MarketId]
  FROM [dbo].[OrderGroup] og
  JOIN [dbo].[OrderGroup_PurchaseOrder] po on po.ObjectId = og.OrderGroupId
  WHERE po.[Created] >= DATEADD(DAY, -47, GETDATE());
