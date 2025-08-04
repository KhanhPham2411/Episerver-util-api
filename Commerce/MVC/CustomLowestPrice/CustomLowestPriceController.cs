using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Mediachase.Commerce.Pricing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Foundation.Features.CatalogContent.Services;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using System.Data;
using Mediachase.Data.Provider;
using Mediachase.DataProvider;

namespace Foundation.Features.CustomLowestPrice
{
    /// <summary>
    /// API Controller for testing lowest price functionality
    /// Demonstrates the issue with current implementation and provides the fix
    /// </summary>
    [Route("util-api/custom-lowest-price")]
    [ApiController]
    public class CustomLowestPriceController : ControllerBase
    {
        private readonly ILowestPriceService _lowestPriceService;
        private readonly IPricingService _pricingService;
        private readonly IRequestHostResolver _requestHostResolver;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly IConnectionStringHandler _connectionStringHandler;

        public CustomLowestPriceController(
            ILowestPriceService lowestPriceService,
            IPricingService pricingService,
            IRequestHostResolver requestHostResolver,
            ISiteDefinitionResolver siteDefinitionResolver,
            IConnectionStringHandler connectionStringHandler)
        {
            _lowestPriceService = lowestPriceService;
            _pricingService = pricingService;
            _requestHostResolver = requestHostResolver;
            _siteDefinitionResolver = siteDefinitionResolver;
            _connectionStringHandler = connectionStringHandler;
        }

