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
using EPiServer.DataAccess;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CatalogGroup
{
    /// <summary>
    /// API for creating products (SKUs) and variants in Optimizely Commerce.
    /// Sample usage: https://localhost:5000/util-api/custom-product/create-product?productName=TestProduct
    /// </summary>
    [ApiController]
    [Route("util-api/custom-product")]
    public class CustomProductController : ControllerBase
    {
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;

        public CustomProductController()
        {
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
        }

        /// <summary>
        /// Creates a product (SKU) in the specified or first catalog.
        /// Sample usage: https://localhost:5000/util-api/custom-product/create-product?productName=TestProduct
        /// Optionally add &catalogName=TestCatalog to specify a catalog. If not provided, the first catalog under root is used.
        /// </summary>
        [HttpGet("create-product")]
        public IActionResult CreateProduct(string productName, string catalogName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName))
                {
                    return BadRequest("productName is required.");
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

                // Efficiently check if product exists by code
                var productLink = _referenceConverter.GetContentLink(productName, CatalogContentType.CatalogEntry);
                if (!ContentReference.IsNullOrEmpty(productLink))
                {
                    var existing = _contentRepository.Get<GenericProduct>(productLink);
                    if (existing != null && existing.ParentLink.ID == catalog.ContentLink.ID)
                    {
                        return Ok($"Product already exists: Code={existing.Code}, Name={existing.Name}");
                    }
                }

                // Create and publish the product
                var product = _contentRepository.GetDefault<GenericProduct>(catalog.ContentLink);
                product.Name = productName;
                product.Code = productName.Replace(" ", "_");
                _contentRepository.Save(product, SaveAction.Publish, AccessLevel.NoAccess);
                return Ok($"Product created: Code={product.Code}, Name={product.Name}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
} 