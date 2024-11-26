


using EPiServer.Find;
using EPiServer.Find.Cms;
using Foundation.Features.CatalogContent.Product;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using MetaClass = Mediachase.BusinessFoundation.Data.Meta.Management.MetaClass;
using MetaField = Mediachase.BusinessFoundation.Data.Meta.Management.MetaField;

namespace Foundation.Custom
{
    [ApiController]
    [Route("business-manager")]
    public class BusinessManagerApiController : ControllerBase
    {
       

        public BusinessManagerApiController()
        {
        }

        [HttpGet]
        [Route("list")]
        public async Task<ActionResult<string>> list([FromQuery] string keyword = "a")
        {
            string log = "";

            var metaClassName = "Organization";
            var filters = new List<FilterElement>().ToArray();
            
            var items = List(metaClassName, filters);

            log += string.Join(",", items.Select(s => s.Properties["Name"].Value));
            return Ok(log);
        }

        public static EntityObject[] List(string metaClassName, FilterElement[] filters)
        {
            var baseResponse = BusinessManager.Execute(new ListRequest(metaClassName, filters));
            ListResponse response = (ListResponse)baseResponse;

            return response.EntityObjects ?? new EntityObject[0];
        }
    }
}