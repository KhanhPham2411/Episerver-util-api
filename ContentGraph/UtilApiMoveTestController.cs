using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.DataAccess;
using EPiServer.Security;

namespace Foundation.Custom.Episerver_util_api
{
    /// <summary>
    /// API to replicate the AddHttpContentItemWhenMoveContent duplicate key issue in Content Graph connector.
    ///
    /// </summary>
    [Route("util-api/movetest")]
    [ApiController]
    public class UtilApiMoveTestController : ControllerBase
    {
        private readonly IContentRepository _contentRepository;

        public UtilApiMoveTestController(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }

        /// <summary>
        /// Moves the specified content to the target location multiple times to trigger the duplicate key exception.
        ///
        /// Sample usage:
        ///   GET https://localhost:5000/util-api/movetest?contentId=5&targetId=6&repeat=2
        ///
        /// </summary>
        /// <param name="contentId">The content ID to move.</param>
        /// <param name="targetId">The target parent ID.</param>
        /// <param name="repeat">How many times to move (default: 2).</param>
        /// <returns>Result message or error.</returns>
        [HttpGet]
        public IActionResult MoveTest(int contentId, int targetId, int repeat = 2)
        {
            try
            {
                var contentLink = new ContentReference(contentId);
                var targetLink = new ContentReference(targetId);
                for (int i = 0; i < repeat; i++)
                {
                    _contentRepository.Move(contentLink, targetLink, AccessLevel.NoAccess, AccessLevel.NoAccess);
                }
                return Ok($"Moved content {contentId} to {targetId} {repeat} times.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
} 