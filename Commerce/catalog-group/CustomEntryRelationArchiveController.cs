using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.ServiceApi.Commerce.Controllers.Catalog.Construction;
using EPiServer.ServiceApi.Commerce.Controllers.Catalog.Persistence.Internal;
using EPiServer.ServiceApi.Commerce.Models.Catalog;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Data;
using Mediachase.Commerce.Catalog.Managers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.Custom.Episerver_util_api.Commerce.CatalogGroup
{
    /// <summary>
    /// Controller to replicate Zendesk ticket 1796210: Archived entries can be linked via Service API entryrelations endpoint.
    /// Get sample URLs with real data: https://localhost:5000/util-api/custom-entry-relation-archive/get-sample-urls
    /// Real examples: 
    /// - Products: P-39205836, P-22471481, P-22471486
    /// - Variants: SKU-39206333, SKU-39205836, SKU-22471481
    /// - Archived: P-40977269 (ASHBURY DRESS)
    /// </summary>
    [ApiController]
    [Route("util-api/custom-entry-relation-archive")]
    public class CustomEntryRelationArchiveController : ControllerBase
    {
        private readonly ICatalogArchive _catalogArchive;
        private readonly IContentRepository _contentRepository;
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;
        private readonly EntryRelationModelCommitter _entryRelationModelCommitter;
        private readonly IRelationRepository _relationRepository;
        private readonly ICatalogSystem _catalogSystem;
        private readonly IdentityResolver _identityResolver;

        public CustomEntryRelationArchiveController()
        {
            _catalogArchive = ServiceLocator.Current.GetInstance<ICatalogArchive>();
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            _entryRelationModelCommitter = ServiceLocator.Current.GetInstance<EntryRelationModelCommitter>();
            _relationRepository = ServiceLocator.Current.GetInstance<IRelationRepository>();
            _catalogSystem = ServiceLocator.Current.GetInstance<ICatalogSystem>();
            _identityResolver = ServiceLocator.Current.GetInstance<IdentityResolver>();
        }

        /// <summary>
        /// Step 1: Check if an entry is archived by entry code.
        /// Sample URLs: 
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/check-entry-archived/P-39205836
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/check-entry-archived/SKU-39206333
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/check-entry-archived/P-40977269 (archived entry)
        /// </summary>
        [HttpGet("check-entry-archived/{entryCode}")]
        public IActionResult CheckEntryArchived(string entryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entryCode))
                {
                    return BadRequest("Entry code is required.");
                }

                // Get entry content link
                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(entryLink))
                {
                    return BadRequest($"Entry with code '{entryCode}' not found.");
                }

                // Try to load the entry
                var entryExists = _contentLoader.TryGet<EntryContentBase>(entryLink, out var entry);
                
                // Check if entry is in archive
                var archivedItems = _catalogArchive.GetArchivedItems();
                var entryId = _referenceConverter.GetObjectId(entryLink);
                var archivedItem = archivedItems.FirstOrDefault(x => x.CatalogEntryId == entryId);

                // Get catalog info
                var catalogId = entry?.CatalogId ?? 0;
                var catalogLink = catalogId > 0 ? _referenceConverter.GetContentLink(catalogId, CatalogContentType.Catalog, 0) : ContentReference.EmptyReference;
                var catalogName = "Unknown";
                if (!ContentReference.IsNullOrEmpty(catalogLink))
                {
                    var catalog = _contentLoader.Get<CatalogContent>(catalogLink);
                    catalogName = catalog?.Name ?? "Unknown";
                }

                var isArchived = archivedItem != null;
                var isInArchiveCatalog = catalogName.Equals("System.Archived", StringComparison.OrdinalIgnoreCase);

                return Ok(new
                {
                    success = true,
                    message = $"Entry '{entryCode}' status checked",
                    entryCode = entryCode,
                    entryId = entryId,
                    entryExists = entryExists,
                    entryName = entry?.Name,
                    catalogId = catalogId,
                    catalogName = catalogName,
                    isArchived = isArchived,
                    isInArchiveCatalog = isInArchiveCatalog,
                    archivedItem = archivedItem != null ? new
                    {
                        archivedItem.ArchivedDate,
                        archivedItem.ArchivedBy,
                        archivedItem.OriginalCatalogId,
                        archivedItem.OriginalParentId
                    } : null,
                    shouldBlockRelation = isArchived || isInArchiveCatalog
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 2: Archive an entry by entry code.
        /// Sample URL: https://localhost:5000/util-api/custom-entry-relation-archive/archive-entry/{entryCode}
        /// </summary>
        [HttpGet("archive-entry/{entryCode}")]
        public IActionResult ArchiveEntry(string entryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entryCode))
                {
                    return BadRequest("Entry code is required.");
                }

                // Get entry content link
                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(entryLink))
                {
                    return BadRequest($"Entry with code '{entryCode}' not found.");
                }

                // Check if entry exists
                if (!_contentLoader.TryGet<EntryContentBase>(entryLink, out var entry))
                {
                    return BadRequest($"Entry with code '{entryCode}' could not be loaded.");
                }

                // Check if already archived
                var archivedItemsBefore = _catalogArchive.GetArchivedItems();
                var entryId = _referenceConverter.GetObjectId(entryLink);
                var alreadyArchived = archivedItemsBefore.Any(x => x.CatalogEntryId == entryId);

                if (alreadyArchived)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Entry '{entryCode}' is already archived",
                        entryCode = entryCode,
                        entryId = entryId,
                        alreadyArchived = true
                    });
                }

                // Get entry details before archiving
                var catalogId = entry.CatalogId;
                var parentLink = entry.ParentLink;
                var parentId = 0;
                if (!ContentReference.IsNullOrEmpty(parentLink))
                {
                    parentId = _referenceConverter.GetObjectId(parentLink);
                }

                // Archive the entry
                _catalogArchive.ArchiveContent(entryLink);

                // Verify it's in archive
                var archivedItemsAfter = _catalogArchive.GetArchivedItems();
                var archivedItem = archivedItemsAfter.FirstOrDefault(x => x.CatalogEntryId == entryId);

                return Ok(new
                {
                    success = true,
                    message = $"Entry '{entryCode}' archived successfully",
                    entryCode = entryCode,
                    entryId = entryId,
                    entryName = entry.Name,
                    catalogId = catalogId,
                    parentId = parentId,
                    archivedItem = archivedItem != null ? new
                    {
                        archivedItem.ArchivedDate,
                        archivedItem.ArchivedBy,
                        archivedItem.OriginalCatalogId,
                        archivedItem.OriginalParentId
                    } : null,
                    totalArchivedItems = archivedItemsAfter.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 3: Create a relation to an archived entry (THIS REPLICATES THE BUG).
        /// This demonstrates that Service API allows creating relations to archived entries.
        /// Sample URLs:
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/create-relation-to-archived?parentEntryCode=P-39205836&childEntryCode=SKU-39206333&relationType=ProductVariation
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/create-relation-to-archived?parentEntryCode=P-22471481&childEntryCode=SKU-22471481&relationType=ProductVariation
        /// </summary>
        [HttpGet("create-relation-to-archived")]
        public IActionResult CreateRelationToArchived(string parentEntryCode, string childEntryCode, string relationType = "ProductVariation")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parentEntryCode))
                {
                    return BadRequest("parentEntryCode is required.");
                }

                if (string.IsNullOrWhiteSpace(childEntryCode))
                {
                    return BadRequest("childEntryCode is required.");
                }

                if (string.IsNullOrWhiteSpace(relationType))
                {
                    return BadRequest("relationType is required.");
                }

                // Check if child entry is archived BEFORE creating relation
                var childEntryLink = _referenceConverter.GetContentLink(childEntryCode, CatalogContentType.CatalogEntry);
                var childEntryId = ContentReference.IsNullOrEmpty(childEntryLink) ? 0 : _referenceConverter.GetObjectId(childEntryLink);
                var archivedItems = _catalogArchive.GetArchivedItems();
                var childIsArchived = archivedItems.Any(x => x.CatalogEntryId == childEntryId);

                // Check if parent entry is archived
                var parentEntryLink = _referenceConverter.GetContentLink(parentEntryCode, CatalogContentType.CatalogEntry);
                var parentEntryId = ContentReference.IsNullOrEmpty(parentEntryLink) ? 0 : _referenceConverter.GetObjectId(parentEntryLink);
                var parentIsArchived = archivedItems.Any(x => x.CatalogEntryId == parentEntryId);

                // Create the relation using Service API logic (this is what the bug does)
                var relation = new EPiServer.ServiceApi.Commerce.Models.Catalog.EntryRelation
                {
                    ParentEntryCode = parentEntryCode,
                    ChildEntryCode = childEntryCode,
                    RelationType = relationType,
                    Quantity = 1,
                    SortOrder = 0,
                    GroupName = string.Empty
                };

                // This is the problematic call - it doesn't check if entries are archived
                var relationCreated = _entryRelationModelCommitter.SaveCatalogEntryRelation(relation);

                // Get relation details after creation
                var relationsAfter = GetEntryRelations(parentEntryCode);

                return Ok(new
                {
                    success = relationCreated,
                    message = relationCreated 
                        ? $"Relation created successfully (BUG: This should have been blocked!)" 
                        : "Failed to create relation",
                    warning = (childIsArchived || parentIsArchived) 
                        ? "WARNING: One or both entries are archived, but relation was still created/attempted!" 
                        : null,
                    parentEntryCode = parentEntryCode,
                    parentEntryId = parentEntryId,
                    parentIsArchived = parentIsArchived,
                    childEntryCode = childEntryCode,
                    childEntryId = childEntryId,
                    childIsArchived = childIsArchived,
                    relationType = relationType,
                    relationCreated = relationCreated,
                    relationsAfter = relationsAfter
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 4: List all relations for an entry by entry code.
        /// Sample URLs:
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/list-relations/P-39205836
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/list-relations/P-22471481
        /// </summary>
        [HttpGet("list-relations/{entryCode}")]
        public IActionResult ListRelations(string entryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entryCode))
                {
                    return BadRequest("Entry code is required.");
                }

                var relations = GetEntryRelations(entryCode);

                // Check if entry is archived
                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                var entryId = ContentReference.IsNullOrEmpty(entryLink) ? 0 : _referenceConverter.GetObjectId(entryLink);
                var archivedItems = _catalogArchive.GetArchivedItems();
                var isArchived = archivedItems.Any(x => x.CatalogEntryId == entryId);

                // Check each related entry if it's archived
                var relationsWithArchiveStatus = relations.Select(r => new
                {
                    r.ChildEntryCode,
                    r.ParentEntryCode,
                    r.RelationType,
                    r.Quantity,
                    r.SortOrder,
                    r.GroupName,
                    childIsArchived = CheckIfEntryArchived(r.ChildEntryCode),
                    parentIsArchived = CheckIfEntryArchived(r.ParentEntryCode),
                    hasInvalidRelation = CheckIfEntryArchived(r.ChildEntryCode) || CheckIfEntryArchived(r.ParentEntryCode)
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Relations for entry '{entryCode}'",
                    entryCode = entryCode,
                    entryId = entryId,
                    isArchived = isArchived,
                    totalRelations = relations.Count,
                    relations = relationsWithArchiveStatus,
                    invalidRelations = relationsWithArchiveStatus.Where(r => r.hasInvalidRelation).ToList(),
                    invalidRelationsCount = relationsWithArchiveStatus.Count(r => r.hasInvalidRelation)
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 5: Clean up invalid relations (relations crossing archive boundary).
        /// Sample URL: https://localhost:5000/util-api/custom-entry-relation-archive/cleanup-invalid-relations
        /// </summary>
        [HttpGet("cleanup-invalid-relations")]
        public IActionResult CleanupInvalidRelations()
        {
            try
            {
                // Use the built-in method to delete relations over archive boundary
                CatalogRelationManager.DeleteRelationsOverArchiveBoundary();

                // Get archived items count
                var archivedItems = _catalogArchive.GetArchivedItems();

                return Ok(new
                {
                    success = true,
                    message = "Invalid relations cleaned up successfully",
                    totalArchivedItems = archivedItems.Count(),
                    note = "Relations crossing archive boundary have been deleted"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 6: Get entry details including catalog information.
        /// Sample URLs:
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/get-entry-details/P-39205836
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/get-entry-details/SKU-39206333
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/get-entry-details/P-40977269 (archived entry)
        /// </summary>
        [HttpGet("get-entry-details/{entryCode}")]
        public IActionResult GetEntryDetails(string entryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entryCode))
                {
                    return BadRequest("Entry code is required.");
                }

                // Get entry content link
                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(entryLink))
                {
                    return BadRequest($"Entry with code '{entryCode}' not found.");
                }

                // Load entry
                if (!_contentLoader.TryGet<EntryContentBase>(entryLink, out var entry))
                {
                    return BadRequest($"Entry with code '{entryCode}' could not be loaded.");
                }

                var entryId = _referenceConverter.GetObjectId(entryLink);
                var catalogId = entry.CatalogId;
                var catalogLink = _referenceConverter.GetContentLink(catalogId, CatalogContentType.Catalog, 0);
                var catalog = _contentLoader.Get<CatalogContent>(catalogLink);
                var catalogName = catalog?.Name ?? "Unknown";

                // Check archive status
                var archivedItems = _catalogArchive.GetArchivedItems();
                var archivedItem = archivedItems.FirstOrDefault(x => x.CatalogEntryId == entryId);
                var isArchived = archivedItem != null;
                var isInArchiveCatalog = catalogName.Equals("System.Archived", StringComparison.OrdinalIgnoreCase);

                // Get parent info
                var parentLink = entry.ParentLink;
                var parentId = 0;
                var parentCode = string.Empty;
                if (!ContentReference.IsNullOrEmpty(parentLink))
                {
                    parentId = _referenceConverter.GetObjectId(parentLink);
                    if (_contentLoader.TryGet<EntryContentBase>(parentLink, out var parent))
                    {
                        parentCode = parent.Code;
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Entry details for '{entryCode}'",
                    entryCode = entryCode,
                    entryId = entryId,
                    entryName = entry.Name,
                    entryType = entry.ClassTypeId,
                    catalogId = catalogId,
                    catalogName = catalogName,
                    catalogGuid = catalog?.ContentGuid,
                    parentId = parentId,
                    parentCode = parentCode,
                    isArchived = isArchived,
                    isInArchiveCatalog = isInArchiveCatalog,
                    archivedItem = archivedItem != null ? new
                    {
                        archivedItem.ArchivedDate,
                        archivedItem.ArchivedBy,
                        archivedItem.OriginalCatalogId,
                        archivedItem.OriginalParentId
                    } : null,
                    shouldBlockInServiceApi = isArchived || isInArchiveCatalog
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 0: Get sample URLs with real data from database for testing.
        /// Sample URL: https://localhost:5000/util-api/custom-entry-relation-archive/get-sample-urls
        /// </summary>
        [HttpGet("get-sample-urls")]
        public IActionResult GetSampleUrls()
        {
            try
            {
                // Real data from database:
                // Products: P-39205836, P-22471481, P-22471486, P-22471487, P-22471422
                // Variants: SKU-39206333, SKU-39205836, SKU-22471481, SKU-36210819, SKU-22471486
                // Archived: P-40977269 (ASHBURY DRESS)

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var sampleUrls = new
                {
                    baseUrl = baseUrl,
                    samples = new
                    {
                        checkEntryArchived = new[]
                        {
                            $"{baseUrl}/util-api/custom-entry-relation-archive/check-entry-archived/P-39205836",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/check-entry-archived/SKU-39206333",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/check-entry-archived/P-40977269" // Archived entry
                        },
                        archiveEntry = new[]
                        {
                            $"{baseUrl}/util-api/custom-entry-relation-archive/archive-entry/SKU-39206333",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/archive-entry/SKU-39205836"
                        },
                        createRelationToArchived = new[]
                        {
                            $"{baseUrl}/util-api/custom-entry-relation-archive/create-relation-to-archived?parentEntryCode=P-39205836&childEntryCode=SKU-39206333&relationType=ProductVariation",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/create-relation-to-archived?parentEntryCode=P-22471481&childEntryCode=SKU-22471481&relationType=ProductVariation"
                        },
                        listRelations = new[]
                        {
                            $"{baseUrl}/util-api/custom-entry-relation-archive/list-relations/P-39205836",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/list-relations/P-22471481"
                        },
                        getEntryDetails = new[]
                        {
                            $"{baseUrl}/util-api/custom-entry-relation-archive/get-entry-details/P-39205836",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/get-entry-details/SKU-39206333",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/get-entry-details/P-40977269" // Archived entry
                        },
                        completeWorkflow = new[]
                        {
                            $"{baseUrl}/util-api/custom-entry-relation-archive/complete-workflow?parentEntryCode=P-39205836&childEntryCode=SKU-39206333&relationType=ProductVariation",
                            $"{baseUrl}/util-api/custom-entry-relation-archive/complete-workflow?parentEntryCode=P-22471481&childEntryCode=SKU-22471481&relationType=ProductVariation"
                        },
                        cleanupInvalidRelations = new[]
                        {
                            $"{baseUrl}/util-api/custom-entry-relation-archive/cleanup-invalid-relations"
                        }
                    },
                    realDataFromDatabase = new
                    {
                        products = new[]
                        {
                            new { code = "P-39205836", name = "TUXEDO SWEATSHIRT", id = 83 },
                            new { code = "P-22471481", name = "SILK MIX SWEATER", id = 80 },
                            new { code = "P-22471486", name = "SILK MIX ROLLER NECK SWEATER", id = 77 },
                            new { code = "P-22471487", name = "SILK MIX ROLLER NECK SWEATER", id = 74 },
                            new { code = "P-22471422", name = "SILK MIX SWEATER", id = 71 }
                        },
                        variants = new[]
                        {
                            new { code = "SKU-39206333", name = "TUXEDO SWEATSHIRT", id = 85, parentCode = "P-39205836" },
                            new { code = "SKU-39205836", name = "TUXEDO SWEATSHIRT", id = 84, parentCode = "P-39205836" },
                            new { code = "SKU-22471481", name = "SILK MIX SWEATER", id = 81, parentCode = "P-22471481" },
                            new { code = "SKU-36210819", name = "SILK MIX SWEATER", id = 82, parentCode = "P-22471481" },
                            new { code = "SKU-22471486", name = "SILK MIX ROLLER NECK SWEATER", id = 78, parentCode = "P-22471486" }
                        },
                        archivedEntries = new[]
                        {
                            new { code = "P-40977269", name = "ASHBURY DRESS", id = 19, archivedDate = "2025-11-12T04:16:03.553Z", archivedBy = "admin@example.com" }
                        },
                        existingRelations = new[]
                        {
                            new { parentCode = "P-39205836", childCode = "SKU-39206333", relationType = "ProductVariation" },
                            new { parentCode = "P-39205836", childCode = "SKU-39205836", relationType = "ProductVariation" },
                            new { parentCode = "P-22471481", childCode = "SKU-36210819", relationType = "ProductVariation" },
                            new { parentCode = "P-22471481", childCode = "SKU-22471481", relationType = "ProductVariation" }
                        }
                    },
                    recommendedTestFlow = new[]
                    {
                        "1. Check if entry is archived: /check-entry-archived/P-40977269 (should return isArchived=true)",
                        "2. Archive a variant: /archive-entry/SKU-39206333",
                        "3. Verify it's archived: /check-entry-archived/SKU-39206333",
                        "4. Create relation to archived (BUG): /create-relation-to-archived?parentEntryCode=P-39205836&childEntryCode=SKU-39206333&relationType=ProductVariation",
                        "5. List relations to see invalid one: /list-relations/P-39205836",
                        "6. Cleanup invalid relations: /cleanup-invalid-relations"
                    }
                };

                return Ok(sampleUrls);
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 7: Complete workflow to replicate the issue - archive entry, then create relation.
        /// Sample URLs:
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/complete-workflow?parentEntryCode=P-39205836&childEntryCode=SKU-39206333&relationType=ProductVariation
        /// - https://localhost:5000/util-api/custom-entry-relation-archive/complete-workflow?parentEntryCode=P-22471481&childEntryCode=SKU-22471481&relationType=ProductVariation
        /// </summary>
        [HttpGet("complete-workflow")]
        public IActionResult CompleteWorkflow(string parentEntryCode, string childEntryCode, string relationType = "ProductVariation")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parentEntryCode))
                {
                    return BadRequest("parentEntryCode is required.");
                }

                if (string.IsNullOrWhiteSpace(childEntryCode))
                {
                    return BadRequest("childEntryCode is required.");
                }

                var results = new List<object>();

                // Step 1: Check initial status
                var checkChild = CheckEntryArchived(childEntryCode);
                if (checkChild is OkObjectResult okChild)
                {
                    results.Add(new { step = "1. Check child entry status", result = okChild.Value });
                }

                // Step 2: Archive the child entry
                var archiveResult = ArchiveEntry(childEntryCode);
                if (archiveResult is OkObjectResult okArchive)
                {
                    results.Add(new { step = "2. Archive child entry", result = okArchive.Value });
                }

                // Step 3: Verify it's archived
                var checkAfterArchive = CheckEntryArchived(childEntryCode);
                if (checkAfterArchive is OkObjectResult okAfterArchive)
                {
                    results.Add(new { step = "3. Verify child entry is archived", result = okAfterArchive.Value });
                }

                // Step 4: Create relation to archived entry (THIS IS THE BUG)
                var createRelation = CreateRelationToArchived(parentEntryCode, childEntryCode, relationType);
                if (createRelation is OkObjectResult okRelation)
                {
                    results.Add(new { step = "4. Create relation to archived entry (BUG REPRODUCTION)", result = okRelation.Value });
                }

                // Step 5: List relations to see the problematic relation
                var listRelations = ListRelations(parentEntryCode);
                if (listRelations is OkObjectResult okList)
                {
                    results.Add(new { step = "5. List relations (shows invalid relation)", result = okList.Value });
                }

                return Ok(new
                {
                    success = true,
                    message = "Complete workflow executed - Issue replicated",
                    workflowResults = results,
                    summary = "This workflow demonstrates that archived entries can be linked via Service API, which is the bug described in Zendesk ticket 1796210"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Helper method to get entry relations.
        /// </summary>
        private List<EPiServer.ServiceApi.Commerce.Models.Catalog.EntryRelation> GetEntryRelations(string entryCode)
        {
            var relations = new List<EPiServer.ServiceApi.Commerce.Models.Catalog.EntryRelation>();

            if (string.IsNullOrWhiteSpace(entryCode))
            {
                return relations;
            }

            try
            {
                if (!_identityResolver.TryGetEntryId(entryCode, out var entryId))
                {
                    return relations;
                }

                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(entryLink))
                {
                    return relations;
                }

                // Get relations using IRelationRepository
                var entryRelations = _relationRepository.GetChildren<EPiServer.Commerce.Catalog.Linking.EntryRelation>(entryLink);
                
                // Get relation DTO to get relation types
                var catalogEntryResponseGroup = new CatalogRelationResponseGroup(CatalogRelationResponseGroup.ResponseGroup.CatalogEntry);
                var relationDto = _catalogSystem.GetCatalogRelationDto(0, 0, entryId, "", catalogEntryResponseGroup);
                
                foreach (var relation in entryRelations)
                {
                    if (_contentLoader.TryGet<EntryContentBase>(relation.Child, out var childEntry))
                    {
                        // Get relation type from DTO if available
                        var childEntryId = _referenceConverter.GetObjectId(relation.Child);
                        var relationType = "ProductVariation"; // Default
                        var groupName = relation.GroupName ?? string.Empty;
                        
                        if (relationDto?.CatalogEntryRelation != null)
                        {
                            var relationRow = relationDto.CatalogEntryRelation.FirstOrDefault(
                                x => x.ParentEntryId == entryId && x.ChildEntryId == childEntryId);
                            if (relationRow != null)
                            {
                                relationType = relationRow.RelationTypeId;
                                if (relationRow.IsGroupNameNull())
                                {
                                    groupName = string.Empty;
                                }
                                else
                                {
                                    groupName = relationRow.GroupName;
                                }
                            }
                        }
                        
                        relations.Add(new EPiServer.ServiceApi.Commerce.Models.Catalog.EntryRelation
                        {
                            ParentEntryCode = entryCode,
                            ChildEntryCode = childEntry.Code,
                            RelationType = relationType,
                            Quantity = relation.Quantity ?? 1m,
                            SortOrder = relation.SortOrder,
                            GroupName = groupName
                        });
                    }
                }
            }
            catch
            {
                // Ignore errors in helper method
            }

            return relations;
        }

        /// <summary>
        /// Helper method to check if an entry is archived.
        /// </summary>
        private bool CheckIfEntryArchived(string entryCode)
        {
            if (string.IsNullOrWhiteSpace(entryCode))
            {
                return false;
            }

            try
            {
                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(entryLink))
                {
                    return false;
                }

                var entryId = _referenceConverter.GetObjectId(entryLink);
                var archivedItems = _catalogArchive.GetArchivedItems();
                return archivedItems.Any(x => x.CatalogEntryId == entryId);
            }
            catch
            {
                return false;
            }
        }
    }
}

