SELECT TOP 10
    qt.query_sql_text,
    q.query_id,
    q.object_id,
    SUM(rs.count_executions) AS total_executions,
    AVG(rs.avg_cpu_time) / 1000 AS avg_cpu_ms,
    MAX(rs.max_cpu_time) / 1000 AS max_cpu_ms,
    MAX(rs.max_duration) / 1000 AS max_duration_ms, -- Added Max Duration
    p.query_plan
FROM sys.query_store_runtime_stats AS rs
JOIN sys.query_store_runtime_stats_interval AS rsi 
    ON rs.runtime_stats_interval_id = rsi.runtime_stats_interval_id
JOIN sys.query_store_plan AS p ON rs.plan_id = p.plan_id
JOIN sys.query_store_query AS q ON p.query_id = q.query_id
JOIN sys.query_store_query_text AS qt ON q.query_text_id = qt.query_text_id
WHERE rsi.start_time >= DATEADD(day, -1, GETUTCDATE()) 
  AND q.object_id = OBJECT_ID('ecf_Search_PurchaseOrder')
GROUP BY qt.query_sql_text, q.query_id, p.query_plan, q.object_id
ORDER BY avg_cpu_ms DESC;
