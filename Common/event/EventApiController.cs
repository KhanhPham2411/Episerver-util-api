using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using Foundation.Infrastructure.Cms.Users;
using Mediachase.Commerce.Catalog.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("event-api")]
    public class EventApiController : ControllerBase
    {
        public EventApiController(ApplicationDbContext<SiteUser> db)
        {
        }

        [HttpGet]
        [Route("entry-updated")]
        public async Task<ActionResult<string>> EntryUpdated()
        {
            EventContext.Instance.EntryUpdated += _entryUpdatedHandler;

            return Ok("register EntryUpdated successfully");
        }

        private void _entryUpdatedHandler(object sender, EntryEventArgs e)
        {
            Console.WriteLine(e);
        }
    }
}