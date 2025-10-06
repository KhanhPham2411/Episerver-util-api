using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Security;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.Linking;
using Foundation.Features.CatalogContent.Product;
using EPiServer.DataAccess;
using EPiServer.Web.Routing;
using System.Collections.Generic;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CatalogGroup
{
    /// <summary>
    /// API for replicating and testing the primary category issue in Optimizely Commerce.
    /// This controller demonstrates the problem where ParentLink and primary relations can get out of sync.
    /// </summary>
    [ApiController]
    [Route("util-api/custom-primary-category")]
    public class CustomPrimaryCategoryController : ControllerBase
    {
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IRelationRepository _relationRepository;
        private readonly IContentLoader _contentLoader;
        private readonly UrlResolver _urlResolver;

        public CustomPrimaryCategoryController()
        {
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            _relationRepository = ServiceLocator.Current.GetInstance<IRelationRepository>();
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
        }

        /// <summary>
        /// Step 1: Create a test product with a specific primary category.
        /// Sample usage: https://localhost:5000/util-api/custom-primary-category/create-test-product?productName=TestProduct&categoryCode=mens
        /// </summary>
        [HttpGet("create-test-product")]
        public IActionResult CreateTestProduct(string productName, string categoryCode = "mens")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName))
                {
                    return BadRequest("productName is required.");
                }

                // Get the catalog root
                var rootLink = _referenceConverter.GetRootLink();
                var catalogs = _contentRepository.GetChildren<CatalogContent>(rootLink);
                var catalog = catalogs.FirstOrDefault();
                if (catalog == null)
                {
                    return BadRequest("No catalogs found under root.");
                }

                

                // Find the category node
                var categoryLink = _referenceConverter.GetContentLink(categoryCode, CatalogContentType.CatalogNode);
                if (ContentReference.IsNullOrEmpty(categoryLink))
                {
                    return BadRequest($"Category with code '{categoryCode}' not found.");
                }

                var category = _contentLoader.Get<NodeContent>(categoryLink);
                if (category == null)
                {
                    return BadRequest($"Category with code '{categoryCode}' not found.");
                }

                // Check if product already exists
                GenericProduct currentProduct = null;
                var productCode = productName.Replace(" ", "_").ToLower();
                var existingProductLink = _referenceConverter.GetContentLink(productCode, CatalogContentType.CatalogEntry);
                if (!ContentReference.IsNullOrEmpty(existingProductLink))
                {
                    var existingProduct = _contentLoader.Get<GenericProduct>(existingProductLink);
                    if (existingProduct != null)
                    {
                        //return Ok(new
                        //{
                        //    Message = "Product already exists",
                        //    ProductCode = existingProduct.Code,
                        //    ProductName = existingProduct.Name,
                        //    ParentLink = existingProduct.ParentLink.ToString(),
                        //    ProductUrl = _urlResolver.GetUrl(existingProduct.ContentLink),
                        //    Step = "1 - Product already exists, skipping creation"
                        //});
                        currentProduct = existingProduct;
                    }
                }
                
                if (currentProduct == null)
                {
                    // Create product with specific parent category
                    var product = _contentRepository.GetDefault<GenericProduct>(category.ContentLink);
                    product.Name = productName;
                    product.Code = productName.Replace(" ", "_").ToLower();
                    product.DisplayName = productName;

                    currentProduct = product;
                }

                var writableProduct = currentProduct.CreateWritableClone<GenericProduct>();

                writableProduct.ParentLink = categoryLink;
                _contentRepository.Save(writableProduct, SaveAction.Publish, AccessLevel.NoAccess);


                // Get the product URL
                var productUrl = _urlResolver.GetUrl(writableProduct.ContentLink);

                return Ok(new
                {
                    Message = "Test product created successfully",
                    ProductCode = writableProduct.Code,
                    ProductName = writableProduct.Name,
                    PrimaryCategory = category.Code,
                    ParentLink = writableProduct.ParentLink.ToString(),
                    ProductUrl = productUrl,
                    Step = "1 - Product created with primary category"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 2: Add a collection relation (non-primary) to the product.
        /// Sample usage: https://localhost:5000/util-api/custom-primary-category/add-collection-relation?productCode=testproduct&collectionCode=womens
        /// </summary>
        [HttpGet("add-collection-relation")]
        public IActionResult AddCollectionRelation(string productCode, string collectionCode = "womens")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productCode))
                {
                    return BadRequest("productCode is required.");
                }

                // Get the product
                var productLink = _referenceConverter.GetContentLink(productCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(productLink))
                {
                    return BadRequest($"Product with code '{productCode}' not found.");
                }

                var product = _contentLoader.Get<GenericProduct>(productLink);
                if (product == null)
                {
                    return BadRequest($"Product with code '{productCode}' not found.");
                }

                // Find or create collection category
                var collectionLink = _referenceConverter.GetContentLink(collectionCode, CatalogContentType.CatalogNode);
                if (ContentReference.IsNullOrEmpty(collectionLink))
                {
                    // Create collection category if it doesn't exist
                    var rootLink = _referenceConverter.GetRootLink();
                    var catalogs = _contentRepository.GetChildren<CatalogContent>(rootLink);
                    var catalog = catalogs.FirstOrDefault();
                    
                    var collection = _contentRepository.GetDefault<NodeContent>(catalog.ContentLink);
                    collection.Name = collectionCode;
                    collection.Code = collectionCode;
                    collection.DisplayName = collectionCode;
                    _contentRepository.Save(collection, SaveAction.Publish, AccessLevel.NoAccess);
                    collectionLink = collection.ContentLink;
                }

                // Add non-primary relation to collection
                var collectionRelation = new NodeEntryRelation
                {
                    IsPrimary = false,
                    SortOrder = 100,
                    Parent = collectionLink,
                    Child = product.ContentLink
                };

                _relationRepository.UpdateRelation(collectionRelation);

                // Get current product state
                var refreshedProduct = _contentLoader.Get<GenericProduct>(product.ContentLink);
                var productUrl = _urlResolver.GetUrl(refreshedProduct.ContentLink);

                return Ok(new
                {
                    Message = "Collection relation added successfully",
                    ProductCode = product.Code,
                    PrimaryCategory = refreshedProduct.ParentLink.ToString(),
                    CollectionCategory = collectionLink.ToString(),
                    ProductUrl = productUrl,
                    Step = "2 - Collection relation added (non-primary)"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 3: Change the primary category by updating ParentLink and saving.
        /// Sample usage: https://localhost:5000/util-api/custom-primary-category/change-primary-category?productCode=testproduct&newCategoryCode=jackets
        /// </summary>
        [HttpGet("change-primary-category")]
        public IActionResult ChangePrimaryCategory(string productCode, string newCategoryCode = "electronics")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productCode))
                {
                    return BadRequest("productCode is required.");
                }

                // Get the product
                var productLink = _referenceConverter.GetContentLink(productCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(productLink))
                {
                    return BadRequest($"Product with code '{productCode}' not found.");
                }

                var product = _contentLoader.Get<GenericProduct>(productLink);
                if (product == null)
                {
                    return BadRequest($"Product with code '{productCode}' not found.");
                }

                // Find the new category
                var newCategoryLink = _referenceConverter.GetContentLink(newCategoryCode, CatalogContentType.CatalogNode);
                if (ContentReference.IsNullOrEmpty(newCategoryLink))
                {
                    // Create new category if it doesn't exist
                    var rootLink = _referenceConverter.GetRootLink();
                    var catalogs = _contentRepository.GetChildren<CatalogContent>(rootLink);
                    var catalog = catalogs.FirstOrDefault();
                    
                    var newCategory = _contentRepository.GetDefault<NodeContent>(catalog.ContentLink);
                    newCategory.Name = newCategoryCode;
                    newCategory.Code = newCategoryCode;
                    newCategory.DisplayName = newCategoryCode;
                    _contentRepository.Save(newCategory, SaveAction.Publish, AccessLevel.NoAccess);
                    newCategoryLink = newCategory.ContentLink;
                }

                var oldParentLink = product.ParentLink;

                // Create writable clone and change ParentLink
                var writableProduct = product.CreateWritableClone<GenericProduct>();
                writableProduct.ParentLink = newCategoryLink;

                // Save the product with new ParentLink
                _contentRepository.Save(writableProduct, SaveAction.Publish, AccessLevel.NoAccess);

                // Get URLs before and after
                var oldUrl = _urlResolver.GetUrl(product.ContentLink);
                var newUrl = _urlResolver.GetUrl(writableProduct.ContentLink);

                return Ok(new
                {
                    Message = "Primary category changed successfully",
                    ProductCode = product.Code,
                    OldParentLink = oldParentLink.ToString(),
                    NewParentLink = writableProduct.ParentLink.ToString(),
                    OldUrl = oldUrl,
                    NewUrl = newUrl,
                    Step = "3 - Primary category changed via ParentLink"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 4: Demonstrate the issue - check if URL generation is correct after relation changes.
        /// Sample usage: https://localhost:5000/util-api/custom-primary-category/check-url-consistency?productCode=testproduct
        /// </summary>
        [HttpGet("check-url-consistency")]
        public IActionResult CheckUrlConsistency(string productCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productCode))
                {
                    return BadRequest("productCode is required.");
                }

                // Get the product
                var productLink = _referenceConverter.GetContentLink(productCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(productLink))
                {
                    return BadRequest($"Product with code '{productCode}' not found.");
                }

                var product = _contentLoader.Get<GenericProduct>(productLink);
                if (product == null)
                {
                    return BadRequest($"Product with code '{productCode}' not found.");
                }

                // Get all parent relations (categories) for this product
                var relations = _relationRepository.GetParents<NodeEntryRelation>(product.ContentLink);
                var primaryRelation = relations.FirstOrDefault(r => r.IsPrimary);
                var allRelations = relations.ToList();

                // Generate URL
                var productUrl = _urlResolver.GetUrl(product.ContentLink);

                // Check if URL contains the primary category
                var urlContainsPrimary = primaryRelation != null && productUrl.Contains(primaryRelation.Parent.ToString());

                return Ok(new
                {
                    Message = "URL consistency check completed",
                    ProductCode = product.Code,
                    ParentLink = product.ParentLink.ToString(),
                    PrimaryRelation = primaryRelation?.Parent.ToString() ?? "None",
                    AllRelations = allRelations.Select(r => new { 
                        Parent = r.Parent.ToString(), 
                        IsPrimary = r.IsPrimary,
                        SortOrder = r.SortOrder 
                    }).ToList(),
                    ProductUrl = productUrl,
                    UrlContainsPrimary = urlContainsPrimary,
                    IsConsistent = product.ParentLink.ToString() == primaryRelation?.Parent.ToString(),
                    Step = "4 - URL consistency check"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 5: Force refresh the product content to see if it fixes the issue.
        /// Sample usage: https://localhost:5000/util-api/custom-primary-category/force-refresh?productCode=testproduct
        /// </summary>
        [HttpGet("force-refresh")]
        public IActionResult ForceRefresh(string productCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productCode))
                {
                    return BadRequest("productCode is required.");
                }

                // Get the product
                var productLink = _referenceConverter.GetContentLink(productCode, CatalogContentType.CatalogEntry);
                if (ContentReference.IsNullOrEmpty(productLink))
                {
                    return BadRequest($"Product with code '{productCode}' not found.");
                }

                // Force refresh by getting fresh instance
                var refreshedProduct = _contentLoader.Get<GenericProduct>(productLink);
                
                // Get parent relations (categories)
                var relations = _relationRepository.GetParents<NodeEntryRelation>(refreshedProduct.ContentLink);
                var primaryRelation = relations.FirstOrDefault(r => r.IsPrimary);

                // Generate URL
                var productUrl = _urlResolver.GetUrl(refreshedProduct.ContentLink);

                return Ok(new
                {
                    Message = "Product refreshed successfully",
                    ProductCode = refreshedProduct.Code,
                    ParentLink = refreshedProduct.ParentLink.ToString(),
                    PrimaryRelation = primaryRelation?.Parent.ToString() ?? "None",
                    ProductUrl = productUrl,
                    IsConsistent = refreshedProduct.ParentLink.ToString() == primaryRelation?.Parent.ToString(),
                    Step = "5 - Product content refreshed"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Complete test: Run all steps in sequence to reproduce the issue.
        /// Sample usage: https://localhost:5000/util-api/custom-primary-category/run-complete-test?productName=TestProduct&originalCategory=mens&newCategory=electronics&collectionCode=womens
        /// </summary>
        [HttpGet("run-complete-test")]
        public IActionResult RunCompleteTest(string productName, string originalCategory = "mens", string newCategory = "electronics", string collectionCode = "womens")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName))
                {
                    return BadRequest("productName is required.");
                }

                var results = new List<object>();
                var productCode = productName.Replace(" ", "_").ToLower();

                // Step 1: Create product
                var step1 = CreateTestProduct(productName, originalCategory);
                if (step1 is OkObjectResult ok1)
                {
                    results.Add(ok1.Value);
                }

                // Step 2: Add collection relation
                var step2 = AddCollectionRelation(productCode, collectionCode);
                if (step2 is OkObjectResult ok2)
                {
                    results.Add(ok2.Value);
                }

                // Step 3: Change primary category
                var step3 = ChangePrimaryCategory(productCode, newCategory);
                if (step3 is OkObjectResult ok3)
                {
                    results.Add(ok3.Value);
                }

                // Step 4: Check consistency
                var step4 = CheckUrlConsistency(productCode);
                if (step4 is OkObjectResult ok4)
                {
                    results.Add(ok4.Value);
                }

                // Step 5: Force refresh
                var step5 = ForceRefresh(productCode);
                if (step5 is OkObjectResult ok5)
                {
                    results.Add(ok5.Value);
                }

                return Ok(new
                {
                    Message = "Complete test executed",
                    ProductCode = productCode,
                    Steps = results,
                    Summary = "This test reproduces the primary category issue where ParentLink and primary relations can get out of sync, causing incorrect URL generation until app restart."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Clean up: Delete test products and categories created during testing.
        /// Sample usage: https://localhost:5000/util-api/custom-primary-category/cleanup-test-data?productCode=testproduct
        /// </summary>
        [HttpGet("cleanup-test-data")]
        public IActionResult CleanupTestData(string productCode = "testproduct")
        {
            try
            {
                var deletedItems = new List<string>();

                // Delete product
                var productLink = _referenceConverter.GetContentLink(productCode, CatalogContentType.CatalogEntry);
                if (!ContentReference.IsNullOrEmpty(productLink))
                {
                    _contentRepository.Delete(productLink, true, AccessLevel.NoAccess);
                    deletedItems.Add($"Product: {productCode}");
                }

                // Delete test categories
                var testCategories = new[] { "mens", "electronics", "womens" };
                foreach (var categoryCode in testCategories)
                {
                    var categoryLink = _referenceConverter.GetContentLink(categoryCode, CatalogContentType.CatalogNode);
                    if (!ContentReference.IsNullOrEmpty(categoryLink))
                    {
                        _contentRepository.Delete(categoryLink, true, AccessLevel.NoAccess);
                        deletedItems.Add($"Category: {categoryCode}");
                    }
                }

                return Ok(new
                {
                    Message = "Test data cleanup completed",
                    DeletedItems = deletedItems,
                    Count = deletedItems.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }
    }
}
