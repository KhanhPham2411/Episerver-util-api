using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Core;
using EPiServer.Commerce.Catalog.ContentTypes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.Custom.EpiserverUtilApi.Find
{
    /// <summary>
    /// API controller to demonstrate string search with special characters in Optimizely Find.
    /// Demonstrates the difference between MatchContained (for collections) and Match (for string properties).
    /// Sample usage: https://localhost:5000/util-api/custom-string-search/test-matchcontained-error?searchWord=P/D
    /// </summary>
    [ApiController]
    [Route("util-api/custom-string-search")]
    public class CustomStringSearchController : ControllerBase
    {
        private readonly IClient _client;

        public CustomStringSearchController(IClient client)
        {
            _client = client;
        }

        private string GetDisplayName(IContent content)
        {
            // Try to get DisplayName from common content types
            if (content is EntryContentBase entry)
            {
                return entry.DisplayName ?? entry.Name;
            }
            // For other content types, just return Name
            return content.Name;
        }

        /// <summary>
        /// Step 1: Demonstrates the ERROR - trying to use MatchContained on string properties.
        /// MatchContained is designed for collections (IEnumerable&lt;T&gt;), not direct string properties.
        /// This will cause a compilation error or runtime exception.
        /// Sample usage: https://localhost:5000/util-api/custom-string-search/test-matchcontained-error?searchWord=P/D
        /// </summary>
        [HttpGet("test-matchcontained-error")]
        public IActionResult TestMatchContainedError([FromQuery] string searchWord = "P/D")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchWord))
                {
                    return BadRequest(new { error = "searchWord parameter is required." });
                }

                var response = new
                {
                    title = "Testing MatchContained on String Properties (INCORRECT)",
                    searchWord = searchWord,
                    error = "MatchContained() cannot be used directly on string properties.",
                    explanation = new
                    {
                        matchContainedSignature = "MatchContained<T>(this IEnumerable<T> value, Expression<Func<T, string>> fieldSelector, string valueToMatch)",
                        issue = "MatchContained requires an IEnumerable<T> as the first parameter and an Expression<Func<T, string>> fieldSelector as the second parameter.",
                        solution = "For string properties, use .Match() or .MatchCaseInsensitive() instead."
                    },
                    correctEndpoints = new[]
                    {
                        "/util-api/custom-string-search/test-match-correct",
                        "/util-api/custom-string-search/test-match-case-insensitive"
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Exception occurred",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Step 2: Demonstrates the CORRECT approach - using Match() for string properties with special characters.
        /// Match() uses the non-analyzed $$string field, preserving special characters exactly.
        /// Sample usage: https://localhost:5000/util-api/custom-string-search/test-match-correct?searchWord=P/D
        /// Sample usage with apostrophe: https://localhost:5000/util-api/custom-string-search/test-match-correct?searchWord=L'amour
        /// </summary>
        [HttpGet("test-match-correct")]
        public IActionResult TestMatchCorrect([FromQuery] string searchWord = "P/D")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchWord))
                {
                    return BadRequest(new { error = "searchWord parameter is required." });
                }

                // CORRECT: Use .Match() for string properties
                // Note: IContent only has Name property, not DisplayName
                var query = _client.Search<IContent>()
                    .Filter(x => 
                        x.Name.Match(searchWord)
                        // Note: If you have custom properties like ItemNumber, HFBName, add them here:
                        // | x.ItemNumber.Match(searchWord)
                        // | x.HFBName.Match(searchWord)
                    );

                var searchResult = query.GetContentResult();
                var variants = searchResult.ToList();

                var results = variants.Take(20).Select(v => new
                {
                    displayName = GetDisplayName(v),
                    name = v.Name ?? "N/A",
                    contentLink = v.ContentLink.ToString(),
                    contentType = v.GetType().Name
                }).ToList();

                var response = new
                {
                    title = "Testing Match() on String Properties (CORRECT)",
                    searchWord = searchWord,
                    description = "Using .Match() which searches against the non-analyzed $$string field. This preserves special characters like '/', ''' (apostrophe), etc. exactly as they are.",
                    totalResults = variants.Count,
                    results = results,
                    hasMoreResults = variants.Count > 20,
                    moreResultsCount = variants.Count > 20 ? variants.Count - 20 : 0,
                    notes = variants.Count == 0 ? new[]
                    {
                        ".Match() performs exact matching. Make sure:",
                        "1. The search word matches exactly (case-sensitive)",
                        "2. The data exists in the index",
                        "3. Try using .MatchCaseInsensitive() for case-insensitive matching"
                    } : null,
                    howItWorks = new
                    {
                        method = ".Match() uses TermFilter against the non-analyzed $$string field suffix.",
                        specialCharacterHandling = new
                        {
                            example1 = "'P/D' is searched as 'P/D' (not tokenized into 'P' and 'D')",
                            example2 = "'L'amour' is searched as 'L'amour' (not tokenized into 'L' and 'amour')"
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Exception occurred",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Step 3: Demonstrates case-insensitive exact matching using MatchCaseInsensitive().
        /// This also uses the non-analyzed $$string field but performs case-insensitive comparison.
        /// Sample usage: https://localhost:5000/util-api/custom-string-search/test-match-case-insensitive?searchWord=p/d
        /// </summary>
        [HttpGet("test-match-case-insensitive")]
        public IActionResult TestMatchCaseInsensitive([FromQuery] string searchWord = "P/D")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchWord))
                {
                    return BadRequest(new { error = "searchWord parameter is required." });
                }

                // CORRECT: Use .MatchCaseInsensitive() for case-insensitive exact matching
                var query = _client.Search<IContent>()
                    .Filter(x => 
                        x.Name.MatchCaseInsensitive(searchWord)
                        // Note: If you have custom properties like ItemNumber, HFBName, add them here:
                        // | x.ItemNumber.MatchCaseInsensitive(searchWord)
                        // | x.HFBName.MatchCaseInsensitive(searchWord)
                    );

                var searchResult = query.GetContentResult();
                var variants = searchResult.ToList();

                var results = variants.Take(20).Select(v => new
                {
                    displayName = GetDisplayName(v),
                    name = v.Name ?? "N/A",
                    contentLink = v.ContentLink.ToString(),
                    contentType = v.GetType().Name
                }).ToList();

                var response = new
                {
                    title = "Testing MatchCaseInsensitive() on String Properties",
                    searchWord = searchWord,
                    description = "Using .MatchCaseInsensitive() which searches against the non-analyzed $$string field with case-insensitive matching (converts to lowercase).",
                    totalResults = variants.Count,
                    results = results,
                    hasMoreResults = variants.Count > 20,
                    moreResultsCount = variants.Count > 20 ? variants.Count - 20 : 0,
                    differenceFromMatch = new
                    {
                        explanation = ".MatchCaseInsensitive() converts the search term to lowercase and searches against the .lowercase field variant, allowing case-insensitive matching while still preserving special characters exactly."
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Exception occurred",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Step 4: Demonstrates the difference between full-text search (.For()) and filter (.Match()).
        /// Full-text search tokenizes special characters, while Match() preserves them exactly.
        /// Sample usage: https://localhost:5000/util-api/custom-string-search/compare-fulltext-vs-filter?searchWord=P/D
        /// </summary>
        [HttpGet("compare-fulltext-vs-filter")]
        public IActionResult CompareFullTextVsFilter([FromQuery] string searchWord = "P/D")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchWord))
                {
                    return BadRequest(new { error = "searchWord parameter is required." });
                }

                // Full-text search using .For() - tokenizes special characters
                var fullTextQuery = _client.Search<IContent>()
                    .For(searchWord)
                    .InField(x => x.Name, 10.0)
                    .Take(20);

                var fullTextResults = fullTextQuery.GetContentResult().ToList();
                var fullTextVariants = fullTextResults.Take(10).Select(v => new
                {
                    displayName = GetDisplayName(v),
                    name = v.Name ?? "N/A",
                    contentType = v.GetType().Name
                }).ToList();

                // Filter using .Match() - preserves special characters exactly
                var filterQuery = _client.Search<IContent>()
                    .Filter(x => 
                        x.Name.Match(searchWord)
                    )
                    .Take(20);

                var filterResults = filterQuery.GetContentResult().ToList();
                var filterVariants = filterResults.Take(10).Select(v => new
                {
                    displayName = GetDisplayName(v),
                    name = v.Name ?? "N/A",
                    contentType = v.GetType().Name
                }).ToList();

                var response = new
                {
                    title = "Comparing Full-Text Search vs Filter with Special Characters",
                    searchWord = searchWord,
                    fullTextSearch = new
                    {
                        description = "This uses analyzed fields which tokenize on special characters.",
                        explanation = $"Searching for '{searchWord}' will be tokenized into separate words.",
                        totalResults = fullTextResults.Count,
                        results = fullTextVariants,
                        characteristics = new[]
                        {
                            "Tokenizes 'P/D' into ['P', 'D']",
                            "Finds documents containing 'P' AND/OR 'D'",
                            "May return irrelevant results"
                        }
                    },
                    filterSearch = new
                    {
                        description = "This uses non-analyzed $$string field which preserves special characters exactly.",
                        explanation = $"Searching for '{searchWord}' will match only exact occurrences.",
                        totalResults = filterResults.Count,
                        results = filterVariants,
                        characteristics = new[]
                        {
                            "Preserves 'P/D' as exact string 'P/D'",
                            "Finds only documents with exact 'P/D'",
                            "Returns only exact matches"
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Exception occurred",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Step 5: Demonstrates the recommended approach using filter for exact matching.
        /// This ensures only exact matches are returned with special characters preserved.
        /// Note: Combining with .For() for relevance scoring requires calling .For() first, then .Filter().
        /// Sample usage: https://localhost:5000/util-api/custom-string-search/combined-approach?searchWord=P/D
        /// </summary>
        [HttpGet("combined-approach")]
        public IActionResult CombinedApproach([FromQuery] string searchWord = "P/D")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchWord))
                {
                    return BadRequest(new { error = "searchWord parameter is required." });
                }

                // Use filter for exact matching (preserves special characters)
                var query = _client.Search<IContent>()
                    .Filter(x => 
                        x.Name.Match(searchWord)
                        // Note: If you have custom properties like ItemNumber, HFBName, add them here:
                        // | x.ItemNumber.Match(searchWord)
                        // | x.HFBName.Match(searchWord)
                    )
                    .Take(20);

                var searchResult = query.GetContentResult();
                var variants = searchResult.ToList();

                var results = variants.Select(v => new
                {
                    displayName = GetDisplayName(v),
                    name = v.Name ?? "N/A",
                    contentLink = v.ContentLink.ToString(),
                    contentType = v.GetType().Name
                }).ToList();

                var response = new
                {
                    title = "Recommended Approach: Filter for Exact Matching",
                    searchWord = searchWord,
                    description = "This approach uses .Match() filter to ensure only exact matches are returned. Special characters like '/', ''' (apostrophe) are preserved exactly.",
                    totalResults = variants.Count,
                    results = results,
                    benefits = new[]
                    {
                        "Only exact matches are returned (special characters preserved)",
                        "'P/D' matches only 'P/D' (not 'P' and 'D' separately)",
                        "'L'amour' matches only 'L'amour' (not 'L' and 'amour' separately)"
                    },
                    optionalRelevanceScoring = new
                    {
                        note = "If you need relevance scoring, you can call .For() first, then .Filter():",
                        exampleCode = new[]
                        {
                            "var query = _client.Search<IContent>()",
                            "    .For(searchWord)",
                            "    .InField(x => x.Name, 500.0)",
                            "    .Filter(x => x.Name.Match(searchWord));"
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = "Exception occurred",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
