using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using Mediachase.MetaDataPlus;
using Mediachase.Commerce.Orders.Managers;
using EPiServer.Commerce.Order;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.OrderGroup
{
    /// <summary>
    /// API for testing Purchase Order search functionality and replicating the META.Created issue.
    /// Sample usage: https://localhost:5000/util-api/custom-purchase-order/search-orders
    /// </summary>
    [ApiController]
    [Route("util-api/custom-purchase-order")]
    public class CustomPurchaseOrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;

        public CustomPurchaseOrderController()
        {
            _orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
        }

        /// <summary>
        /// Search purchase orders with different OrderBy clauses to replicate the META.Created issue.
        /// Sample usage: https://localhost:5000/util-api/custom-purchase-order/search-orders?orderBy=META.Created%20DESC
        /// </summary>
        [HttpGet("search-orders")]
        public IActionResult SearchOrders(
            [FromQuery] string orderBy = "OrderGroupId DESC",
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            [FromQuery] string customerId = null,
            [FromQuery] string whereClause = null,
            [FromQuery] string metaWhereClause = null)
        {
            try
            {
                object result = new
                {
                    SearchParameters = new
                    {
                        OrderBy = orderBy,
                        Skip = skip,
                        Take = take,
                        CustomerId = customerId,
                        WhereClause = whereClause,
                        MetaWhereClause = metaWhereClause
                    },
                    Results = new List<object>(),
                    Error = (string)null
                };

                // Create search options
                var searchOptions = new OrderSearchOptions
                {
                    CacheResults = false,
                    StartingRecord = skip,
                    RecordsToRetrieve = take,
                    Namespace = "Mediachase.Commerce.Orders"
                };
                searchOptions.Classes.Add("PurchaseOrder");

                // Create search parameters
                var parameters = new OrderSearchParameters
                {
                    OrderByClause = orderBy,
                    SqlMetaWhereClause = !string.IsNullOrWhiteSpace(customerId) 
                        ? $"META.CustomerId = '{customerId}'" 
                        : string.Empty
                };

                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    parameters.SqlWhereClause = whereClause;
                }

                if (!string.IsNullOrWhiteSpace(metaWhereClause))
                {
                    if (!string.IsNullOrEmpty(parameters.SqlMetaWhereClause))
                    {
                        parameters.SqlMetaWhereClause += " AND " + metaWhereClause;
                    }
                    else
                    {
                        parameters.SqlMetaWhereClause = metaWhereClause;
                    }
                }

                // Perform the search - this is where the META.Created issue occurs
                var purchaseOrders = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions);

                // Convert results to a readable format
                var orderResults = purchaseOrders.Select(po => new
                {
                    OrderGroupId = po.OrderGroupId,
                    CustomerId = po.CustomerId,
                    CustomerName = po.CustomerName,
                    Status = po.Status,
                    Total = po.Total,
                    Created = po.Created,
                    Modified = po.Modified,
                    MarketId = po.MarketId
                }).ToList();

                result = new
                {
                    SearchParameters = new
                    {
                        OrderBy = orderBy,
                        Skip = skip,
                        Take = take,
                        CustomerId = customerId,
                        WhereClause = whereClause,
                        MetaWhereClause = metaWhereClause
                    },
                    Results = orderResults,
                    Error = (string)null
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Test different OrderBy clauses to demonstrate the META.Created issue.
        /// Sample usage: https://localhost:5000/util-api/custom-purchase-order/test-orderby-clauses
        /// </summary>
        [HttpGet("test-orderby-clauses")]
        public IActionResult TestOrderByClauses([FromQuery] int skip = 0, [FromQuery] int take = 5)
        {
            try
            {
                var testCases = new[]
                {
                    "OrderGroupId DESC",
                    "CustomerName DESC", 
                    "META.Created DESC",  // This will cause the error
                    "META.Modified DESC", // This might also cause an error
                    "Total DESC",
                    "Status DESC"
                };

                var results = new List<object>();

                foreach (var orderBy in testCases)
                {
                    try
                    {
                        var searchOptions = new OrderSearchOptions
                        {
                            CacheResults = false,
                            StartingRecord = skip,
                            RecordsToRetrieve = take,
                            Namespace = "Mediachase.Commerce.Orders"
                        };
                        searchOptions.Classes.Add("PurchaseOrder");

                        var parameters = new OrderSearchParameters
                        {
                            OrderByClause = orderBy
                        };

                        var purchaseOrders = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions);

                        results.Add(new
                        {
                            OrderBy = orderBy,
                            Success = true,
                            Count = purchaseOrders.Count(),
                            Error = (string)null,
                            SampleResults = purchaseOrders.Take(2).Select(po => new
                            {
                                OrderGroupId = po.OrderGroupId,
                                CustomerName = po.CustomerName,
                                Status = po.Status,
                                Total = po.Total
                            }).ToList()
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            OrderBy = orderBy,
                            Success = false,
                            Count = 0,
                            Error = ex.Message,
                            SampleResults = new List<object>()
                        });
                    }
                }

                return Ok(new
                {
                    TestCases = results,
                    Summary = new
                    {
                        TotalTests = testCases.Length,
                        Successful = results.Count(r => ((dynamic)r).Success),
                        Failed = results.Count(r => !((dynamic)r).Success)
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Create a test purchase order for testing purposes.
        /// Sample usage: https://localhost:5000/util-api/custom-purchase-order/create-test-order?customerName=TestCustomer
        /// </summary>
        [HttpGet("create-test-order")]
        public IActionResult CreateTestOrder(
            [FromQuery] string customerName = "TestCustomer",
            [FromQuery] string customerEmail = "test@example.com")
        {
            try
            {
                // Create a test customer first
                // Create a simple test contact
                var customer = CustomerContact.CreateInstance();
                customer.Email = customerEmail;
                customer.FirstName = customerName;
                customer.LastName = "Test";
                customer.SaveChanges();

                // Create a test purchase order
                var purchaseOrder = _orderRepository.Create<PurchaseOrder>(customer.PrimaryKeyId.Value, "Test Order");
                purchaseOrder.CustomerId = customer.PrimaryKeyId.Value;
                purchaseOrder.CustomerName = customerName;
                purchaseOrder.CustomerEmail = customerEmail;
                purchaseOrder.Status = "InProgress";
                purchaseOrder.MarketId = "DEFAULT";
                purchaseOrder.Total = 100.00m;
                purchaseOrder.SubTotal = 90.00m;
                purchaseOrder.TaxTotal = 10.00m;
                purchaseOrder.ShippingTotal = 0m;
                purchaseOrder.HandlingTotal = 0m;

                // Save the order
                _orderRepository.Save(purchaseOrder);

                return Ok(new
                {
                    Success = true,
                    OrderGroupId = purchaseOrder.OrderGroupId,
                    CustomerId = purchaseOrder.CustomerId,
                    CustomerName = purchaseOrder.CustomerName,
                    Status = purchaseOrder.Status,
                    Total = purchaseOrder.Total,
                    Created = purchaseOrder.Created,
                    Modified = purchaseOrder.Modified,
                    Message = "Test purchase order created successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get all purchase orders with basic information.
        /// Sample usage: https://localhost:5000/util-api/custom-purchase-order/list-orders
        /// </summary>
        [HttpGet("list-orders")]
        public IActionResult ListOrders([FromQuery] int skip = 0, [FromQuery] int take = 10)
        {
            try
            {
                var searchOptions = new OrderSearchOptions
                {
                    CacheResults = false,
                    StartingRecord = skip,
                    RecordsToRetrieve = take,
                    Namespace = "Mediachase.Commerce.Orders"
                };
                searchOptions.Classes.Add("PurchaseOrder");

                var parameters = new OrderSearchParameters
                {
                    OrderByClause = "OrderGroupId DESC" // Use safe ordering
                };

                var purchaseOrders = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions);

                var result = purchaseOrders.Select(po => new
                {
                    OrderGroupId = po.OrderGroupId,
                    CustomerId = po.CustomerId,
                    CustomerName = po.CustomerName,
                    CustomerEmail = po.CustomerEmail,
                    Status = po.Status,
                    Total = po.Total,
                    SubTotal = po.SubTotal,
                    TaxTotal = po.TaxTotal,
                    ShippingTotal = po.ShippingTotal,
                    Created = po.Created,
                    Modified = po.Modified,
                    MarketId = po.MarketId,
                    BillingCurrency = po.BillingCurrency
                }).ToList();

                return Ok(new
                {
                    Count = result.Count,
                    Orders = result,
                    Pagination = new
                    {
                        Skip = skip,
                        Take = take
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Demonstrate the exact issue from the C# code shown in the images.
        /// Sample usage: https://localhost:5000/util-api/custom-purchase-order/replicate-issue
        /// </summary>
        [HttpGet("replicate-issue")]
        public IActionResult ReplicateIssue([FromQuery] string customerId = null)
        {
            try
            {
                var result = new
                {
                    Steps = new List<object>(),
                    FinalError = (string)null
                };

                var steps = new List<object>();
                var skip = 0;
                var take = 20;

                // Step 1: Create search options (from the C# code)
                var searchOptions = new OrderSearchOptions
                {
                    CacheResults = false,
                    StartingRecord = skip,
                    RecordsToRetrieve = take,
                    Namespace = "Mediachase.Commerce.Orders"
                };
                searchOptions.Classes.Add("PurchaseOrder");

                steps.Add(new
                {
                    Step = 1,
                    Description = "Create OrderSearchOptions",
                    Success = true,
                    Details = "CacheResults = false, StartingRecord = " + skip + ", RecordsToRetrieve = " + take
                });

                // Step 2: Create search parameters
                var parameters = new OrderSearchParameters
                {
                    OrderByClause = "OrderGroupId DESC",
                    SqlMetaWhereClause = !string.IsNullOrWhiteSpace(customerId) 
                        ? $"META.CustomerId = '{customerId}'" 
                        : string.Empty
                };

                steps.Add(new
                {
                    Step = 2,
                    Description = "Create OrderSearchParameters with OrderGroupId DESC",
                    Success = true,
                    Details = "OrderByClause = 'OrderGroupId DESC'"
                });

                // Step 3: First search (this works)
                try
                {
                    var purchaseOrders1 = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions);
                    steps.Add(new
                    {
                        Step = 3,
                        Description = "First search with OrderGroupId DESC",
                        Success = true,
                        Count = purchaseOrders1.Count(),
                        Details = "This works fine"
                    });
                }
                catch (Exception ex)
                {
                    steps.Add(new
                    {
                        Step = 3,
                        Description = "First search with OrderGroupId DESC",
                        Success = false,
                        Error = ex.Message,
                        Details = "Unexpected error in first search"
                    });
                }

                // Step 4: Try CustomerName DESC (this might work)
                try
                {
                    parameters.OrderByClause = "CustomerName DESC";
                    var purchaseOrders2 = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions);
                    steps.Add(new
                    {
                        Step = 4,
                        Description = "Search with CustomerName DESC",
                        Success = true,
                        Count = purchaseOrders2.Count(),
                        Details = "This works because CustomerName exists in OrderGroup table"
                    });
                }
                catch (Exception ex)
                {
                    steps.Add(new
                    {
                        Step = 4,
                        Description = "Search with CustomerName DESC",
                        Success = false,
                        Error = ex.Message,
                        Details = "CustomerName search failed"
                    });
                }

                // Step 5: Try META.Created DESC (this will fail)
                try
                {
                    parameters.OrderByClause = "META.Created DESC";
                    var purchaseOrders3 = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions);
                    steps.Add(new
                    {
                        Step = 5,
                        Description = "Search with META.Created DESC",
                        Success = true,
                        Count = purchaseOrders3.Count(),
                        Details = "This should fail but didn't - unexpected"
                    });
                }
                catch (Exception ex)
                {
                    steps.Add(new
                    {
                        Step = 5,
                        Description = "Search with META.Created DESC",
                        Success = false,
                        Error = ex.Message,
                        Details = "This is the expected error: META.Created column not found in OrderGroup table"
                    });
                }

                return Ok(new
                {
                    Steps = steps,
                    Summary = new
                    {
                        TotalSteps = steps.Count,
                        Successful = steps.Count(s => ((dynamic)s).Success),
                        Failed = steps.Count(s => !((dynamic)s).Success)
                    },
                    Explanation = "The META.Created issue occurs because the OrderGroup table doesn't have a 'Created' column, and the META alias is not available in the final query context of the ecf_Search_PurchaseOrder stored procedure."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
