using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceApi.Validation;
using ControllerBase = EPiServer.ServiceApi.Commerce.Controllers.Catalog.ControllerBase;
using AuthorizePermissionAttribute = EPiServer.ServiceApi.Configuration.AuthorizePermissionAttribute;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Managers;
using System;
using System.Linq;

namespace Foundation.Custom
{
    /// <summary>
    /// Helper endpoints to reproduce and observe the media asset sort-order behavior
    /// introduced with COM-19564 (Service API 7.3.0).
    /// Sample URLs (GET) - use real data from your database:
    /// - https://localhost:5000/util-api/custom-media-asset-sort/inspect?entryCode=P-22471487
    /// - https://localhost:5000/util-api/custom-media-asset-sort/seed-old?entryCode=P-22471487&mediaGuid=04520f43-f9db-472f-bfa3-dc4ba398ba3a&sortOrder=10&groupName=Image
    /// - https://localhost:5000/util-api/custom-media-asset-sort/simulate-new?entryCode=P-22471487&mediaGuid=04520f43-f9db-472f-bfa3-dc4ba398ba3a&sortOrder=10&groupName=Image
    /// Example: P-22471487 is "COTTON MERINO SWEATER" with 3 existing images (sort 0,1,2)
    /// </summary>
    [Route("util-api/custom-media-asset-sort")]
    [RequireHttpsOrClose]
    [ValidateReadOnlyMode(AllowedVerbs = HttpVerbs.Get)]
    [ExceptionHandling]
    [RequestLogging]
    // [Authorize(Policy = "ServiceApiAuthorizationPolicy")]
    public class CustomMediaAssetSortController : ControllerBase
    {
        private readonly IContentRepository _contentRepository;
        private readonly IPermanentLinkMapper _permanentLinkMapper;
        private readonly ReferenceConverter _referenceConverter;
        private readonly ICatalogSystem _catalogSystem;

        public CustomMediaAssetSortController(
            IContentRepository contentRepository,
            IPermanentLinkMapper permanentLinkMapper,
            ReferenceConverter referenceConverter,
            ICatalogSystem catalogSystem,
            IContentLoader contentLoader,
            IContentVersionRepository contentVersionRepository) : base(contentLoader, contentVersionRepository)
        {
            _contentRepository = contentRepository;
            _permanentLinkMapper = permanentLinkMapper;
            _referenceConverter = referenceConverter;
            _catalogSystem = catalogSystem;
        }

