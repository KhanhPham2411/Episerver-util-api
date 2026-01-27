using EPiServer.Commerce.Catalog;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Provider.Internal;
using Mediachase.Data.Provider;
using System.Diagnostics;
using EPiServer.DataAccess;
using EPiServer.Security;
using Foundation.Features.CatalogContent.Product;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CatalogGroup
{
    /// <summary>
    /// API for replicating the archive timeout issue when archiving large catalog folders.
    /// This controller helps test and diagnose transaction timeout issues when archiving nodes with many children.
    /// Sample usage: https://localhost:5000/util-api/custom-archive-timeout/get-node-statistics?nodeId=4
    /// </summary>
    [ApiController]
    [Route("util-api/custom-archive-timeout")]
    public class CustomArchiveTimeoutController : ControllerBase
    {
        private readonly ICatalogArchive _catalogArchive;
        private readonly IContentRepository _contentRepository;
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;
        private readonly ICatalogSystem _catalogSystem;
        private readonly CatalogContentMoveHandler _catalogContentMoveHandler;

        public CustomArchiveTimeoutController()
        {
            _catalogArchive = ServiceLocator.Current.GetInstance<ICatalogArchive>();
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            _catalogSystem = ServiceLocator.Current.GetInstance<ICatalogSystem>();
            _catalogContentMoveHandler = ServiceLocator.Current.GetInstance<CatalogContentMoveHandler>();
        }

        /// <summary>
        /// Step 1: Get statistics about a catalog node including total child count (recursive).
        /// This helps identify nodes that might cause timeout issues when archiving.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-timeout/get-node-statistics?nodeId=4
        /// </summary>
        [HttpGet("get-node-statistics")]
        public IActionResult GetNodeStatistics(int nodeId)
        {
            try
            {
                var nodeDto = _catalogSystem.GetCatalogNodeDto(nodeId);
                if (nodeDto.CatalogNode.Count == 0)
                {
                    return BadRequest(new { success = false, message = $"Node with ID {nodeId} not found" });
                }

                var nodeRow = nodeDto.CatalogNode[0];
                var catalogId = nodeRow.CatalogId;
                var parentNodeId = nodeRow.ParentNodeId;

                // Count direct children
                var childNodesDto = _catalogSystem.GetCatalogNodesDto(catalogId, nodeId);
                var directChildNodeCount = childNodesDto.CatalogNode.Count;

                // Count direct entries
                var relationDto = _catalogSystem.GetCatalogRelationDto(
                    0, nodeId, 0, string.Empty,
                    new CatalogRelationResponseGroup(CatalogRelationResponseGroup.ResponseGroup.NodeEntry));
                var directEntryCount = relationDto.NodeEntryRelation.Count;

                // Recursively count all descendants
                var totalDescendants = CountDescendantsRecursive(nodeId, catalogId);

                // Get ContentReference
                var contentLink = _referenceConverter.GetNodeContentLink(nodeId);
                var contentType = _referenceConverter.GetContentType(contentLink);
                var canLoadContent = _contentRepository.TryGet<IContent>(contentLink, out var content);

                return Ok(new
                {
                    success = true,
                    message = "Node statistics retrieved successfully",
                    nodeId = nodeId,
                    nodeName = nodeRow.Name,
                    catalogId = catalogId,
                    parentNodeId = parentNodeId,
                    directChildNodeCount = directChildNodeCount,
                    directEntryCount = directEntryCount,
                    totalDirectChildren = directChildNodeCount + directEntryCount,
                    totalDescendantNodes = totalDescendants.TotalNodes,
                    totalDescendantEntries = totalDescendants.TotalEntries,
                    totalDescendants = totalDescendants.TotalNodes + totalDescendants.TotalEntries,
                    contentLink = contentLink?.ToString(),
                    contentType = contentType.ToString(),
                    canLoadContent = canLoadContent,
                    contentName = content?.Name,
                    warning = totalDescendants.TotalNodes + totalDescendants.TotalEntries > 1000
                        ? "This node has many descendants. Archiving may cause transaction timeout."
                        : null
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 2: Get a list of nodes with the most children, sorted by total descendant count.
        /// This helps identify nodes that are most likely to cause timeout issues.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-timeout/get-nodes-with-many-children?top=10
        /// </summary>
        [HttpGet("get-nodes-with-many-children")]
        public IActionResult GetNodesWithManyChildren(int top = 10)
        {
            try
            {
                var allNodes = _catalogSystem.GetCatalogNodeDto(0).CatalogNode;
                var nodeStats = new List<object>();

                foreach (var nodeRow in allNodes)
                {
                    var nodeId = nodeRow.CatalogNodeId;
                    var catalogId = nodeRow.CatalogId;

                    // Count direct children
                    var childNodesDto = _catalogSystem.GetCatalogNodesDto(catalogId, nodeId);
                    var directChildNodeCount = childNodesDto.CatalogNode.Count;

                    var relationDto = _catalogSystem.GetCatalogRelationDto(
                        0, nodeId, 0, string.Empty,
                        new CatalogRelationResponseGroup(CatalogRelationResponseGroup.ResponseGroup.NodeEntry));
                    var directEntryCount = relationDto.NodeEntryRelation.Count;

                    // Get recursive count (limit depth to avoid performance issues)
                    var descendants = CountDescendantsRecursive(nodeId, catalogId, maxDepth: 3);

                    nodeStats.Add(new
                    {
                        nodeId = nodeId,
                        nodeName = nodeRow.Name,
                        catalogId = catalogId,
                        parentNodeId = nodeRow.ParentNodeId,
                        directChildNodeCount = directChildNodeCount,
                        directEntryCount = directEntryCount,
                        totalDescendantNodes = descendants.TotalNodes,
                        totalDescendantEntries = descendants.TotalEntries,
                        totalDescendants = descendants.TotalNodes + descendants.TotalEntries
                    });
                }

                var sortedNodes = nodeStats
                    .OrderByDescending(n => ((dynamic)n).totalDescendants)
                    .Take(top)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Top {top} nodes with most descendants",
                    nodes = sortedNodes,
                    totalNodesAnalyzed = allNodes.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 3: Archive a catalog node. This replicates the timeout issue when archiving nodes with many children.
        /// The operation is wrapped in a single transaction, which can timeout for large nodes.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-timeout/archive-node?nodeId=4
        /// </summary>
        [HttpGet("archive-node")]
        public IActionResult ArchiveNode(int nodeId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var contentLink = _referenceConverter.GetNodeContentLink(nodeId);
                if (ContentReference.IsNullOrEmpty(contentLink))
                {
                    return BadRequest(new { success = false, message = $"Node with ID {nodeId} not found" });
                }

                // Get statistics before archiving
                var statsBefore = GetNodeStatisticsInternal(nodeId);

                // Check if already archived
                var archivedItems = _catalogArchive.GetArchivedItems();
                var isAlreadyArchived = archivedItems.Any(x => x.CatalogNodeId == nodeId);

                if (isAlreadyArchived)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Node {nodeId} is already archived",
                        nodeId = nodeId,
                        alreadyArchived = true
                    });
                }

                // Attempt to archive (this is where the timeout issue occurs)
                string archiveError = null;
                bool archiveSuccess = false;
                Exception archiveException = null;

                try
                {
                    _catalogArchive.ArchiveContent(contentLink);
                    archiveSuccess = true;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("SqlTransaction has completed"))
                {
                    archiveError = "Transaction timeout: SqlTransaction has completed; it is no longer usable";
                    archiveException = ex;
                }
                catch (Exception ex)
                {
                    archiveError = ex.Message;
                    archiveException = ex;
                }

                stopwatch.Stop();

                // Get statistics after archiving
                var statsAfter = archiveSuccess ? GetNodeStatisticsInternal(nodeId) : null;

                // Check archive status
                var archivedItemsAfter = _catalogArchive.GetArchivedItems();
                var isInArchive = archivedItemsAfter.Any(x => x.CatalogNodeId == nodeId);
                var archivedItem = archivedItemsAfter.FirstOrDefault(x => x.CatalogNodeId == nodeId);

                dynamic statsBeforeDynamic = statsBefore;
                return Ok(new
                {
                    success = archiveSuccess,
                    message = archiveSuccess
                        ? $"Node {nodeId} archived successfully"
                        : $"Archive failed: {archiveError}",
                    nodeId = nodeId,
                    nodeName = statsBeforeDynamic?.NodeName,
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    elapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                    statisticsBefore = statsBefore,
                    statisticsAfter = statsAfter,
                    archiveSuccess = archiveSuccess,
                    archiveError = archiveError,
                    isInArchive = isInArchive,
                    archivedItem = archivedItem,
                    exceptionType = archiveException?.GetType().Name,
                    exceptionMessage = archiveException?.Message,
                    innerExceptionMessage = archiveException?.InnerException?.Message,
                    isTimeoutError = archiveError?.Contains("SqlTransaction has completed") ?? false
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 4: Test archive operation with transaction monitoring.
        /// This method provides detailed information about the archive process and transaction behavior.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-timeout/test-archive-with-monitoring?nodeId=4
        /// </summary>
        [HttpGet("test-archive-with-monitoring")]
        public IActionResult TestArchiveWithMonitoring(int nodeId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var contentLink = _referenceConverter.GetNodeContentLink(nodeId);
                if (ContentReference.IsNullOrEmpty(contentLink))
                {
                    return BadRequest(new { success = false, message = $"Node with ID {nodeId} not found" });
                }

                var statsBefore = GetNodeStatisticsInternal(nodeId);
                dynamic statsBeforeDynamic = statsBefore;
                var totalDescendants = statsBeforeDynamic?.TotalDescendants ?? 0;

                // Estimate time (rough calculation: ~0.1 seconds per item)
                var estimatedSeconds = totalDescendants * 0.1;
                var estimatedMinutes = estimatedSeconds / 60;

                var warnings = new List<string>();
                if (totalDescendants > 1000)
                {
                    warnings.Add($"Node has {totalDescendants} descendants. This may take {estimatedMinutes:F1} minutes.");
                }
                if (totalDescendants > 10000)
                {
                    warnings.Add("WARNING: This operation will likely timeout due to transaction duration exceeding 30 minutes.");
                }

                // Check if already archived
                var archivedItems = _catalogArchive.GetArchivedItems();
                var isAlreadyArchived = archivedItems.Any(x => x.CatalogNodeId == nodeId);

                if (isAlreadyArchived)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Node {nodeId} is already archived",
                        nodeId = nodeId,
                        alreadyArchived = true,
                        statistics = statsBefore
                    });
                }

                // Attempt archive with monitoring
                string archiveError = null;
                bool archiveSuccess = false;
                Exception archiveException = null;

                try
                {
                    _catalogArchive.ArchiveContent(contentLink);
                    archiveSuccess = true;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("SqlTransaction has completed"))
                {
                    archiveError = "Transaction timeout: SqlTransaction has completed; it is no longer usable";
                    archiveException = ex;
                }
                catch (Exception ex)
                {
                    archiveError = ex.Message;
                    archiveException = ex;
                }

                stopwatch.Stop();

                var statsAfter = archiveSuccess ? GetNodeStatisticsInternal(nodeId) : null;
                var archivedItemsAfter = _catalogArchive.GetArchivedItems();
                var isInArchive = archivedItemsAfter.Any(x => x.CatalogNodeId == nodeId);

                return Ok(new
                {
                    success = archiveSuccess,
                    message = archiveSuccess
                        ? $"Node {nodeId} archived successfully in {stopwatch.Elapsed.TotalSeconds:F1} seconds"
                        : $"Archive failed after {stopwatch.Elapsed.TotalSeconds:F1} seconds: {archiveError}",
                    nodeId = nodeId,
                    nodeName = statsBeforeDynamic?.NodeName,
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    elapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                    estimatedTimeSeconds = estimatedSeconds,
                    estimatedTimeMinutes = estimatedMinutes,
                    warnings = warnings,
                    statisticsBefore = statsBefore,
                    statisticsAfter = statsAfter,
                    archiveSuccess = archiveSuccess,
                    archiveError = archiveError,
                    isInArchive = isInArchive,
                    isTimeoutError = archiveError?.Contains("SqlTransaction has completed") ?? false,
                    exceptionType = archiveException?.GetType().Name,
                    exceptionMessage = archiveException?.Message,
                    innerExceptionMessage = archiveException?.InnerException?.Message,
                    recommendation = totalDescendants > 10000
                        ? "Consider splitting the archive operation into smaller batches or using a background job."
                        : null
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 5: Add multiple products to a catalog node for testing archive timeout issues.
        /// This endpoint creates the specified number of products under a node (resolved by node code) to help replicate the timeout scenario.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-timeout/add-products?nodeCode=womens-shirts&count=100
        /// </summary>
        [HttpGet("add-products")]
        public IActionResult AddProducts(string nodeCode, int count = 10, string productPrefix = "TestProduct")
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(nodeCode))
                {
                    return BadRequest(new { success = false, message = "nodeCode is required" });
                }

                if (count <= 0)
                {
                    return BadRequest(new { success = false, message = "Count must be greater than 0" });
                }

                if (count > 10000)
                {
                    return BadRequest(new { success = false, message = "Count cannot exceed 10,000 products per request" });
                }

                // Resolve node by code instead of numeric id
                var nodeContentLink = _referenceConverter.GetContentLink(nodeCode, CatalogContentType.CatalogNode);
                if (ContentReference.IsNullOrEmpty(nodeContentLink))
                {
                    return BadRequest(new { success = false, message = $"Node with code '{nodeCode}' not found" });
                }

                if (!_contentRepository.TryGet<NodeContent>(nodeContentLink, out var nodeContent))
                {
                    return BadRequest(new { success = false, message = $"Could not load node content for code '{nodeCode}'" });
                }

                // Get numeric node id for statistics / logging
                var nodeId = _referenceConverter.GetObjectId(nodeContentLink);

                var createdProducts = new List<object>();
                var failedCount = 0;
                var errorMessages = new List<string>();

                for (int i = 1; i <= count; i++)
                {
                    try
                    {
                        var productName = $"{productPrefix}_{nodeId}_{i}_{DateTime.Now:yyyyMMddHHmmss}";
                        var productCode = productName.Replace(" ", "_");

                        // Check if product already exists
                        var existingProductLink = _referenceConverter.GetContentLink(productCode, CatalogContentType.CatalogEntry);
                        if (!ContentReference.IsNullOrEmpty(existingProductLink))
                        {
                            var existing = _contentRepository.Get<GenericProduct>(existingProductLink);
                            if (existing != null)
                            {
                                createdProducts.Add(new
                                {
                                    productCode = existing.Code,
                                    productName = existing.Name,
                                    productId = existing.ContentLink.ID,
                                    status = "Already exists"
                                });
                                continue;
                            }
                        }

                        // Create new product
                        var product = _contentRepository.GetDefault<GenericProduct>(nodeContentLink);
                        product.Name = productName;
                        product.Code = productCode;
                        var savedLink = _contentRepository.Save(product, SaveAction.Publish, AccessLevel.NoAccess);

                        createdProducts.Add(new
                        {
                            productCode = productCode,
                            productName = productName,
                            productId = savedLink.ID,
                            status = "Created"
                        });
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        errorMessages.Add($"Product {i}: {ex.Message}");
                        if (errorMessages.Count > 10) // Limit error messages
                        {
                            break;
                        }
                    }
                }

                stopwatch.Stop();

                // Get updated statistics
                var statsAfter = GetNodeStatisticsInternal(nodeId);
                dynamic statsAfterDynamic = statsAfter;

                return Ok(new
                {
                    success = true,
                    message = $"Created {createdProducts.Count} products under node '{nodeCode}'",
                    nodeCode = nodeCode,
                    nodeId = nodeId,
                    nodeName = nodeContent.Name,
                    requestedCount = count,
                    createdCount = createdProducts.Count,
                    failedCount = failedCount,
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    elapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                    products = createdProducts.Take(100), // Limit to first 100 for response size
                    totalProductsCreated = createdProducts.Count,
                    errorMessages = errorMessages.Any() ? errorMessages : null,
                    statisticsAfter = statsAfter,
                    totalDescendantsAfter = statsAfterDynamic?.TotalDescendants ?? 0,
                    note = createdProducts.Count > 100 ? "Only first 100 products shown in response" : null
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 6: Get detailed information about the archive operation flow.
        /// This shows the code path that causes the transaction timeout issue.
        /// Sample usage: https://localhost:5000/util-api/custom-archive-timeout/get-archive-flow-info
        /// </summary>
        [HttpGet("get-archive-flow-info")]
        public IActionResult GetArchiveFlowInfo()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    message = "Archive operation flow information",
                    flow = new
                    {
                        step1 = "ArchiveStore.MoveToArchive() - Entry point",
                        step2 = "DefaultCatalogArchive.ArchiveContent() - Main archive logic",
                        step3 = "CatalogArchiveHandler.ArchiveContent() - Calls move handler",
                        step4 = "CatalogContentMoveHandler.Move() - Wraps in transaction",
                        step5 = "MoveWithTransaction() - Creates SINGLE TransactionScope",
                        step6 = "SetParentNodeForNode() - Sets parent for node",
                        step7 = "MoveCatalogNode() - RECURSIVE processing of ALL children",
                        issue = "All operations occur within a SINGLE transaction, causing timeout for large nodes"
                    },
                    problem = new
                    {
                        description = "When archiving a node with 24,000+ items, all operations occur in one transaction",
                        operations = "Approximately 168,000+ database operations (reads, updates, relation changes)",
                        duration = "Takes 30+ minutes",
                        timeout = "SQL Server transaction timeout (typically 30 minutes)",
                        error = "System.InvalidOperationException: This SqlTransaction has completed; it is no longer usable"
                    },
                    solution = new
                    {
                        recommendation1 = "Batch processing: Process items in batches (e.g., 100 per transaction)",
                        recommendation2 = "Background job: Move archive operations to background scheduled job",
                        recommendation3 = "Increase timeouts: Temporary workaround (not recommended for production)"
                    },
                    codeLocations = new
                    {
                        moveHandler = "EPiServer.Business.Commerce\\Catalog\\Provider\\Internal\\CatalogContentMoveHandler.cs",
                        moveWithTransaction = "Line 189-196: MoveWithTransaction() wraps everything in single transaction",
                        moveCatalogNode = "Line 234-325: MoveCatalogNode() recursively processes all children without batching",
                        archiveStore = "EPiServer.Commerce.Shell\\Rest\\ArchiveStore.cs",
                        archiveEntryPoint = "Line 386-402: MoveToArchive() entry point"
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        #region Helper Methods

        private (int TotalNodes, int TotalEntries) CountDescendantsRecursive(int nodeId, int catalogId, int currentDepth = 0, int maxDepth = 10)
        {
            if (currentDepth >= maxDepth)
            {
                return (0, 0);
            }

            var totalNodes = 0;
            var totalEntries = 0;

            try
            {
                // Count direct child nodes
                var childNodesDto = _catalogSystem.GetCatalogNodesDto(catalogId, nodeId);
                totalNodes = childNodesDto.CatalogNode.Count;

                // Count direct entries
                var relationDto = _catalogSystem.GetCatalogRelationDto(
                    0, nodeId, 0, string.Empty,
                    new CatalogRelationResponseGroup(CatalogRelationResponseGroup.ResponseGroup.NodeEntry));
                totalEntries = relationDto.NodeEntryRelation.Count;

                // Recursively count descendants of child nodes
                foreach (var childNode in childNodesDto.CatalogNode)
                {
                    var childDescendants = CountDescendantsRecursive(childNode.CatalogNodeId, catalogId, currentDepth + 1, maxDepth);
                    totalNodes += childDescendants.TotalNodes;
                    totalEntries += childDescendants.TotalEntries;
                }
            }
            catch
            {
                // If we can't count, return what we have so far
            }

            return (totalNodes, totalEntries);
        }

        private object GetNodeStatisticsInternal(int nodeId)
        {
            try
            {
                var nodeDto = _catalogSystem.GetCatalogNodeDto(nodeId);
                if (nodeDto.CatalogNode.Count == 0)
                {
                    return null;
                }

                var nodeRow = nodeDto.CatalogNode[0];
                var catalogId = nodeRow.CatalogId;

                var childNodesDto = _catalogSystem.GetCatalogNodesDto(catalogId, nodeId);
                var directChildNodeCount = childNodesDto.CatalogNode.Count;

                var relationDto = _catalogSystem.GetCatalogRelationDto(
                    0, nodeId, 0, string.Empty,
                    new CatalogRelationResponseGroup(CatalogRelationResponseGroup.ResponseGroup.NodeEntry));
                var directEntryCount = relationDto.NodeEntryRelation.Count;

                var descendants = CountDescendantsRecursive(nodeId, catalogId);

                return new
                {
                    NodeId = nodeId,
                    NodeName = nodeRow.Name,
                    CatalogId = catalogId,
                    ParentNodeId = nodeRow.ParentNodeId,
                    DirectChildNodeCount = directChildNodeCount,
                    DirectEntryCount = directEntryCount,
                    TotalDirectChildren = directChildNodeCount + directEntryCount,
                    TotalDescendantNodes = descendants.TotalNodes,
                    TotalDescendantEntries = descendants.TotalEntries,
                    TotalDescendants = descendants.TotalNodes + descendants.TotalEntries
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
