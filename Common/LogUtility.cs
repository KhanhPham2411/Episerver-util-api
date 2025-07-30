using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Foundation.Custom.Common
{
    public static class LogUtility
    {
        /// <summary>
        /// Logs detailed request/session context to Application Insights.
        /// Usage: LogUtility.LogAnonymousSessionContext(httpContext, telemetryClient, "AnonymousIdIssued", cartId);
        /// 
        /// </summary>
        public static void LogAnonymousSessionContext(HttpContext httpContext, TelemetryClient telemetry, string eventName = "AnonymousIdIssued", string cartId = null)
        {
            if (httpContext == null || telemetry == null) return;

            var anonymousId = httpContext.Request.Cookies["EPiServer_Commerce_AnonymousId"];
            var arrAffinity = httpContext.Request.Cookies["ARRAffinity"];
            var arrAffinitySameSite = httpContext.Request.Cookies["ARRAffinitySameSite"];
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            var requestPath = httpContext.Request.Path.ToString();
            var requestMethod = httpContext.Request.Method;
            var queryString = httpContext.Request.QueryString.ToString();
            var instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
            var machineName = Environment.MachineName;
            var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
            var timestamp = DateTime.UtcNow.ToString("o");

            // Prefer X-Forwarded-For for client IP
            string clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
            {
                clientIp = xff.ToString().Split(',')[0].Trim();
            }

            var properties = new Dictionary<string, string>
            {
                { "AnonymousId", anonymousId ?? "null" },
                { "ARRAffinity", arrAffinity ?? "null" },
                { "ARRAffinitySameSite", arrAffinitySameSite ?? "null" },
                { "ClientIP", clientIp ?? "null" },
                { "UserAgent", userAgent ?? "null" },
                { "RequestPath", requestPath },
                { "RequestMethod", requestMethod },
                { "QueryString", queryString },
                { "InstanceId", instanceId ?? "null" },
                { "MachineName", machineName ?? "null" },
                { "TraceId", traceId ?? "null" },
                { "Timestamp", timestamp }
            };
            if (!string.IsNullOrEmpty(cartId))
            {
                properties.Add("CartId", cartId);
            }

            telemetry.TrackEvent(eventName, properties);
        }
    }
} 