
using EPiServer.Authorization;


namespace Foundation.Custom
{
    [ApiController]
    [Route("policy-options")]
    public class PolicyOptionsApiController : ControllerBase
    {
        private readonly CommercePolicyOptions _commercePolicyOptions;
        private readonly CmsPolicyOptions _cmsPolicyOptions;

        public PolicyOptionsApiController(
            CommercePolicyOptions commercePolicyOptions, CmsPolicyOptions cmsPolicyOptions)
        {
            _commercePolicyOptions = commercePolicyOptions;
            _cmsPolicyOptions = cmsPolicyOptions;
        }

        [HttpGet]
        [Route("MarketsRoles")]
        public async Task<ActionResult<string>> MarketsRoles([FromQuery] string demo = "")
        {
            string log = "";

            var roles = _commercePolicyOptions.MarketsRoles.ToList();

            log += string.Join(",", roles);
            return Ok(log);
        }

        [HttpGet]
        [Route("AdminRoles")]
        public async Task<ActionResult<string>> AdminRoles([FromQuery] string demo = "")
        {
            string log = "";

            var roles = _cmsPolicyOptions.AdminRoles.ToList();

            log += string.Join(",", roles);
            return Ok(log);
        }
    }
}
