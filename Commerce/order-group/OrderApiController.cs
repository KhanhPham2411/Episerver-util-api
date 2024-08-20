using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Foundation.Social.Models;
using Geta.NotFoundHandler.Admin.Pages.Geta.NotFoundHandler.Admin.Models;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("order-api")]
    public class OrderApiController : ControllerBase
    {
        public OrderApiController()
        {

        }

        [HttpGet]
        [Route("FindPurchaseOrdersByStatus")]
        public async Task<ActionResult<string>> FindPurchaseOrdersByStatus([FromQuery] string id = null)
        {
            var orders = OrderContext.Current.FindPurchaseOrdersByStatus(OrderStatus.InProgress);

            return Content(string.Join("|", orders.Select(t => t.Name)));
        }

        [HttpGet]
        [Route("FindPurchaseOrders")]
        public async Task<ActionResult<string>> FindPurchaseOrders([FromQuery] string id = null)
        {
            var _orderSearchService = ServiceLocator.Current.GetInstance<IOrderSearchService>();

            OrderSearchFilter filter = new()
            {
                ReturnTotalCount = true,
                CustomerId = new Guid("CD0DEE6E-6F76-4B29-A5E3-AE095DE807B6"),
                //StartingIndex = paging.StartingIndex(),
                //RecordsToRetrieve = paging.Take(),
                //SiteId = site.Id.ToString(),
            };

            var results = _orderSearchService.FindPurchaseOrders(filter);
            var orders = results.Orders.ToList();
            if (orders.Count == 0) {
                return Content("No order found");
            }

            return Content(string.Join("|", orders.Select(t => t.OrderNumber)));
        }
    }
}
