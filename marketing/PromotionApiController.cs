using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Commerce.Marketing;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("promotion-api")]
    public class PromotionApiController : ControllerBase
    {
        public PromotionApiController()
        {

        }

        [HttpGet]
        [Route("GetPromotionForEntryId")]
        public async Task<ActionResult<string>> GetPromotionForEntryId([FromQuery] string id = null)
        {
            var promotionEngine = ServiceLocator.Current.GetInstance<IPromotionEngine>();
            var currentMarket = ServiceLocator.Current.GetInstance<ICurrentMarket>();

            var discountPrices = promotionEngine.GetDiscountPrices(new ContentReference(int.Parse(id), "CatalogContent"), currentMarket.GetCurrentMarket());

            var promotions = discountPrices.SelectMany(c => c.DiscountPrices.Select(t => t.Promotion));

            return Content(string.Join("|", promotions.Select(t => t.Name)));
        }
    }
}