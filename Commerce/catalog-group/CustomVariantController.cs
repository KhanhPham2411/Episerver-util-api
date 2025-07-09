using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Security;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog;
using Foundation.Features.CatalogContent.Product;
using Foundation.Features.CatalogContent.Variation;
using EPiServer.DataAccess;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CatalogGroup
{
    /// <summary>
    /// API for creating variants (SKUs) in Optimizely Commerce.
    /// Sample usage: https://localhost:5000/util-api/custom-variant/create-variant?productName=TestProduct&variantName=TestVariant
    /// Optionally add &catalogName=TestCatalog to specify a catalog. If not provided, the first catalog under root is used.
    /// </summary>
    [ApiController]
    [Route("util-api/custom-variant")]
    public class CustomVariantController : ControllerBase
    {
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;

        public CustomVariantController()
        {
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
        }

        /// <summary>
        /// Creates a variant (SKU) under the specified product in the specified or first catalog.
        /// Sample usage: https://localhost:5000/util-api/custom-variant/create-variant?productName=TestProduct&variantName=TestVariant
        /// Optionally add &catalogName=TestCatalog to specify a catalog. If not provided, the first catalog under root is used.
        /// </summary>
        [HttpGet("create-variant")]
        public IActionResult CreateVariant(string productName, string variantName, string catalogName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName))
                {
                    return BadRequest("productName is required.");
                }
                if (string.IsNullOrWhiteSpace(variantName))
                {
                    return BadRequest("variantName is required.");
                }

                // Get the catalog root using ReferenceConverter
                var rootLink = _referenceConverter.GetRootLink();
                var catalogs = _contentRepository.GetChildren<CatalogContent>(rootLink);
                CatalogContent catalog = null;
                if (!string.IsNullOrWhiteSpace(catalogName))
                {
                    catalog = catalogs.FirstOrDefault(c => c.Name.Equals(catalogName, StringComparison.OrdinalIgnoreCase));
                    if (catalog == null)
                    {
                        return BadRequest($"Catalog '{catalogName}' not found.");
                    }
                }
                else
                {
                    catalog = catalogs.FirstOrDefault();
                    if (catalog == null)
                    {
                        return BadRequest("No catalogs found under root.");
                    }
                }

                // Efficiently get product by code
                var productLink = _referenceConverter.GetContentLink(productName, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(productLink))
                {
                    return BadRequest($"Product '{productName}' not found in catalog '{catalog.Name}'.");
                }
                var product = _contentRepository.Get<GenericProduct>(productLink);
                if (product == null || product.ParentLink.ID != catalog.ContentLink.ID)
                {
                    return BadRequest($"Product '{productName}' not found in catalog '{catalog.Name}'.");
                }

                // Efficiently check if variant exists by code
                var variantLink = _referenceConverter.GetContentLink(variantName, CatalogContentType.CatalogEntry);
                if (!ContentReference.IsNullOrEmpty(variantLink))
                {
                    var existing = _contentRepository.Get<GenericVariant>(variantLink);
                    if (existing != null && existing.ParentLink.ID == product.ContentLink.ID)
                    {
                        return Ok($"Variant already exists: Code={existing.Code}, Name={existing.Name}");
                    }
                }

                // Create and publish the variant
                var variant = _contentRepository.GetDefault<GenericVariant>(product.ContentLink);
                variant.Name = variantName;
                variant.Code = variantName.Replace(" ", "_");
                _contentRepository.Save(variant, SaveAction.Publish, AccessLevel.NoAccess);
                return Ok($"Variant created: Code={variant.Code}, Name={variant.Name}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Deletes a variant (SKU) by code. Example: https://localhost:5000/util-api/custom-variant/delete-variant?variantName=TestVariant
        /// </summary>
        [HttpGet("delete-variant")]
        public IActionResult DeleteVariant(string variantName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(variantName))
                {
                    return BadRequest("variantName is required.");
                }
                var variantLink = _referenceConverter.GetContentLink(variantName, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(variantLink))
                {
                    return NotFound($"Variant with code '{variantName}' not found.");
                }
                var variant = _contentRepository.Get<GenericVariant>(variantLink);
                if (variant == null)
                {
                    return NotFound($"Variant with code '{variantName}' not found.");
                }
                _contentRepository.Delete(variantLink, true, AccessLevel.NoAccess);
                return Ok($"Variant '{variantName}' deleted successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
} 