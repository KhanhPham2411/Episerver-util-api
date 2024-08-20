using Mediachase.BusinessFoundation.Data;
using MetaClass = Mediachase.BusinessFoundation.Data.Meta.Management.MetaClass;
using MetaField = Mediachase.BusinessFoundation.Data.Meta.Management.MetaField;

namespace Foundation.Custom
{
    [ApiController]
    [Route("businessFoundation")]
    public class BusinessFoundationApiController : ControllerBase
    {
        public BusinessFoundationApiController()
        {

        }

        [HttpGet]
        [Route("ListMetaClasses")]
        public async Task<ActionResult<string>> ListMetaClasses([FromQuery] string demo = null)
        {
            if (String.IsNullOrEmpty(demo))
            {
                demo = "demo";
            }
            string log = "";

            var excludedClasses = new List<string>
            {
                "CreditCard",
                "RecentReferenceHistory",
                "CustomizationItem",
                "CustomizationItemArgument",
                "CustomPage"
            };


            var metaClasses = new List<MetaClass>();
            metaClasses.AddRange(DataContext.Current.MetaModel.MetaClasses.Cast<MetaClass>()
                .Where(m => !excludedClasses.Contains(m.Name))
                .OrderBy(m => m.Name)
                .OrderBy(m => m.AccessLevel));


            log += "List of Name metaclass:\n";
            log += string.Join(",", metaClasses.Select(s => s.Name));

            log += "| List of AccessLevel metaclass:\n";
            log += string.Join("\n", metaClasses.Select(s => s.AccessLevel));


            return Ok(log);
        }
    }
}