        /// <summary>
        /// Get current price for a product using the existing service
        /// Sample URL: https://localhost:5000/util-api/custom-lowest-price/get-current-price?entryCode=SKU123
        /// </summary>
        [HttpGet("get-current-price")]
        public IActionResult GetCurrentPrice(string entryCode)
        {
            try
            {
                if (string.IsNullOrEmpty(entryCode))
                {
                    return BadRequest("Entry code is required");
                }

                var currentPrice = _pricingService.GetCurrentPrice(entryCode);
                
                var result = new
                {
                    EntryCode = entryCode,
                    HasPrice = currentPrice.HasValue,
                    Price = currentPrice?.Amount,
                    Currency = currentPrice?.Currency.CurrencyCode,
                    Message = !currentPrice.HasValue ? "No current price found" : "Current price found"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get lowest price using the ILowestPriceService directly
        /// Sample URL: https://localhost:5000/util-api/custom-lowest-price/get-lowest-price-service?entryCode=SKU123
        /// </summary>
        [HttpGet("get-lowest-price-service")]
        public IActionResult GetLowestPriceService(string entryCode)
        {
            try
            {
                if (string.IsNullOrEmpty(entryCode))
                {
                    return BadRequest("Entry code is required");
                }

                var siteId = GetSiteId();
                var lowestPrices = _lowestPriceService.List(new[] { entryCode }, siteId);
                
                var result = new
                {
                    EntryCode = entryCode,
                    SiteId = siteId,
                    Results = lowestPrices.Select(lp => new
                    {
                        CatalogEntryCode = lp.CatalogEntryCode,
                        MarketId = lp.MarketId.Value,
                        CurrencyCode = lp.Currency.CurrencyCode,
                        SiteId = lp.SiteId,
                        LowestPrice = lp.LowestPrice.Amount,
                        AppliedDate = lp.AppliedDate
                    }).ToList(),
                    Count = lowestPrices.Count(),
                    Message = !lowestPrices.Any() ? "No lowest price found with current service (excludes today's data)" : "Lowest price found"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get lowest price using direct database query with current implementation (excludes today's data)
        /// Sample URL: https://localhost:5000/util-api/custom-lowest-price/get-lowest-price-current-impl?entryCode=SKU123
        /// </summary>
        [HttpGet("get-lowest-price-current-impl")]
        public IActionResult GetLowestPriceCurrentImpl(string entryCode)
        {
            try
            {
                if (string.IsNullOrEmpty(entryCode))
                {
                    return BadRequest("Entry code is required");
                }

                var siteId = GetSiteId();
                var daysAgo = 30; // Default from LowestPriceOptions

                var command = new DataCommand
                {
                    ConnectionString = _connectionStringHandler.Commerce.ConnectionString,
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "ecf_LowestPrice_List"
                };

                var catalogEntryTable = CreateCatalogEntryTable(new[] { entryCode });
                command.Parameters.Add(new DataParameter("CatalogEntryCodes", catalogEntryTable));
                command.Parameters.Add(new DataParameter("SiteId", siteId));
                command.Parameters.Add(new DataParameter("DaysAgo", daysAgo));

                var result = new List<object>();
                var data = DataService.LoadReader(command);
                
                using (var reader = data.DataReader)
                {
                    while (data.DataReader.Read())
                    {
                        result.Add(new
                        {
                            CatalogEntryCode = reader["CatalogEntryCode"],
                            MarketId = reader["MarketId"],
                            CurrencyCode = reader["CurrencyCode"],
                            SiteId = reader["SiteId"],
                            LowestPrice = reader["LowestPrice"],
                            AppliedDate = reader["AppliedDate"]
                        });
                    }
                }

                return Ok(new
                {
                    EntryCode = entryCode,
                    SiteId = siteId,
                    DaysAgo = daysAgo,
                    Results = result,
                    Count = result.Count,
                    Message = result.Count == 0 ? "No results found with current implementation (excludes today's data)" : "Results found"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get lowest price using modified query that includes today's data (the fix)
        /// Sample URL: https://localhost:5000/util-api/custom-lowest-price/get-lowest-price-fixed-impl?entryCode=SKU123
        /// </summary>
        [HttpGet("get-lowest-price-fixed-impl")]
        public IActionResult GetLowestPriceFixedImpl(string entryCode)
        {
            try
            {
                if (string.IsNullOrEmpty(entryCode))
                {
                    return BadRequest("Entry code is required");
                }

                var siteId = GetSiteId();
                var daysAgo = 30;

                // This simulates the fixed stored procedure
                var sql = @"
                    DECLARE @Now DATETIME = CAST(GETUTCDATE() AS DATE)
                    DECLARE @From DATETIME = @Now - @DaysAgo
                    DECLARE @To DATETIME = DATEADD(DAY, 1, @Now)  -- Include today's data

                    SELECT P.CatalogEntryCode, P.MarketId, P.CurrencyCode, P.SiteId, 
                           MIN(P.LowestPrice) AS LowestPrice, MAX(P.AppliedDate) AS AppliedDate
                    FROM LowestPrice P
                    WHERE P.CatalogEntryCode = @EntryCode 
                          AND P.SiteId = @SiteId 
                          AND P.AppliedDate BETWEEN @From AND @To
                    GROUP BY P.CatalogEntryCode, P.MarketId, P.CurrencyCode, P.SiteId";

                var command = new DataCommand
                {
                    ConnectionString = _connectionStringHandler.Commerce.ConnectionString,
                    CommandType = CommandType.Text,
                    CommandText = sql
                };

                command.Parameters.Add(new DataParameter("EntryCode", entryCode));
                command.Parameters.Add(new DataParameter("SiteId", siteId));
                command.Parameters.Add(new DataParameter("DaysAgo", daysAgo));

                var result = new List<object>();
                var data = DataService.LoadReader(command);
                
                using (var reader = data.DataReader)
                {
                    while (data.DataReader.Read())
                    {
                        result.Add(new
                        {
                            CatalogEntryCode = reader["CatalogEntryCode"],
                            MarketId = reader["MarketId"],
                            CurrencyCode = reader["CurrencyCode"],
                            SiteId = reader["SiteId"],
                            LowestPrice = reader["LowestPrice"],
                            AppliedDate = reader["AppliedDate"]
                        });
                    }
                }

                return Ok(new
                {
                    EntryCode = entryCode,
                    SiteId = siteId,
                    DaysAgo = daysAgo,
                    Results = result,
                    Count = result.Count,
                    Message = result.Count == 0 ? "No results found with fixed implementation" : "Results found with fixed implementation (includes today's data)"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Compare both implementations side by side
        /// Sample URL: https://localhost:5000/util-api/custom-lowest-price/compare-implementations?entryCode=SKU123
        /// </summary>
        [HttpGet("compare-implementations")]
        public IActionResult CompareImplementations(string entryCode)
        {
            try
            {
                if (string.IsNullOrEmpty(entryCode))
                {
                    return BadRequest("Entry code is required");
                }

                var serviceImpl = GetLowestPriceService(entryCode) as OkObjectResult;
                var currentImpl = GetLowestPriceCurrentImpl(entryCode) as OkObjectResult;
                var fixedImpl = GetLowestPriceFixedImpl(entryCode) as OkObjectResult;

                var comparison = new
                {
                    EntryCode = entryCode,
                    //ServiceImplementation = serviceImpl?.Value,
                    CurrentImplementation = currentImpl?.Value,
                    FixedImplementation = fixedImpl?.Value,
                    //Difference = new
                    //{
                    //    ServiceCount = ((dynamic)serviceImpl?.Value)?.Count ?? 0,
                    //    CurrentCount = ((dynamic)currentImpl?.Value)?.Count ?? 0,
                    //    FixedCount = ((dynamic)fixedImpl?.Value)?.Count ?? 0,
                    //    HasDifference = ((dynamic)currentImpl?.Value)?.Count != ((dynamic)fixedImpl?.Value)?.Count
                    //}
                };

                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Create a test lowest price entry for today (for testing purposes)
        /// Sample URL: https://localhost:5000/util-api/custom-lowest-price/create-test-entry?entryCode=SKU123&price=99.99&marketId=US&currencyCode=USD
        /// </summary>
        [HttpGet("create-test-entry")]
        public IActionResult CreateTestEntry(string entryCode, decimal price, string marketId = "US", string currencyCode = "USD")
        {
            try
            {
                if (string.IsNullOrEmpty(entryCode))
                {
                    return BadRequest("Entry code is required");
                }

                var siteId = GetSiteId();
                var appliedDate = DateTime.UtcNow;

                var sql = @"
                    INSERT INTO LowestPrice (CatalogEntryCode, MarketId, CurrencyCode, AppliedDate, SiteId, LowestPrice)
                    VALUES (@EntryCode, @MarketId, @CurrencyCode, @AppliedDate, @SiteId, @LowestPrice)";

                var command = new DataCommand
                {
                    ConnectionString = _connectionStringHandler.Commerce.ConnectionString,
                    CommandType = CommandType.Text,
                    CommandText = sql
                };

                command.Parameters.Add(new DataParameter("EntryCode", entryCode));
                command.Parameters.Add(new DataParameter("MarketId", marketId));
                command.Parameters.Add(new DataParameter("CurrencyCode", currencyCode));
                command.Parameters.Add(new DataParameter("AppliedDate", appliedDate));
                command.Parameters.Add(new DataParameter("SiteId", siteId));
                command.Parameters.Add(new DataParameter("LowestPrice", price));

                DataService.ExecuteNonExec(command);

                return Ok(new
                {
                    Message = "Test entry created successfully",
                    EntryCode = entryCode,
                    Price = price,
                    MarketId = marketId,
                    CurrencyCode = currencyCode,
                    AppliedDate = appliedDate,
                    SiteId = siteId
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get all lowest price entries for a product
        /// Sample URL: https://localhost:5000/util-api/custom-lowest-price/get-all-entries?entryCode=SKU123
        /// </summary>
        [HttpGet("get-all-entries")]
        public IActionResult GetAllEntries(string entryCode)
        {
            try
            {
                if (string.IsNullOrEmpty(entryCode))
                {
                    return BadRequest("Entry code is required");
                }

                var siteId = GetSiteId();

                var sql = @"
                    SELECT CatalogEntryCode, MarketId, CurrencyCode, SiteId, LowestPrice, AppliedDate
                    FROM LowestPrice 
                    WHERE CatalogEntryCode = @EntryCode AND SiteId = @SiteId
                    ORDER BY AppliedDate DESC";

                var command = new DataCommand
                {
                    ConnectionString = _connectionStringHandler.Commerce.ConnectionString,
                    CommandType = CommandType.Text,
                    CommandText = sql
                };

                command.Parameters.Add(new DataParameter("EntryCode", entryCode));
                command.Parameters.Add(new DataParameter("SiteId", siteId));

                var result = new List<object>();
                var data = DataService.LoadReader(command);
                
                using (var reader = data.DataReader)
                {
                    while (data.DataReader.Read())
                    {
                        result.Add(new
                        {
                            CatalogEntryCode = reader["CatalogEntryCode"],
                            MarketId = reader["MarketId"],
                            CurrencyCode = reader["CurrencyCode"],
                            SiteId = reader["SiteId"],
                            LowestPrice = reader["LowestPrice"],
                            AppliedDate = reader["AppliedDate"],
                            IsToday = ((DateTime)reader["AppliedDate"]).Date == DateTime.UtcNow.Date
                        });
                    }
                }

                return Ok(new
                {
                    EntryCode = entryCode,
                    SiteId = siteId,
                    TotalEntries = result.Count,
                    TodayEntries = result.Count(r => (bool)((dynamic)r).IsToday),
                    Entries = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private string GetSiteId()
        {
            var site = _siteDefinitionResolver.GetByHostname(_requestHostResolver.HostName, true, out _);
            return site?.Id.ToString() ?? string.Empty;
        }

        private static DataTable CreateCatalogEntryTable(IEnumerable<string> catalogEntryCodes)
        {
            var result = new DataTable();
            result.Columns.Add("CatalogEntryCode", typeof(string));

            if (catalogEntryCodes == null || !catalogEntryCodes.Any())
            {
                return result;
            }

            foreach (var entryCode in catalogEntryCodes)
            {
                result.Rows.Add(entryCode);
            }

            return result;
        }
    }
} 