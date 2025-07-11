
using EPiServer.Find;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;


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

        [HttpGet]
        [Route("listv2")]
        public async Task<ActionResult<string>> listv2([FromQuery] string keyword = "a")
        {
            string log = "";

            var metaClassName = "Organization";
            var filters = new List<FilterElement>().ToArray();
            var items = List(metaClassName, filters);

            Task.Run(() => {
                var items = List(metaClassName, filters);

                log += string.Join(",", items.Select(s => s.Properties["Name"].Value));
            });
            
               
      

            return Ok(log);
        }

        [HttpGet]
        [Route("load")]
        public async Task<ActionResult<string>> load([FromQuery] string keyword = "a")
        {
            string log = "";

            var metaClassName = "Organization";
            var primaryKeyId = new PrimaryKeyId(Guid.Empty);

            var item = Load(metaClassName, primaryKeyId);

            log += item.Properties["Name"].Value;
            return Ok(log);
        }

        public static EntityObject[] List(string metaClassName, FilterElement[] filters)
        {
            try {
                var baseResponse = BusinessManager.Execute(new ListRequest(metaClassName, filters));
                if (baseResponse == null)
                {
                    return new EntityObject[0];
                }

                ListResponse response = (ListResponse)baseResponse;

                return response.EntityObjects ?? new EntityObject[0];
            }
            catch
            {
                return new EntityObject[0];
            }
        }

        public static EntityObject Load(string metaClassName, PrimaryKeyId primaryKeyId)
        {
            try
            {
                var baseResponse = BusinessManager.Execute(new LoadRequest(new EntityObject(metaClassName, primaryKeyId)));
                if (baseResponse == null)
                {
                    return null;
                }

                LoadResponse response = (LoadResponse)baseResponse;
                return response.EntityObject;
            }
            catch (Mediachase.BusinessFoundation.Data.Meta.ObjectNotFoundException)
            {
                return null;
            }
        }
    }
}