        /// <summary>
        /// GET: https://localhost:5000/util-api/custom-media-asset-sort/inspect?entryCode=P-22471487
        /// Lists current CommerceMedia relations (AssetLink, GroupName, SortOrder) for the entry.
        /// Example: P-22471487 has 3 images with sort orders 0, 1, 2
        /// </summary>
        [HttpGet]
        [Route("inspect")]
        //[AuthorizePermission("EPiServerServiceApi", "ReadAccess")]
        public IActionResult Inspect(string entryCode)
        {
            try
            {
                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(entryLink))
                {
                    return NotFound($"Entry with code '{entryCode}' not found.");
                }

                if (!_contentRepository.TryGet<EntryContentBase>(entryLink, out var entry))
                {
                    return NotFound($"Entry content for code '{entryCode}' not found.");
                }

                var assets = entry.CommerceMediaCollection
                    .Select(x => new
                    {
                        x.AssetLink,
                        x.GroupName,
                        x.SortOrder
                    })
                    .ToList();

                return Ok(new
                {
                    EntryCode = entryCode,
                    EntryLink = entryLink,
                    AssetCount = assets.Count,
                    Assets = assets
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// GET: https://localhost:5000/util-api/custom-media-asset-sort/seed-old?entryCode=P-22471487&mediaGuid=04520f43-f9db-472f-bfa3-dc4ba398ba3a&sortOrder=10&groupName=Image
        /// Simulates pre-7.3.0 (DTO-based) behavior: ALWAYS sets SortOrder (and GroupName) for the relation,
        /// updating existing rows if present. Run inspect before/after to see the change.
        /// </summary>
        [HttpGet]
        [Route("seed-old")]
        //[AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public IActionResult SeedOld(string entryCode, Guid mediaGuid, int sortOrder = 0, string groupName = "default")
        {
            try
            {
                var mediaLink = _permanentLinkMapper.Find(mediaGuid)?.ContentReference;
                if (ContentReference.IsNullOrEmpty(mediaLink))
                {
                    return NotFound($"Media content with guid '{mediaGuid}' not found.");
                }

                var entryDto = _catalogSystem.GetCatalogEntryDto(entryCode, new CatalogEntryResponseGroup(CatalogEntryResponseGroup.ResponseGroup.Assets));
                if (entryDto.CatalogEntry.Count == 0)
                {
                    return NotFound($"Catalog entry DTO for code '{entryCode}' not found.");
                }

                var assetRow = entryDto.CatalogItemAsset.FirstOrDefault(r => r.AssetKey.Equals(mediaGuid.ToString("D"), StringComparison.OrdinalIgnoreCase));
                if (assetRow == null)
                {
                    assetRow = entryDto.CatalogItemAsset.NewCatalogItemAssetRow();
                    assetRow.CatalogEntryId = entryDto.CatalogEntry.First().CatalogEntryId;
                    assetRow.CatalogNodeId = 0;
                    assetRow.AssetKey = mediaGuid.ToString("D");
                    entryDto.CatalogItemAsset.Rows.Add(assetRow);
                }

                assetRow.AssetType = "image";
                assetRow.GroupName = string.IsNullOrWhiteSpace(groupName) ? "default" : groupName;
                assetRow.SortOrder = sortOrder;

                _catalogSystem.SaveCatalogEntry(entryDto);

                return Ok(new
                {
                    Message = "Seed (old DTO behavior) executed. SortOrder always set/updated.",
                    EntryCode = entryCode,
                    MediaGuid = mediaGuid,
                    GroupName = assetRow.GroupName,
                    SortOrder = assetRow.SortOrder
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// GET: https://localhost:5000/util-api/custom-media-asset-sort/simulate-new?entryCode=P-22471487&mediaGuid=04520f43-f9db-472f-bfa3-dc4ba398ba3a&sortOrder=10&groupName=Image
        /// Simulates post-7.3.0 (Content API) behavior from COM-19564:
        /// - Adds the relation with SortOrder only when it does NOT already exist.
        /// - If the relation exists, SortOrder/GroupName are left unchanged (this is the bug).
        /// </summary>
        [HttpGet]
        [Route("simulate-new")]
        //[AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public IActionResult SimulateNew(string entryCode, Guid mediaGuid, int sortOrder = 0, string groupName = "default")
        {
            try
            {
                var entryLink = _referenceConverter.GetContentLink(entryCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(entryLink))
                {
                    return NotFound($"Entry with code '{entryCode}' not found.");
                }

                if (!_contentRepository.TryGet<EntryContentBase>(entryLink, out var entry))
                {
                    return NotFound($"Entry content for code '{entryCode}' not found.");
                }

                var mediaLink = _permanentLinkMapper.Find(mediaGuid)?.ContentReference;
                if (ContentReference.IsNullOrEmpty(mediaLink))
                {
                    return NotFound($"Media content with guid '{mediaGuid}' not found.");
                }

                var writeable = entry.CreateWritableClone<EntryContentBase>();
                var existing = writeable.CommerceMediaCollection.FirstOrDefault(x => x.AssetLink == mediaLink);

                if (existing == null)
                {
                    writeable.CommerceMediaCollection.Add(new CommerceMedia
                    {
                        AssetLink = mediaLink,
                        AssetType = entry.GetOriginalType().FullName.ToLowerInvariant(),
                        GroupName = string.IsNullOrWhiteSpace(groupName) ? "default" : groupName,
                        SortOrder = sortOrder
                    });

                    var saveAction = entry.Status == VersionStatus.Published ? SaveAction.Publish : SaveAction.Save;
                    _contentRepository.Save(writeable, saveAction | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);

                    return Ok(new
                    {
                        Message = "Added new relation (post-7.3.0 behavior). SortOrder applied for new relation.",
                        EntryCode = entryCode,
                        MediaGuid = mediaGuid,
                        GroupName = groupName,
                        SortOrder = sortOrder
                    });
                }

                // Existing relation -> NO update (mirrors COM-19564 behavior)
                return Ok(new
                {
                    Message = "Relation already existed. No SortOrder update performed (post-7.3.0 behavior).",
                    EntryCode = entryCode,
                    MediaGuid = mediaGuid,
                    ExistingGroupName = existing.GroupName,
                    ExistingSortOrder = existing.SortOrder
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }
    }
}

