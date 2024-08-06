


using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Security;
using Foundation.Features.CatalogContent.Product;
using Mediachase.Commerce.Core;

namespace Foundation.Custom
{
    [ApiController]
    [Route("options")]
    public class OptionsApiController : ControllerBase
    {
        private readonly ApplicationOptions _applicationOptions;

        public OptionsApiController(ApplicationOptions applicationOptions)
        {
            _applicationOptions = applicationOptions;
        }

        [HttpGet]
        [Route("listRoles")]
        public async Task<ActionResult<string>> listRoles([FromQuery] string demo = "")
        {
            string log = "";

            var keyRoles = _applicationOptions.Roles.ToList().Select(item => item.Key);

            log += string.Join(",", keyRoles);
            return Ok(log);
        }

        [HttpGet]
        [Route("inRole")]
        public async Task<ActionResult<string>> inRole([FromQuery] string demo = "")
        {
            string log = "";

            var roleValue = _applicationOptions.Roles.GetValueOrDefault("AdminRole");
            log += $"Role value is {roleValue} => ";
            if (PrincipalInfo.CurrentPrincipal.IsInRole(roleValue))
            {
                log += $"Current user is in role {roleValue}";
                return Ok(log);
            }

            log += $"Current user is not in role {roleValue}";
            return Ok(log);
        }

    }
}
