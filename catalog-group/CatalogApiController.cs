using Foundation.Features.CustomCatalog;

namespace Foundation.Custom
{
    [ApiController]
    [Route("catalog-api")]
    public class CatalogApiController : ControllerBase
    {
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentRepository _contentRepository;


        public CatalogApiController(
            ReferenceConverter referenceConverter,
            IContentRepository contentRepository,
            IContentVersionRepository contentVersionRepository)
        {
            _referenceConverter = referenceConverter;
            _contentRepository = contentRepository;
            _contentVersionRepository = contentVersionRepository;
        }

        [HttpGet]
        [Route("GetCatalog")]
        public async Task<ActionResult<string>> GetCatalog([FromQuery] string code = null)
        {
            string log = "";

            var catalogContentLink = _referenceConverter.GetCatalogContentLink(-2147483637);
            // CustomCatalog : EPiServer.Commerce.Catalog.ContentTypes.CatalogContent
            var catalog = _contentRepository.Get<CustomCatalog>(catalogContentLink).CreateWritableClone<CustomCatalog>();
            log += catalog.Text + "\n";

            return Ok(log);
        }

        [HttpGet]
        [Route("GetCatalogLastestVersion")]
        public async Task<ActionResult<string>> GetCatalogLastestVersion([FromQuery] string code = null)
        {
            string log = "";

            var catalogContentLink = _referenceConverter.GetCatalogContentLink(-2147483637);
            // Retrieve the specific version based on VersionStatus
            var versions = _contentVersionRepository.List(catalogContentLink).ToList();
            var lastestVersion = versions
                .Where(v => v.Status == VersionStatus.Published && v.LanguageBranch == "en")
                .OrderByDescending(v => v.Saved)
                .FirstOrDefault();

            var catalog = _contentRepository.Get<CustomCatalog>(lastestVersion.ContentLink).CreateWritableClone<CustomCatalog>();
            log += catalog.Text + "\n";


            return Ok(log);
        }
    }
}
