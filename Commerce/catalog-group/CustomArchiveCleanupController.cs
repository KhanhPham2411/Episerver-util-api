using EPiServer.Commerce.Catalog;
using EPiServer.Core;
using EPiServer.ServiceApi;
using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceApi.Controllers;
using EPiServer.ServiceApi.Extensions;
using EPiServer.ServiceApi.Internal;
using EPiServer.ServiceApi.Validation;
using EPiServer.ServiceApi.Configuration;
using System;

namespace Foundation.Custom.Episerver_util_api.Commerce.CatalogGroup
{
    /// <summary>
    /// Custom endpoint (Service API auth) to clear the entire Commerce archive.
    /// Sample URL: https://localhost:5000/util-api/custom-archive-cleanup/empty-archive
    /// </summary>
    [ApiController]
    [Route("util-api/custom-archive-cleanup")]
    [RequireHttpsOrClose]
    [ValidateReadOnlyMode(AllowedVerbs = HttpVerbs.Get)]
    [ExceptionHandling]
    [RequestLogging]
    [Authorize(Policy = ServiceApiAuthorizationRequirement.PolicyName)]
    public class CustomArchiveCleanupController : ControllerBase
    {
        private readonly ICatalogArchive _catalogArchive;

        public CustomArchiveCleanupController(
            ICatalogArchive catalogArchive)
            : base()
        {
            _catalogArchive = catalogArchive;
        }

        /// <summary>
        /// Empties the archive (removes all archived entries/nodes) and cleans up invalid relations.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-cleanup/empty-archive
        /// </summary>
        [HttpPost("empty-archive")]
        [EPiServer.ServiceApi.Configuration.AuthorizePermission(EPiServer.ServiceApi.Configuration.Permissions.GroupName, EPiServer.ServiceApi.Configuration.Permissions.Read)]
        public IActionResult EmptyArchive()
        {
            try
            {
                _catalogArchive.DeleteAll();

                return Ok(new
                {
                    success = true,
                    message = "Archive successfully emptied."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }
    }
}

