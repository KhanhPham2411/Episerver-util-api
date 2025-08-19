using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceApi.Validation;
using EPiServer.ServiceApi.Commerce.Controllers.Catalog;
using ControllerBase = EPiServer.ServiceApi.Commerce.Controllers.Catalog.ControllerBase;
using AuthorizePermissionAttribute = EPiServer.ServiceApi.Configuration.AuthorizePermissionAttribute;
using EPiServer.ServiceApi.Commerce;
using EPiServer.ServiceApi.Commerce.Models.Catalog;

using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Objects;
using System;
using System.Linq;
using EPiServer.ServiceApi.Commerce.Controllers.Catalog.Construction;
using Mediachase.Commerce.Catalog.Managers;

namespace Foundation.Custom
{
    /// <summary>
    /// Custom controller to replicate Node Entry Relation delete issue
    /// Demonstrates the problematic DELETE method vs working GET method
    /// </summary>
    [Route("util-api/custom-node-entry-relation")]
    [RequireHttpsOrClose]
    [ValidateReadOnlyMode(AllowedVerbs = HttpVerbs.Get)]
    [ExceptionHandling]
    [RequestLogging]
    // [Authorize(Policy = "ServiceApiAuthorizationPolicy")]
    public class CustomNodeEntryRelationController : ControllerBase
    {
        private readonly ICatalogSystem _catalogSystem;
        private readonly IdentityResolver _identityResolver;
        private readonly CatalogRelationResponseGroup _nodeEntryResponseGroup;
        private readonly NodeEntryRelationModelFactory _nodeEntryRelationModelFactory;

        public CustomNodeEntryRelationController(
            ICatalogSystem catalogSystem,
            IdentityResolver identityResolver,
            NodeEntryRelationModelFactory nodeEntryRelationModelFactory,
            IContentLoader contentLoader,
            IContentVersionRepository contentVersionRepository) : base(contentLoader, contentVersionRepository)
        {
            _catalogSystem = catalogSystem;
            _identityResolver = identityResolver;
            _nodeEntryRelationModelFactory = nodeEntryRelationModelFactory;
            _nodeEntryResponseGroup = new CatalogRelationResponseGroup(CatalogRelationResponseGroup.ResponseGroup.NodeEntry);
        }

