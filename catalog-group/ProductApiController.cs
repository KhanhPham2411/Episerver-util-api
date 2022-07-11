using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using Foundation.Features.CatalogContent.Product;
using Mediachase.Commerce.Catalog;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("product-api")]
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
        
        [HttpGet]
        [Route("CreateProduct")]
        public async Task<ActionResult<string>> CreateProduct([FromQuery] string code = null)
        {
            var _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            string log = "";

            var rootLink = _referenceConverter.GetRootLink();
            var parent = _contentRepository.GetChildren<CatalogContent>(rootLink).First();

            GenericProduct product = _contentRepository.GetDefault<GenericProduct>(parent.ContentLink);
            var guid = Guid.NewGuid().ToString();
            product.Code = "sample_product"+guid;
            product.Name = "Sample product" + guid;
            product.DisplayName = "Sample product" + guid;
            product.IsPendingPublish = false;
            product.StopPublish = DateTime.Today.AddYears(10);  

            _contentRepository.Save(product);
            log += product.Code + " created successfully";

            return Ok(log);
        }

        [HttpGet]
        [Route("GetProductWithMedia")]
        public async Task<ActionResult<string>> GetProductWithMedia([FromQuery] string code = null)
        {
            var _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            string log = "";

            var productContentLink = _referenceConverter.GetContentLink(code ?? "p-39813617");
            var product = _contentRepository.Get<GenericProduct>(productContentLink).CreateWritableClone<GenericProduct>();

            var images = product.CommerceMediaCollection?.Select(x => x.AssetLink.GetUri()).ToList();

            
            log += product.Code + "\n";
            log += product.ContentGuid.ToString() + "\n";
            log += string.Join(",", images.Select(w => w.ToString()));

            return Ok(log);
        }
    }
}