using EPiServer;
using EPiServer.ServiceLocation;
using Foundation.Features.CatalogContent.Product;
using Mediachase.Commerce.Catalog;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("product")]
    public class ProductApiController : ControllerBase
    {
        public ProductApiController()
        {

        }

        [HttpGet]
        [Route("GetProduct")]
        public async Task<ActionResult<string>> GetProduct([FromQuery] string code = null)
        {
            var _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            string log = "";

            var productContentLink = _referenceConverter.GetContentLink(code ?? "p-39813617");
            var product = _contentRepository.Get<GenericProduct>(productContentLink).CreateWritableClone<GenericProduct>();

            log += product.Code + "\n";
            log += product.ContentGuid.ToString();
            
            return Ok(log);
        }

    }
}