        /// <summary>
        /// GET: https://localhost:5000/util-api/custom-node-entry-relation/entries/{entryCode}/nodeentryrelations
        /// Gets all node entry relations for a specific entry (working method)
        /// </summary>
        [Route("entries/{entryCode}/nodeentryrelations", Name = "CustomGetNodeEntryRelations")]
        [HttpGet]
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess")]
        public virtual IActionResult GetNodeEntryRelations(string entryCode)
        {
            try
            {
                if (!_identityResolver.TryGetEntryId(entryCode, out var entryId))
                {
                    return NotFound($"Entry with code '{entryCode}' not found");
                }

                var relationDto = _catalogSystem.GetCatalogRelationDto(entryId);
                if (relationDto == null || relationDto.NodeEntryRelation == null)
                {
                    return NotFound($"No relations found for entry '{entryCode}'");
                }

                var relations = relationDto.NodeEntryRelation
                    .Select(row => 
                    {
                        if (_identityResolver.TryGetNodeCode(row.CatalogNodeId, out var nodeCode))
                        {
                            return _nodeEntryRelationModelFactory.GetNodeEntryRelation(nodeCode, entryCode, row);
                        }
                        return null;
                    })
                    .Where(r => r != null)
                    .ToList();

                return Ok(new
                {
                    Message = "GET method - Working approach",
                    EntryCode = entryCode,
                    EntryId = entryId,
                    RelationsCount = relations.Count,
                    Relations = relations
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// GET: https://localhost:5000/util-api/custom-node-entry-relation/entries/{entryCode}/nodeentryrelations/{nodeCode}
        /// Gets a specific node entry relation (working method)
        /// </summary>
        [Route("entries/{entryCode}/nodeentryrelations/{nodeCode}", Name = "CustomGetNodeEntryRelation")]
        [HttpGet]
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess")]
        public virtual IActionResult GetNodeEntryRelation(string entryCode, string nodeCode)
        {
            try
            {
                if (!_identityResolver.TryGetEntryId(entryCode, out var entryId))
                {
                    return NotFound($"Entry with code '{entryCode}' not found");
                }

                if (!_identityResolver.TryGetNodeId(nodeCode, out var nodeId))
                {
                    return NotFound($"Node with code '{nodeCode}' not found");
                }

                // Working approach - get relations by entry ID first
                var relationDto = _catalogSystem.GetCatalogRelationDto(entryId);
                if (relationDto == null || relationDto.NodeEntryRelation == null)
                {
                    return NotFound($"No relations found for entry '{entryCode}'");
                }

                var row = relationDto.NodeEntryRelation.FirstOrDefault(r => r.CatalogNodeId == nodeId);
                if (row == null)
                {
                    return NotFound($"No relation found between entry '{entryCode}' and node '{nodeCode}'");
                }

                var relation = _nodeEntryRelationModelFactory.GetNodeEntryRelation(nodeCode, entryCode, row);

                return Ok(new
                {
                    Message = "GET method - Working approach",
                    EntryCode = entryCode,
                    EntryId = entryId,
                    NodeCode = nodeCode,
                    NodeId = nodeId,
                    Relation = relation,
                    DebugInfo = new
                    {
                        RetrievedByEntryId = true,
                        RelationsCount = relationDto.NodeEntryRelation.Count,
                        FoundRelation = row != null
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// DELETE: https://localhost:5000/util-api/custom-node-entry-relation/entries/{entryCode}/nodeentryrelations/{nodeCode}
        /// Deletes a node entry relation (PROBLEMATIC method - replicates the issue)
        /// </summary>
        [Route("entries/{entryCode}/nodeentryrelations/{nodeCode}", Name = "CustomDeleteNodeEntryRelation")]
        [HttpDelete]
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public virtual IActionResult DeleteNodeEntryRelation(string entryCode, string nodeCode)
        {
            try
            {
                if (!_identityResolver.TryGetEntryId(entryCode, out var entryId))
                {
                    return NotFound($"Entry with code '{entryCode}' not found");
                }

                if (!_identityResolver.TryGetNodeId(nodeCode, out var nodeId))
                {
                    return NotFound($"Node with code '{nodeCode}' not found");
                }

                var nodeDto = _catalogSystem.GetCatalogNodeDto(nodeId);
                if (nodeDto == null || nodeDto.CatalogNode == null || !nodeDto.CatalogNode.Any())
                {
                    return NotFound($"Node with ID '{nodeId}' not found");
                }

                var catalogId = nodeDto.CatalogNode.First().CatalogId;

                // PROBLEMATIC APPROACH - This is what causes the 404 error
                // Using wildcard parameters: catalogId=0, catalogEntryId=0
                var relationDto = _catalogSystem.GetCatalogRelationDto(0, nodeId, 0, "", _nodeEntryResponseGroup);
                if (relationDto == null || relationDto.NodeEntryRelation == null)
                {
                    return NotFound($"No relations found for node '{nodeCode}' (problematic approach)");
                }

                var workingDto = (CatalogRelationDto)relationDto.Copy();

                // This filtering often fails because the relation data was retrieved by node ID, not entry ID
                var row = workingDto.NodeEntryRelation.FirstOrDefault(
                    x => x.CatalogEntryId == entryId && x.CatalogNodeId == nodeId && x.CatalogId == catalogId);

                if (row == null)
                {
                    // This is where the 404 occurs in the real implementation
                    return NotFound(new
                    {
                        Message = "DELETE method - PROBLEMATIC approach failed",
                        EntryCode = entryCode,
                        EntryId = entryId,
                        NodeCode = nodeCode,
                        NodeId = nodeId,
                        CatalogId = catalogId,
                        DebugInfo = new
                        {
                            RetrievedByNodeId = true,
                            RelationsCount = relationDto.NodeEntryRelation.Count,
                            FoundRelation = false,
                            Problem = "Relation data retrieved by node ID, but filtering by entry ID failed",
                            RetrievedRelations = relationDto.NodeEntryRelation
                                .Select(r => new
                                {
                                    r.CatalogEntryId,
                                    r.CatalogNodeId,
                                    r.CatalogId,
                                    r.SortOrder,
                                    r.IsPrimary
                                }).ToList()
                        }
                    });
                }

                var model = _nodeEntryRelationModelFactory.GetNodeEntryRelation(nodeCode, entryCode, row);

                // In a real scenario, this would delete the relation
                // row.Delete();
                // _catalogSystem.SaveCatalogRelationDto(workingDto);

                return Ok(new
                {
                    Message = "DELETE method - PROBLEMATIC approach succeeded (unexpected)",
                    EntryCode = entryCode,
                    EntryId = entryId,
                    NodeCode = nodeCode,
                    NodeId = nodeId,
                    DeletedRelation = model,
                    DebugInfo = new
                    {
                        RetrievedByNodeId = true,
                        RelationsCount = relationDto.NodeEntryRelation.Count,
                        FoundRelation = true
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// DELETE: https://localhost:5000/util-api/custom-node-entry-relation/entries/{entryCode}/nodeentryrelations/{nodeCode}/fixed
        /// Deletes a node entry relation (FIXED method - uses the working approach)
        /// </summary>
        [Route("entries/{entryCode}/nodeentryrelations/{nodeCode}/fixed", Name = "CustomDeleteNodeEntryRelationFixed")]
        [HttpDelete]
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public virtual IActionResult DeleteNodeEntryRelationFixed(string entryCode, string nodeCode)
        {
            try
            {
                if (!_identityResolver.TryGetEntryId(entryCode, out var entryId))
                {
                    return NotFound($"Entry with code '{entryCode}' not found");
                }

                if (!_identityResolver.TryGetNodeId(nodeCode, out var nodeId))
                {
                    return NotFound($"Node with code '{nodeCode}' not found");
                }

                // FIXED APPROACH - Use the same method as GET (by entry ID first)
                var relationDto = _catalogSystem.GetCatalogRelationDto(entryId);
                if (relationDto == null || relationDto.NodeEntryRelation == null)
                {
                    return NotFound($"No relations found for entry '{entryCode}'");
                }

                var workingDto = (CatalogRelationDto)relationDto.Copy();

                // Simple filtering - find relation by node ID
                var row = workingDto.NodeEntryRelation.FirstOrDefault(r => r.CatalogNodeId == nodeId);

                if (row == null)
                {
                    return NotFound($"No relation found between entry '{entryCode}' and node '{nodeCode}'");
                }

                var model = _nodeEntryRelationModelFactory.GetNodeEntryRelation(nodeCode, entryCode, row);

                // In a real scenario, this would delete the relation
                // row.Delete();
                // _catalogSystem.SaveCatalogRelationDto(workingDto);

                return Ok(new
                {
                    Message = "DELETE method - FIXED approach (same as GET method)",
                    EntryCode = entryCode,
                    EntryId = entryId,
                    NodeCode = nodeCode,
                    NodeId = nodeId,
                    DeletedRelation = model,
                    DebugInfo = new
                    {
                        RetrievedByEntryId = true,
                        RelationsCount = relationDto.NodeEntryRelation.Count,
                        FoundRelation = true
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// GET: https://localhost:5000/util-api/custom-node-entry-relation/debug/entries/{entryCode}/nodeentryrelations/{nodeCode}
        /// Debug endpoint to compare both approaches side by side
        /// </summary>
        [Route("debug/entries/{entryCode}/nodeentryrelations/{nodeCode}", Name = "CustomDebugNodeEntryRelation")]
        [HttpGet]
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess")]
        public virtual IActionResult DebugNodeEntryRelation(string entryCode, string nodeCode)
        {
            try
            {
                if (!_identityResolver.TryGetEntryId(entryCode, out var entryId))
                {
                    return NotFound($"Entry with code '{entryCode}' not found");
                }

                if (!_identityResolver.TryGetNodeId(nodeCode, out var nodeId))
                {
                    return NotFound($"Node with code '{nodeCode}' not found");
                }

                var nodeDto = _catalogSystem.GetCatalogNodeDto(nodeId);
                var catalogId = nodeDto?.CatalogNode?.FirstOrDefault()?.CatalogId ?? 0;

                // Approach 1: Get by entry ID (working)
                var relationDtoByEntry = _catalogSystem.GetCatalogRelationDto(entryId);
                var relationByEntry = relationDtoByEntry?.NodeEntryRelation?.FirstOrDefault(r => r.CatalogNodeId == nodeId);

                // Approach 2: Get by node ID (problematic)
                var relationDtoByNode = _catalogSystem.GetCatalogRelationDto(0, nodeId, 0, "", _nodeEntryResponseGroup);
                var relationByNode = relationDtoByNode?.NodeEntryRelation?.FirstOrDefault(
                    x => x.CatalogEntryId == entryId && x.CatalogNodeId == nodeId && x.CatalogId == catalogId);

                return Ok(new
                {
                    Message = "Debug comparison of both approaches",
                    EntryCode = entryCode,
                    EntryId = entryId,
                    NodeCode = nodeCode,
                    NodeId = nodeId,
                    CatalogId = catalogId,
                    Comparison = new
                    {
                        Approach1_ByEntryId = new
                        {
                            Method = "GetCatalogRelationDto(entryId)",
                            Success = relationByEntry != null,
                            RelationsCount = relationDtoByEntry?.NodeEntryRelation?.Count ?? 0,
                            FoundRelation = relationByEntry != null,
                            Relation = relationByEntry != null ? new
                            {
                                relationByEntry.CatalogEntryId,
                                relationByEntry.CatalogNodeId,
                                relationByEntry.CatalogId,
                                relationByEntry.SortOrder,
                                relationByEntry.IsPrimary
                            } : null
                        },
                        Approach2_ByNodeId = new
                        {
                            Method = "GetCatalogRelationDto(0, nodeId, 0, \"\", responseGroup)",
                            Success = relationByNode != null,
                            RelationsCount = relationDtoByNode?.NodeEntryRelation?.Count ?? 0,
                            FoundRelation = relationByNode != null,
                            Relation = relationByNode != null ? new
                            {
                                relationByNode.CatalogEntryId,
                                relationByNode.CatalogNodeId,
                                relationByNode.CatalogId,
                                relationByNode.SortOrder,
                                relationByNode.IsPrimary
                            } : null
                        }
                    },
                    AllRelationsByEntry = relationDtoByEntry?.NodeEntryRelation?.Select(r => new
                    {
                        r.CatalogEntryId,
                        r.CatalogNodeId,
                        r.CatalogId,
                        r.SortOrder,
                        r.IsPrimary
                    }).ToList(),
                    AllRelationsByNode = relationDtoByNode?.NodeEntryRelation?.Select(r => new
                    {
                        r.CatalogEntryId,
                        r.CatalogNodeId,
                        r.CatalogId,
                        r.SortOrder,
                        r.IsPrimary
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
