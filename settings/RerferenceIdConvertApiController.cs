
namespace Foundation.Custom
{
    [ApiController]
    [Route("id-converter")]
    public class RerferenceIdConvertApiController : ControllerBase
    {

        public RerferenceIdConvertApiController()
        {
        }

        [HttpGet]
        [Route("convert")]
        public async Task<ActionResult<string>> convert([FromQuery] string contentId = "-2147483620")
        {
            string log = "";

            const int InformationBitMask = ~(-1 << 30);

            int objectId = int.Parse(contentId) & InformationBitMask;

            log += $"The decoded values from the content ID -2147483642 are: {objectId}";
            return Ok(log);
        }

     
    }
}
