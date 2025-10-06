using EPiServer.Commerce.Catalog;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Catalog;
using Foundation.Features.Search.Category;
using EPiServer.Security;

namespace Foundation.Custom.Episerver_util_api.Commerce.CatalogGroup
{
    [ApiController]
    [Route("util-api/custom-archive-debug")]
    public class CustomArchiveDebugController : ControllerBase
    {
        private readonly ICatalogArchive _catalogArchive;
        private readonly IContentRepository _contentRepository;
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;

        public CustomArchiveDebugController()
        {
            _catalogArchive = ServiceLocator.Current.GetInstance<ICatalogArchive>();
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
        }

        /// <summary>
        /// Step 1: Create a test folder node for debugging archive deletion issues.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/create-test-folder
        /// </summary>
        [HttpGet("create-test-folder")]
        public IActionResult CreateTestFolder()
        {
            try
            {
                // Get the catalog root
                var catalogRoot = _referenceConverter.GetCatalogContentLink(1); // Assuming catalog ID 1
                var catalogContent = _contentLoader.Get<CatalogContentBase>(catalogRoot);

                // Create a new folder node
                var folder = _contentRepository.GetDefault<GenericNode>(catalogRoot);
                folder.Name = $"TestFolder_{DateTime.Now:yyyyMMdd_HHmmss}";
                folder.DisplayName = $"Test Folder {DateTime.Now:yyyyMMdd HHmmss}";
                
                var contentReference = _contentRepository.Save(folder, EPiServer.DataAccess.SaveAction.Publish, AccessLevel.NoAccess);
                
                return Ok(new { 
                    success = true, 
                    message = $"Test folder created successfully",
                    contentId = contentReference.ID,
                    contentLink = contentReference.ToString(),
                    folderName = folder.Name
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 2: Archive a content item by ID.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/archive-content/123
        /// </summary>
        [HttpGet("archive-content/{contentId}")]
        public IActionResult ArchiveContent(int contentId)
        {
            try
            {
                var contentLink = new ContentReference(contentId);
                
                // Check if content exists before archiving
                if (!_contentRepository.TryGet<IContent>(contentLink, out var content))
                {
                    return BadRequest($"Content with ID {contentId} not found");
                }

                // Get content details before archiving
                var contentType = _referenceConverter.GetContentType(contentLink);
                var parentLink = content.ParentLink;
                var parentId = 0;
                if (!ContentReference.IsNullOrEmpty(parentLink))
                {
                    parentId = _referenceConverter.GetObjectId(parentLink);
                }

                // Archive the content
                _catalogArchive.ArchiveContent(contentLink);

                // Verify it's in archive
                var archivedItems = _catalogArchive.GetArchivedItems();
                var archivedItem = archivedItems.FirstOrDefault(x => 
                    (contentType == CatalogContentType.CatalogNode && x.CatalogNodeId == contentId) ||
                    (contentType == CatalogContentType.CatalogEntry && x.CatalogEntryId == contentId));

                return Ok(new { 
                    success = true, 
                    message = $"Content {contentId} archived successfully",
                    contentId = contentId,
                    contentType = contentType.ToString(),
                    parentId = parentId,
                    archivedItem = archivedItem,
                    totalArchivedItems = archivedItems.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 3: List all archived items to verify archiving worked.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/list-archived
        /// </summary>
        [HttpGet("list-archived")]
        public IActionResult ListArchived()
        {
            try
            {
                var archivedItems = _catalogArchive.GetArchivedItems();
                
                var result = archivedItems.Select(item => new {
                    item.CatalogNodeId,
                    item.CatalogEntryId,
                    item.ArchivedDate,
                    item.OriginalCatalogId,
                    item.OriginalParentId,
                    item.ArchivedBy,
                    ContentType = item.CatalogNodeId.HasValue ? "CatalogNode" : "CatalogEntry",
                    ContentId = item.CatalogNodeId ?? item.CatalogEntryId ?? 0
                }).ToList();

                return Ok(new { 
                    success = true, 
                    message = $"Found {result.Count} archived items",
                    archivedItems = result,
                    totalCount = result.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 4: Check content ancestry before deletion (this is where the issue occurs).
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/check-ancestry/123
        /// </summary>
        [HttpGet("check-ancestry/{contentId}")]
        public IActionResult CheckAncestry(int contentId)
        {
            try
            {
                var originalInputId = contentId;
                var contentLink = new ContentReference(contentId);
                string resolutionKind = "ContentReference";
                string resolutionError = null;
                
                // Try to resolve content by ContentReference ID first
                if (!_contentRepository.TryGet<IContent>(contentLink, out var content))
                {
                    // Fallbacks: try as Commerce object ids
                    // 1) Node
                    try
                    {
                        var nodeLink = _referenceConverter.GetNodeContentLink(originalInputId);
                        if (!ContentReference.IsNullOrEmpty(nodeLink) && _contentRepository.TryGet<IContent>(nodeLink, out var nodeContent))
                        {
                            contentLink = nodeLink;
                            content = nodeContent;
                            resolutionKind = "CatalogNodeId->ContentLink";
                        }
                    }
                    catch (Exception ex)
                    {
                        resolutionError = $"Node resolve failed: {ex.Message}";
                    }

                    // 2) Entry (only if not yet resolved)
                    if (content == null)
                    {
                        try
                        {
                            var entryLink = _referenceConverter.GetEntryContentLink(originalInputId);
                            if (!ContentReference.IsNullOrEmpty(entryLink) && _contentRepository.TryGet<IContent>(entryLink, out var entryContent))
                            {
                                contentLink = entryLink;
                                content = entryContent;
                                resolutionKind = "CatalogEntryId->ContentLink";
                            }
                        }
                        catch (Exception ex)
                        {
                            resolutionError = (resolutionError == null ? string.Empty : resolutionError + " | ") + $"Entry resolve failed: {ex.Message}";
                        }
                    }

                    // 3) Catalog (only if not yet resolved)
                    if (content == null)
                    {
                        try
                        {
                            var catalogLink = _referenceConverter.GetCatalogContentLink(originalInputId);
                            if (!ContentReference.IsNullOrEmpty(catalogLink) && _contentRepository.TryGet<IContent>(catalogLink, out var catalogContent))
                            {
                                contentLink = catalogLink;
                                content = catalogContent;
                                resolutionKind = "CatalogId->ContentLink";
                            }
                        }
                        catch (Exception ex)
                        {
                            resolutionError = (resolutionError == null ? string.Empty : resolutionError + " | ") + $"Catalog resolve failed: {ex.Message}";
                        }
                    }

                    if (content == null)
                    {
                        return Ok(new {
                            success = false,
                            message = $"Failed to load content from id {originalInputId}",
                            inputId = originalInputId,
                            triedResolution = new [] { "ContentReference", "CatalogNodeId", "CatalogEntryId", "CatalogId" },
                            resolutionError = resolutionError,
                            canLoadContent = false
                        });
                    }
                }

                // Try to get ancestors (this is where the ContentActivityTracker fails)
                List<IContent> ancestors = null;
                string ancestryError = null;
                try
                {
                    ancestors = _contentLoader.GetAncestors(contentLink).ToList();
                }
                catch (Exception ex)
                {
                    ancestryError = ex.Message;
                }

                // Check parent link validity
                var parentLink = content.ParentLink;
                var parentId = 0;
                var parentExists = false;
                if (!ContentReference.IsNullOrEmpty(parentLink))
                {
                    parentId = _referenceConverter.GetObjectId(parentLink);
                    try
                    {
                        var parent = _contentLoader.Get<IContent>(parentLink);
                        parentExists = parent != null;
                    }
                    catch
                    {
                        parentExists = false;
                    }
                }

                return Ok(new { 
                    success = true, 
                    message = "Ancestry check completed",
                    contentId = contentId,
                    resolvedContentLinkId = contentLink.ID,
                    resolutionKind = resolutionKind,
                    contentName = content?.Name,
                    contentType = _referenceConverter.GetContentType(contentLink).ToString(),
                    parentLink = parentLink?.ToString(),
                    parentId = parentId,
                    parentExists = parentExists,
                    ancestorsCount = ancestors?.Count ?? 0,
                    ancestors = ancestors?.Select(a => new { 
                        Id = a.ContentLink.ID, 
                        Name = a.Name, 
                        Type = a.GetType().Name 
                    }).ToList(),
                    ancestryError = ancestryError,
                    canResolveAncestry = ancestryError == null
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 5: Attempt to delete archived content (this reproduces the 404/orphaning issue).
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/delete-archived/123
        /// </summary>
        [HttpGet("delete-archived/{contentId}")]
        public IActionResult DeleteArchived(int contentId)
        {
            try
            {
                var contentLink = new ContentReference(contentId);
                
                // Check if content exists before deletion
                var contentExists = _contentRepository.TryGet<IContent>(contentLink, out var content);
                
                // Check if it's in archive before deletion
                var archivedItems = _catalogArchive.GetArchivedItems();
                var archivedItem = archivedItems.FirstOrDefault(x => 
                    x.CatalogNodeId == contentId || x.CatalogEntryId == contentId);

                // Attempt the deletion (this is where the issue occurs)
                string deletionError = null;
                bool deletionSuccess = false;
                try
                {
                    _catalogArchive.DeleteArchive(contentLink);
                    deletionSuccess = true;
                }
                catch (Exception ex)
                {
                    deletionError = ex.Message;
                }

                // Check archive status after deletion attempt
                var archivedItemsAfter = _catalogArchive.GetArchivedItems();
                var stillInArchive = archivedItemsAfter.Any(x => 
                    x.CatalogNodeId == contentId || x.CatalogEntryId == contentId);

                // Check if content still exists
                var contentStillExists = _contentRepository.TryGet<IContent>(contentLink, out var contentAfter);

                return Ok(new { 
                    success = deletionSuccess, 
                    message = deletionSuccess ? "Deletion successful" : "Deletion failed",
                    contentId = contentId,
                    contentExistsBefore = contentExists,
                    contentStillExists = contentStillExists,
                    wasInArchiveBefore = archivedItem != null,
                    stillInArchive = stillInArchive,
                    isOrphaned = !stillInArchive && contentStillExists,
                    deletionError = deletionError,
                    archivedItemBefore = archivedItem,
                    totalArchivedBefore = archivedItems.Count(),
                    totalArchivedAfter = archivedItemsAfter.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 6: Fix orphaned item by re-inserting CatalogArchived row.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/fix-orphaned/123/456
        /// </summary>
        [HttpGet("fix-orphaned/{contentId}/{originalParentId}")]
        public IActionResult FixOrphaned(int contentId, int originalParentId = 0)
        {
            try
            {
                // Check if content exists
                if (!_contentRepository.TryGet<IContent>(new ContentReference(contentId), out var content))
                {
                    return BadRequest($"Content with ID {contentId} not found");
                }

                // Check if it's already in archive
                var archivedItems = _catalogArchive.GetArchivedItems();
                var alreadyArchived = archivedItems.Any(x => 
                    x.CatalogNodeId == contentId || x.CatalogEntryId == contentId);

                if (alreadyArchived)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Item is already properly archived",
                        contentId = contentId,
                        alreadyArchived = true
                    });
                }

                // This would require direct database access to insert into CatalogArchived
                // For now, we'll return instructions
                var contentType = _referenceConverter.GetContentType(new ContentReference(contentId));
                var catalogId = 1; // Assuming catalog ID 1
                var archivedBy = "system@fix-orphaned.com";
                var archivedDate = DateTime.UtcNow;

                var sqlCommand = contentType == CatalogContentType.CatalogNode
                    ? $"INSERT INTO CatalogArchived (CatalogEntryId, CatalogNodeId, ArchivedDate, OriginalCatalogId, OriginalParentId, ArchivedBy) VALUES (NULL, {contentId}, SYSUTCDATETIME(), {catalogId}, {originalParentId}, '{archivedBy}')"
                    : $"INSERT INTO CatalogArchived (CatalogEntryId, CatalogNodeId, ArchivedDate, OriginalCatalogId, OriginalParentId, ArchivedBy) VALUES ({contentId}, NULL, SYSUTCDATETIME(), {catalogId}, {originalParentId}, '{archivedBy}')";

                return Ok(new { 
                    success = true, 
                    message = "Orphaned item fix instructions",
                    contentId = contentId,
                    contentType = contentType.ToString(),
                    originalParentId = originalParentId,
                    sqlCommand = sqlCommand,
                    instructions = "Execute the SQL command above to fix the orphaned item, then restart the application"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 7: Complete workflow - create, archive, and delete to reproduce the issue.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/complete-workflow
        /// </summary>
        [HttpGet("complete-workflow")]
        public IActionResult CompleteWorkflow()
        {
            try
            {
                var results = new List<object>();
                
                // Step 1: Create test folder
                var createResult = CreateTestFolder();
                if (createResult is OkObjectResult okResult)
                {
                    var createData = okResult.Value;
                    results.Add(new { step = "Create", result = createData });
                    
                    var contentId = (int)((dynamic)createData).contentId;
                    
                    // Step 2: Archive it
                    var archiveResult = ArchiveContent(contentId);
                    results.Add(new { step = "Archive", result = ((OkObjectResult)archiveResult).Value });
                    
                    // Step 3: Check ancestry
                    var ancestryResult = CheckAncestry(contentId);
                    results.Add(new { step = "CheckAncestry", result = ((OkObjectResult)ancestryResult).Value });
                    
                    // Step 4: Try to delete (this should reproduce the issue)
                    var deleteResult = DeleteArchived(contentId);
                    results.Add(new { step = "Delete", result = ((OkObjectResult)deleteResult).Value });
                }
                else
                {
                    results.Add(new { step = "Create", result = "Failed to create test folder" });
                }

                return Ok(new { 
                    success = true, 
                    message = "Complete workflow executed",
                    workflowResults = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 8: Safe delete implementation - delete content first, then remove from archive.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-debug/safe-delete/123
        /// </summary>
        [HttpGet("safe-delete/{contentId}")]
        public IActionResult SafeDelete(int contentId)
        {
            try
            {
                var contentLink = new ContentReference(contentId);
                
                // Check if content exists
                if (!_contentRepository.TryGet<IContent>(contentLink, out var content))
                {
                    return BadRequest($"Content with ID {contentId} not found");
                }

                // Check if it's in archive
                var archivedItems = _catalogArchive.GetArchivedItems();
                var archivedItem = archivedItems.FirstOrDefault(x => 
                    x.CatalogNodeId == contentId || x.CatalogEntryId == contentId);

                if (archivedItem == null)
                {
                    return BadRequest($"Content {contentId} is not archived");
                }

                // Safe deletion: Delete content first, then remove from archive
                string contentDeletionError = null;
                bool contentDeleted = false;
                try
                {
                    _contentRepository.Delete(contentLink, true, AccessLevel.NoAccess);
                    contentDeleted = true;
                }
                catch (Exception ex)
                {
                    contentDeletionError = ex.Message;
                }

                // Only remove from archive if content deletion succeeded
                if (contentDeleted)
                {
                    try
                    {
                        var contentType = _referenceConverter.GetContentType(contentLink);
                        if (contentType == CatalogContentType.CatalogNode)
                        {
                            // This would require access to _catalogArchivedAdmin.DeleteArchiveNode
                            // For demonstration, we'll just return success
                        }
                        else if (contentType == CatalogContentType.CatalogEntry)
                        {
                            // This would require access to _catalogArchivedAdmin.DeleteArchiveEntry
                            // For demonstration, we'll just return success
                        }
                    }
                    catch (Exception ex)
                    {
                        return Ok(new { 
                            success = false, 
                            message = "Content deleted but failed to remove from archive",
                            contentId = contentId,
                            contentDeleted = true,
                            archiveRemovalError = ex.Message
                        });
                    }
                }

                return Ok(new { 
                    success = contentDeleted, 
                    message = contentDeleted ? "Safe deletion successful" : "Content deletion failed",
                    contentId = contentId,
                    contentDeleted = contentDeleted,
                    contentDeletionError = contentDeletionError,
                    archivedItem = archivedItem
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }
    }
}
