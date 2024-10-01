SELECT  TOP(10) item.Name, log.[Text],
	(log.Duration) * POWER(10.00000000000,-7) / 60 as Minutes,
	log.[Exec], log.Duration, 
    (log.Duration) * POWER(10.00000000000,-7) / 360 as Seconds,
        item.DatePart, item.Interval, item.LastExec, item.NextExec
FROM [tblScheduledItemLog] as log INNER JOIN [tblScheduledItem] as item
ON log.fkScheduledItemId = item.pkID
--where CONVERT(VARCHAR(25), log.[Exec], 126) LIKE '2020-06-29%' AND log.Text like  '%publish%'
where item.Name LIKE '%Trim%'
order by log.[Exec] desc
