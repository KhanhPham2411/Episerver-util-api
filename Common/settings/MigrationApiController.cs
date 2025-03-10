
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MigrateActionUrlResolver _migrateActionUrlResolver;


        public MigrationApiController(
            IRootServiceScopeFactory rootServiceScopeFactory, IHttpContextAccessor httpContextAccessor, MigrateActionUrlResolver migrateActionUrlResolver)
        {
            _rootServiceScopeFactory = rootServiceScopeFactory;
            _httpContextAccessor = httpContextAccessor;
            _migrateActionUrlResolver = migrateActionUrlResolver;
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

        [HttpGet]
        [Route("url")]
        public async Task<ActionResult<string>> Url([FromQuery] string demo = "")
        {
            string log = "";

            var path = _migrateActionUrlResolver(_httpContextAccessor.HttpContext, "index");

            return Ok(path);
        }
    }
}
