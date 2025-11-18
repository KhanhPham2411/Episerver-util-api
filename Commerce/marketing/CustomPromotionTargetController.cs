using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.Marketing
{
    /// <summary>
    /// Step 1 – evaluate explicit variant targets. Sample: https://localhost:5000/util-api/custom-promotion-target/step1-variant
    /// </summary>
    [ApiController]
    [Route("util-api/custom-promotion-target")]
    public class CustomPromotionTargetController : ControllerBase
    {
        private readonly CollectionTargetEvaluator _collectionTargetEvaluator;

        public CustomPromotionTargetController(CollectionTargetEvaluator collectionTargetEvaluator)
        {
            _collectionTargetEvaluator = collectionTargetEvaluator;
        }

        /// <summary>
        /// Step 1 – evaluate explicit variant targets. Sample URL: https://localhost:5000/util-api/custom-promotion-target/step1-variant
        /// Sample JSON: { "codes": ["SKU-40707713","SKU-40707735"], "targetEntries": [2,3,5,6,8,9,11,12,14,15], "matchRecursive": true }
        /// </summary>
        [HttpPost("step1-variant")]
        public IActionResult EvaluateVariantTargets([FromBody] VariantTargetRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required.");
                }

                var codeList = ParseCodes(request.Codes);
                var targetList = ParseTargets(request.TargetEntries);

                if (!codeList.Any())
                {
                    return BadRequest("Please provide at least one line item code in the request body.");
                }

                if (!targetList.Any())
                {
                    return BadRequest("Please provide at least one target entry id in the request body.");
                }

                var lineItems = BuildLineItems(codeList);
                var matches = _collectionTargetEvaluator.GetApplicableCodes(lineItems, targetList, request.MatchRecursive);

                return Ok(new
                {
                    Step = "Variant targets",
                    RequestedCodes = codeList,
                    TargetIds = targetList.Select(x => x.ID),
                    MatchRecursive = request.MatchRecursive,
                    MatchingCodes = matches
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 2 – evaluate a category target. Sample URL: https://localhost:5000/util-api/custom-promotion-target/step2-category
        /// Sample JSON: { "codes": ["SKU-40707713","SKU-40707735"], "categoryId": 3, "matchRecursive": true }
        /// </summary>
        [HttpPost("step2-category")]
        public IActionResult EvaluateCategoryTarget([FromBody] CategoryTargetRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required.");
                }

                var codeList = ParseCodes(request.Codes);
                if (!codeList.Any())
                {
                    return BadRequest("Please provide at least one line item code in the request body.");
                }

                if (request.CategoryId <= 0)
                {
                    return BadRequest("Please provide a valid categoryId greater than zero.");
                }

                var lineItems = BuildLineItems(codeList);
                var targets = new List<ContentReference> { new ContentReference(request.CategoryId) };
                var matches = _collectionTargetEvaluator.GetApplicableCodes(lineItems, targets, request.MatchRecursive);

                return Ok(new
                {
                    Step = "Category target",
                    RequestedCodes = codeList,
                    CategoryId = request.CategoryId,
                    MatchRecursive = request.MatchRecursive,
                    MatchingCodes = matches
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Step 3 – compare explicit variant vs category targets. Sample URL: https://localhost:5000/util-api/custom-promotion-target/step3-compare
        /// Sample JSON: { "codes": ["SKU-40707713","SKU-40707735"], "targetEntries": [2,3,5,6,8,9,11,12,14,15], "categoryId": 3, "matchRecursive": true }
        /// </summary>
        [HttpPost("step3-compare")]
        public IActionResult CompareTargets([FromBody] CompareTargetsRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required.");
                }

                var codeList = ParseCodes(request.Codes);
                var variantTargets = ParseTargets(request.TargetEntries);

                if (!codeList.Any())
                {
                    return BadRequest("Please provide at least one line item code in the request body.");
                }

                if (!variantTargets.Any())
                {
                    return BadRequest("Please provide at least one target entry id in the request body.");
                }

                if (request.CategoryId <= 0)
                {
                    return BadRequest("Please provide a valid categoryId greater than zero.");
                }

                var lineItems = BuildLineItems(codeList);

                var variantStopwatch = Stopwatch.StartNew();
                var variantMatches = _collectionTargetEvaluator.GetApplicableCodes(lineItems, variantTargets, request.MatchRecursive);
                variantStopwatch.Stop();

                var categoryTargets = new List<ContentReference> { new ContentReference(request.CategoryId) };
                var categoryStopwatch = Stopwatch.StartNew();
                var categoryMatches = _collectionTargetEvaluator.GetApplicableCodes(lineItems, categoryTargets, request.MatchRecursive);
                categoryStopwatch.Stop();

                return Ok(new
                {
                    Step = "Compare targets",
                    RequestedCodes = codeList,
                    VariantTargets = variantTargets.Select(x => x.ID),
                    CategoryId = request.CategoryId,
                    MatchRecursive = request.MatchRecursive,
                    VariantMatches = variantMatches,
                    VariantEvaluationMilliseconds = variantStopwatch.ElapsedMilliseconds,
                    CategoryMatches = categoryMatches,
                    CategoryEvaluationMilliseconds = categoryStopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        private static IList<ILineItem> BuildLineItems(IEnumerable<string> codes)
        {
            return codes.Select(code => (ILineItem)new InMemoryLineItem
            {
                Code = code,
                Quantity = 1m,
                PlacedPrice = 0m
            }).ToList();
        }

        private static List<string> ParseCodes(IEnumerable<string> rawCodes)
        {
            if (rawCodes == null)
            {
                return new List<string>();
            }

            return rawCodes
                .SelectMany(code =>
                    (code ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(code => code.Trim())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<ContentReference> ParseTargets(IEnumerable<int> rawTargets)
        {
            if (rawTargets == null)
            {
                return new List<ContentReference>();
            }

            return rawTargets
                .Where(id => id > 0)
                .Distinct()
                .Select(id => new ContentReference(id))
                .ToList();
        }

        public class VariantTargetRequest
        {
            public List<string> Codes { get; set; } = new List<string>();
            public List<int> TargetEntries { get; set; } = new List<int>();
            public bool MatchRecursive { get; set; } = true;
        }

        public class CategoryTargetRequest
        {
            public List<string> Codes { get; set; } = new List<string>();
            public int CategoryId { get; set; }
            public bool MatchRecursive { get; set; } = true;
        }

        public class CompareTargetsRequest : VariantTargetRequest
        {
            public int CategoryId { get; set; }
        }
    }
}

