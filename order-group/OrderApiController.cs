using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Commerce.Marketing;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
        [Route("search")]
        public async Task<ActionResult<string>> Search([FromQuery] string id = null)
        {
            var orders = OrderContext.Current.FindPurchaseOrdersByStatus(OrderStatus.InProgress);

            return Content(string.Join("|", orders.Select(t => t.Name)));
        }
    }
}