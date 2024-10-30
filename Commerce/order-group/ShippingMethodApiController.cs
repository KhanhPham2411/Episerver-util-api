


using EPiServer.Find;
using EPiServer.Find.Cms;
using Foundation.Features.CatalogContent.Product;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce;
using MetaClass = Mediachase.BusinessFoundation.Data.Meta.Management.MetaClass;
using MetaField = Mediachase.BusinessFoundation.Data.Meta.Management.MetaField;
using Mediachase.Commerce.Orders.Dto;
using EPiServer.Commerce.UI.Admin.Shipping.Internal;

namespace Foundation.Custom
{
    [ApiController]
    [Route("shipping-method")]
    public class ShippingMethodApiController : ControllerBase
    {


        public ShippingMethodApiController()
        {
        }

        [HttpGet]
        [Route("weight")]
        public async Task<ActionResult<string>> weight([FromQuery] string keyword = "a")
        {
            string log = "";

            var methods = ShippingManager.GetShippingMethods("en");

            var filteredMethod = methods.ShippingMethod.Where(s => {
                var methodCases =
                    ShippingManager.GetShippingMethodCases(s.ShippingMethodId)
                    .Rows.Cast<ShippingMethodDto.ShippingMethodCaseRow>()
                    .Select(x => new ShippingMethodCase
                    {
                        ShippingMethodCaseId = x.ShippingMethodCaseId,
                        ShippingMethodId = x.ShippingMethodId.ToString(),
                        JurisdictionGroupId = x.JurisdictionGroupId,
                        Weight = x.Total,
                        Price = x.Charge,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate
                    });

                return methodCases.Any(caseModel => caseModel.Weight > 1); ;
            });

            log += string.Join(",", filteredMethod.Select(s => s.Name));
            return Ok(log);
        }

    }
}
