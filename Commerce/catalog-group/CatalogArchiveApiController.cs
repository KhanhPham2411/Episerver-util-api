using EPiServer.Commerce.Catalog;
using EPiServer.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.Custom.Episerver_util_api.Commerce.CatalogGroup
{
    [ApiController]
    [Route("api/catalog-archive")]
    public class CatalogArchiveApiController : ControllerBase
    {
        private readonly ICatalogArchive _catalogArchive;

        public CatalogArchiveApiController(ICatalogArchive catalogArchive)
        {
            _catalogArchive = catalogArchive;
        }

        /// <summary>
        /// Archives a catalog entry or node by ContentReference ID.
        /// Sample usage: https://localhost:5000/api/catalog-archive/archive/123
        /// </summary>
        [HttpGet("archive/{contentId}")]
        public IActionResult Archive(int contentId)
        {
            try
            {
                _catalogArchive.ArchiveContent(new ContentReference(contentId));
                return Ok(new { success = true, message = $"Content {contentId} archived." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Restores an archived entry to a parent.
        /// Sample usage: https://localhost:5000/api/catalog-archive/restore/123/456
        /// </summary>
        [HttpGet("restore/{contentId}/{parentId}")]
        public IActionResult Restore(int contentId, int parentId)
        {
            try
            {
                _catalogArchive.RestoreArchive(new ContentReference(contentId), new ContentReference(parentId));
                return Ok(new { success = true, message = $"Content {contentId} restored to parent {parentId}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes an archived entry by ContentReference ID.
        /// Sample usage: DELETE https://localhost:5000/api/catalog-archive/delete/123
        /// </summary>
        [HttpDelete("delete/{contentId}")]
        public IActionResult Delete(int contentId)
        {
            try
            {
                _catalogArchive.DeleteArchive(new ContentReference(contentId));
                return Ok(new { success = true, message = $"Archived content {contentId} deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Lists all archived items.
        /// Sample usage: https://localhost:5000/api/catalog-archive/archived
        /// </summary>
        [HttpGet("archived")]
        public IActionResult GetArchived()
        {
            var archived = _catalogArchive.GetArchivedItems();
            return Ok(archived);
        }

        /// <summary>
        /// Checks if a given entry is archived.
        /// Sample usage: https://localhost:5000/api/catalog-archive/is-archived/123
        /// </summary>
        [HttpGet("is-archived/{entryId}")]
        public IActionResult IsArchived(int entryId)
        {
            var archived = _catalogArchive.GetArchivedItems();
            bool isArchived = archived.Any(x => x.CatalogEntryId == entryId);
            return Ok(new { entryId, isArchived });
        }

        /// <summary>
        /// Checks if a given entry is published (not archived and published).
        /// Sample usage: https://localhost:5000/api/catalog-archive/is-published/123
        /// </summary>
        [HttpGet("is-published/{contentId}")]
        public IActionResult IsPublished(int contentId)
        {
            try
            {
                var contentLoader = EPiServer.ServiceLocation.ServiceLocator.Current.GetInstance<IContentLoader>();
                var publishedStateAssessor = EPiServer.ServiceLocation.ServiceLocator.Current.GetInstance<EPiServer.Core.IPublishedStateAssessor>();
                var content = contentLoader.Get<EPiServer.Core.IContent>(new EPiServer.Core.ContentReference(contentId));
                bool isPublished = publishedStateAssessor.IsPublished(content, EPiServer.Core.PublishedStateCondition.None);
                return Ok(new { contentId, isPublished, message = isPublished ? "Published" : "Not published or archived" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { contentId, isPublished = false, error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for archiving an entry.
    /// </summary>
    public class ArchiveRequest
    {
        public int ContentId { get; set; }
    }

    /// <summary>
    /// Request model for restoring an entry.
    /// </summary>
    public class RestoreRequest
    {
        public int ContentId { get; set; }
        public int ParentId { get; set; }
    }
} 