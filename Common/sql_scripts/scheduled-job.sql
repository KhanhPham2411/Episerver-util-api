SELECT  TOP(100) item.Name, log.[Text],
	log.[Exec], 
	-- log.Duration, 
	-- (log.Duration) * POWER(10.00000000000,-7) / 60 as Minutes,
    (log.Duration) * POWER(10.00000000000,-7) / 360 as Seconds,
    item.DatePart, item.Interval, 
	log.[Server],
	item.LastExec, item.NextExec,
	CASE log.[Status]
        WHEN 0 THEN 'Unknown'
        WHEN 1 THEN 'Succeeded'      -- ✅ SUCCESS
        WHEN 2 THEN 'Failed'         -- ❌ FAILED
        WHEN 3 THEN 'Cancelled'
        WHEN 4 THEN 'UnableToStart'
        WHEN 5 THEN 'Aborted'
        ELSE 'Unknown Status'
    END as StatusText,
FROM [tblScheduledItemLog] as log INNER JOIN [tblScheduledItem] as item
ON log.fkScheduledItemId = item.pkID
--where CONVERT(VARCHAR(25), log.[Exec], 126) LIKE '2020-06-29%' AND log.Text like  '%publish%'
where item.Name LIKE '%Order Export%' AND CONVERT(VARCHAR(25), log.[Exec], 126) LIKE '2025-01-06%'
order by log.[Exec] desc
