using EPiServer.Find;
using EPiServer.ServiceApi.Commerce.Models.Catalog;
using Foundation.Features.CustomCatalog;
using Foundation.Features.Search.Category;
using Mediachase.MetaDataPlus.Configurator;
using System.Globalization;
using System.Text.Json;


namespace Foundation.Custom
{
    [ApiController]
    [Route("catalog-content")]
    public class CatalogContentApiController : ControllerBase
    {
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentRepository _contentRepository;
        private readonly IContentLoader _contentLoader;

        public CatalogContentApiController(ReferenceConverter referenceConverter, IContentRepository contentRepository, IContentLoader contentLoader)
        {
            _referenceConverter = referenceConverter;
            _contentRepository = contentRepository;
            _contentLoader = contentLoader;
        }

        [HttpGet]
        [Route("children")]
        public async Task<ActionResult<string>> GetChildren([FromQuery] string code = "mens")
        {
            string log = "";

            var child = _contentRepository.GetChildren<CatalogContent>(_referenceConverter.GetRootLink());
            var childCustom = _contentRepository.GetChildren<CustomCatalog>(_referenceConverter.GetRootLink());

            return Ok(log);
        }


        [HttpGet]
        [Route("get")]
        public async Task<ActionResult<string>> get()
        {
            string log = "";

            var secondChild = _contentRepository.GetChildren<CatalogContent>(_referenceConverter.GetRootLink())
                        .Skip(1)
                        .FirstOrDefault();

            var customCatalog = _contentRepository.Get<CustomCatalog>(secondChild.ContentLink);

            var value = customCatalog.DemoText;

            return Ok(value);
        }
    }
}
