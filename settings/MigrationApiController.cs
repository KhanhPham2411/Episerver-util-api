
using EPiServer.Authorization;
using EPiServer.Commerce.Internal.Migration;
using EPiServer.ServiceLocation;


namespace Foundation.Custom
{
    [ApiController]
    [Route("migration")]
    public class MigrationApiController : ControllerBase
    {
        private readonly IRootServiceScopeFactory _rootServiceScopeFactory;

        public MigrationApiController(
            IRootServiceScopeFactory rootServiceScopeFactory)
        {
            _rootServiceScopeFactory = rootServiceScopeFactory;
        }

        [HttpGet]
        [Route("run")]
        public async Task<ActionResult<string>> Run([FromQuery] string demo = "")
        {
            string log = "";

            using var scope = _rootServiceScopeFactory.CreateRootScope();
            var manager = scope.ServiceProvider.GetInstance<MigrationManager>();
            manager.Migrate();

            log += "Migration run successfully";
            return Ok(log);
        }

    }
}
