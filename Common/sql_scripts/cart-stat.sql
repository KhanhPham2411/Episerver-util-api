-- Comprehensive SerializableCart Diagnostic Query
-- Shows cart age distribution, reset patterns, and cleanup eligibility

SELECT 
    -- Overall Statistics
    COUNT(*) AS TotalCarts,
    COUNT(DISTINCT CustomerId) AS UniqueCustomers,
    
    -- Age Distribution by Created Date
    SUM(CASE WHEN Created < DATEADD(DAY, -90, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_90PlusDaysOld_Created,
    SUM(CASE WHEN Created >= DATEADD(DAY, -90, GETUTCDATE()) AND Created < DATEADD(DAY, -30, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_30To90DaysOld_Created,
    SUM(CASE WHEN Created >= DATEADD(DAY, -30, GETUTCDATE()) AND Created < DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_7To30DaysOld_Created,
    SUM(CASE WHEN Created >= DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_LessThan7DaysOld_Created,
    
    -- Last Modified Distribution
    SUM(CASE WHEN Modified < DATEADD(DAY, -90, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_90PlusDaysOld_Modified,
    SUM(CASE WHEN Modified >= DATEADD(DAY, -90, GETUTCDATE()) AND Modified < DATEADD(DAY, -30, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_30To90DaysOld_Modified,
    SUM(CASE WHEN Modified >= DATEADD(DAY, -30, GETUTCDATE()) AND Modified < DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_7To30DaysOld_Modified,
    SUM(CASE WHEN Modified >= DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS Carts_LessThan7DaysOld_Modified,
    
    -- Problem Indicators: Old Created but Recent Modified (likely reset carts)
    SUM(CASE WHEN Created < DATEADD(DAY, -30, GETUTCDATE()) AND Modified >= DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS ProblemCarts_OldCreated_RecentModified,
    SUM(CASE WHEN Created < DATEADD(DAY, -90, GETUTCDATE()) AND Modified >= DATEADD(DAY, -30, GETUTCDATE()) THEN 1 ELSE 0 END) AS ProblemCarts_VeryOldCreated_RecentModified,
    
    -- Cleanup Job Eligibility (based on Modified date, default 30 days)
    SUM(CASE WHEN Modified <= DATEADD(DAY, -30, GETUTCDATE()) THEN 1 ELSE 0 END) AS EligibleForCleanup_30Days,
    SUM(CASE WHEN Modified <= DATEADD(DAY, -7, GETUTCDATE()) THEN 1 ELSE 0 END) AS EligibleForCleanup_7Days,
    
    -- Average Ages
    AVG(DATEDIFF(DAY, Created, GETUTCDATE())) AS AvgAge_Days_Created,
    AVG(DATEDIFF(DAY, Modified, GETUTCDATE())) AS AvgAge_Days_Modified,
    AVG(DATEDIFF(DAY, Created, Modified)) AS AvgDaysBetween_CreatedAndModified,
    
    -- Max Ages
    MAX(DATEDIFF(DAY, Created, GETUTCDATE())) AS MaxAge_Days_Created,
    MAX(DATEDIFF(DAY, Modified, GETUTCDATE())) AS MaxAge_Days_Modified
    
FROM SerializableCart
WHERE Name = 'Default'  -- Filter to shopping carts only (exclude WishList, etc.)
