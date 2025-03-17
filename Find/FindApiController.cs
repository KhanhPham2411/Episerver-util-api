using EPiServer.Find;
using EPiServer.Find.Cms;
using Foundation.Features.CatalogContent.Product;
using Mediachase.BusinessFoundation.Data;
using MetaClass = Mediachase.BusinessFoundation.Data.Meta.Management.MetaClass;
using MetaField = Mediachase.BusinessFoundation.Data.Meta.Management.MetaField;

namespace Foundation.Custom
{
    [ApiController]
    [Route("find")]
    public class FindApiController : ControllerBase
    {
        private readonly IClient _client;

        public FindApiController(IClient client)
        {
            _client = client;
        }

        [HttpGet]
        [Route("simple")]
        public async Task<ActionResult<string>> simple([FromQuery] string keyword = "a")
        {
            string log = "";

            var searchResult = _client.Search<GenericProduct>()
                  .For(keyword)
                  .GetContentResult();

            log += string.Join(",", searchResult.Select(s => s.Name));
            return Ok(log);
        }

        // https://docs.developers.optimizely.com/digital-experience-platform/v1.1.0-search-and-navigation/docs/terms-facets
        [HttpGet]
        [Route("facet")]
        public async Task<ActionResult<string>> facet([FromQuery] string keyword = "a")
        {
            string log = "";
        
            var searchResult = _client.Search<GenericProduct>()
                .TermsFacetFor(x => x.Industries)
                .GetContentResult();
        
            var terms = searchResult.TermsFacetFor(x => x.Industries).Terms;
        
            log += "Industries: \n";
            log += string.Join("\n", terms.Select(s => $"{s.Term}: {s.Count}"));
        
            return Ok(log);
        }

        [HttpGet]
        [Route("InFields")]
        public async Task<ActionResult<string>> InFields([FromQuery] string keyword = "a")
        {
            string log = "";

            var searchResult = _client.Search<GenericProduct>()
                  .For(keyword)
                  .InFields(s => s.Name, s => s.DisplayName)
                  .GetContentResult();

            log += string.Join(",", searchResult.Select(s => s.Name));
            return Ok(log);
        }
    }
}
