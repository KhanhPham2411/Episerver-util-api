using Microsoft.AspNetCore.Mvc;
using System;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using System.Linq;
using System.Data;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CatalogGroup
{
    /// <summary>
    /// API for creating catalogs in Optimizely Commerce.
    /// Sample usage: https://localhost:5000/util-api/custom-catalog/create-catalog?catalogName=TestCatalog
    /// </summary>
    [ApiController]
    [Route("util-api/custom-catalog")]
    public class CustomCatalogController : ControllerBase
    {
        /// <summary>
        /// Creates a catalog.
        /// Sample usage: https://localhost:5000/util-api/custom-catalog/create-catalog?catalogName=TestCatalog
        /// </summary>
        [HttpGet("create-catalog")]
        public IActionResult CreateCatalog([FromQuery] string catalogName)
        {
            try
            {
                var allCatalogsDto = CatalogContext.Current.GetCatalogDto();
                var existing = allCatalogsDto.Catalog.FirstOrDefault(x => x.Name.Equals(catalogName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    return Ok($"Catalog already exists: ID={existing.CatalogId}, Name={existing.Name}");
                }

                var now = DateTime.UtcNow;
                var catalogDto = new CatalogDto();
                catalogDto.Catalog.AddCatalogRow(
                    catalogName,
                    now.AddYears(-1),
                    now.AddYears(2),
                    "USD",
                    "kgs",
                    "en",
                    1, // MetaClassId
                    true, // IsPrimary
                    true, // IsActive
                    now,
                    now,
                    "SYSTEM", // CreatorId
                    "SYSTEM", // ModifierId
                    1, // SortOrder
                    "SYSTEM", // Owner
                    "cm",
                    Guid.NewGuid()
                );
                CatalogContext.Current.SaveCatalog(catalogDto);
                var createdId = catalogDto.Catalog[0].CatalogId;
                return Ok($"Catalog created: ID={createdId}, Name={catalogName}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
} 