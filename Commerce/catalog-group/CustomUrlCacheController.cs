using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Internal;
using EPiServer.Framework.Cache;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CatalogGroup
{
    /// <summary>
    /// API for testing and reproducing URL cache language collision issues in Commerce catalog URLs.
    /// Sample usage: https://localhost:5000/util-api/custom-url-cache/clear-cache
    /// </summary>
    [ApiController]
    [Route("util-api/custom-url-cache")]
    public class CustomUrlCacheController : ControllerBase
    {
        private readonly IUrlResolver _urlResolver;
        private readonly IContentUrlCache _contentUrlCache;
        private readonly IContentLoader _contentLoader;
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly ISynchronizedObjectInstanceCache _objectCache;

        public CustomUrlCacheController()
        {
            _urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            _contentUrlCache = ServiceLocator.Current.GetInstance<IContentUrlCache>();
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            _objectCache = ServiceLocator.Current.GetInstance<ISynchronizedObjectInstanceCache>();
        }

        /// <summary>
        /// Clears the Optimizely URL cache to start fresh testing.
        /// Sample usage: https://localhost:5000/util-api/custom-url-cache/clear-cache
        /// </summary>
        [HttpGet("clear-cache")]
        public IActionResult ClearCache()
        {
            try
            {
                // Clear the URL cache by removing the master key
                _objectCache.Remove("ep:url:m");
                return Ok(new
                {
                    message = "URL cache cleared successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Generates a catalog URL WITHOUT explicit language (bug scenario - can cause cache collisions).
        /// Sample usage: https://localhost:5000/util-api/custom-url-cache/generate-url-without-language?contentCode=PRODUCT123
        /// </summary>
        [HttpGet("generate-url-without-language")]
        public IActionResult GenerateUrlWithoutLanguage(string contentCode = null)
        {
            try
            {
                ContentReference contentLink = GetCatalogContentLink(contentCode);
                if (ContentReference.IsNullOrEmpty(contentLink))
                {
                    return BadRequest($"Catalog content not found. Provide a valid contentCode parameter, or use the first available catalog node/entry.");
                }

                // Generate URL WITHOUT explicit language - this is the bug scenario
                var url = _urlResolver.GetUrl(contentLink);
                var content = _contentLoader.Get<CatalogContentBase>(contentLink);
                string code = GetContentCode(content);

                return Ok(new
                {
                    message = "URL generated WITHOUT explicit language (bug scenario)",
                    contentCode = code,
                    contentLink = contentLink.ToString(),
                    generatedUrl = url,
                    currentLanguage = ContentLanguage.PreferredCulture?.Name ?? "null",
                    warning = "This URL may be cached with a key that doesn't include language, causing collisions across languages"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Generates a catalog URL WITH explicit language (correct scenario - prevents cache collisions).
        /// Sample usage: https://localhost:5000/util-api/custom-url-cache/generate-url-with-language?contentCode=PRODUCT123&language=en
        /// </summary>
        [HttpGet("generate-url-with-language")]
        public IActionResult GenerateUrlWithLanguage(string contentCode = null, string language = null)
        {
            try
            {
                ContentReference contentLink = GetCatalogContentLink(contentCode);
                if (ContentReference.IsNullOrEmpty(contentLink))
                {
                    return BadRequest($"Catalog content not found. Provide a valid contentCode parameter, or use the first available catalog node/entry.");
                }

                // Use provided language or current preferred culture
                var lang = !string.IsNullOrWhiteSpace(language) 
                    ? language 
                    : ContentLanguage.PreferredCulture?.Name ?? "en";

                // Generate URL WITH explicit language - this is the correct scenario
                var url = _urlResolver.GetUrl(contentLink, lang);
                var content = _contentLoader.Get<CatalogContentBase>(contentLink);
                string code = GetContentCode(content);

                return Ok(new
                {
                    message = "URL generated WITH explicit language (correct scenario)",
                    contentCode = code,
                    contentLink = contentLink.ToString(),
                    generatedUrl = url,
                    explicitLanguage = lang,
                    note = "This URL is cached with language in the key, preventing collisions across languages"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reproduces the full URL cache language collision scenario.
        /// Step 1: Clear cache, Step 2: Generate URL in language A without explicit language, Step 3: Generate URL in language B without explicit language.
        /// Sample usage: https://localhost:5000/util-api/custom-url-cache/reproduce-collision?contentCode=PRODUCT123&language1=ja-jp&language2=en
        /// </summary>
        [HttpGet("reproduce-collision")]
        public IActionResult ReproduceCollision(string contentCode = null, string language1 = "ja-jp", string language2 = "en")
        {
            try
            {
                ContentReference contentLink = GetCatalogContentLink(contentCode);
                if (ContentReference.IsNullOrEmpty(contentLink))
                {
                    return BadRequest($"Catalog content not found. Provide a valid contentCode parameter, or use the first available catalog node/entry.");
                }

                var results = new List<object>();

                // Step 1: Clear cache
                _objectCache.Remove("ep:url:m");
                results.Add(new
                {
                    step = 1,
                    action = "Clear URL cache",
                    timestamp = DateTime.UtcNow
                });

                // Step 2: Generate URL in language1 WITHOUT explicit language
                var originalLanguage = ContentLanguage.PreferredCulture;
                try
                {
                    SetLanguage(language1);
                    var url1 = _urlResolver.GetUrl(contentLink);
                    results.Add(new
                    {
                        step = 2,
                        action = $"Generate URL in {language1} WITHOUT explicit language",
                        currentLanguage = language1,
                        generatedUrl = url1,
                        warning = "This URL may be cached without language in the key"
                    });
                }
                finally
                {
                    if (originalLanguage != null)
                    {
                        SetLanguage(originalLanguage.Name);
                    }
                }

                // Step 3: Generate URL in language2 WITHOUT explicit language
                try
                {
                    SetLanguage(language2);
                    var url2 = _urlResolver.GetUrl(contentLink);
                    results.Add(new
                    {
                        step = 3,
                        action = $"Generate URL in {language2} WITHOUT explicit language",
                        currentLanguage = language2,
                        generatedUrl = url2,
                        issue = "If url2 matches url1, this indicates a cache collision - the URL from language1 was reused for language2"
                    });
                }
                finally
                {
                    if (originalLanguage != null)
                    {
                        SetLanguage(originalLanguage.Name);
                    }
                }

                // Step 4: Generate URL in language2 WITH explicit language (correct way)
                try
                {
                    SetLanguage(language2);
                    var url2Correct = _urlResolver.GetUrl(contentLink, language2);
                    results.Add(new
                    {
                        step = 4,
                        action = $"Generate URL in {language2} WITH explicit language (correct way)",
                        currentLanguage = language2,
                        explicitLanguage = language2,
                        generatedUrl = url2Correct,
                        note = "This should produce the correct URL for language2"
                    });
                }
                finally
                {
                    if (originalLanguage != null)
                    {
                        SetLanguage(originalLanguage.Name);
                    }
                }

                return Ok(new
                {
                    message = "URL cache collision reproduction scenario completed",
                    contentLink = contentLink.ToString(),
                    scenario = $"Tested languages: {language1} -> {language2}",
                    steps = results,
                    conclusion = "If step 3 shows the same URL as step 2, this confirms the cache collision bug. Step 4 shows the correct fix."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Compares URL generation with and without explicit language for the same content in different languages.
        /// Sample usage: https://localhost:5000/util-api/custom-url-cache/compare-url-generation?contentCode=PRODUCT123
        /// </summary>
        [HttpGet("compare-url-generation")]
        public IActionResult CompareUrlGeneration(string contentCode = null)
        {
            try
            {
                ContentReference contentLink = GetCatalogContentLink(contentCode);
                if (ContentReference.IsNullOrEmpty(contentLink))
                {
                    return BadRequest($"Catalog content not found. Provide a valid contentCode parameter, or use the first available catalog node/entry.");
                }

                var content = _contentLoader.Get<CatalogContentBase>(contentLink);
                var availableLanguages = _contentRepository.GetLanguageBranches<CatalogContentBase>(contentLink)
                    .Select(l => l.Language.Name)
                    .ToList();

                if (!availableLanguages.Any())
                {
                    availableLanguages.Add(ContentLanguage.PreferredCulture?.Name ?? "en");
                }

                var comparison = new List<object>();
                var originalLanguage = ContentLanguage.PreferredCulture;

                foreach (var lang in availableLanguages.Take(3)) // Test up to 3 languages
                {
                    try
                    {
                        SetLanguage(lang);

                        // Without explicit language
                        var urlWithoutLang = _urlResolver.GetUrl(contentLink);

                        // With explicit language
                        var urlWithLang = _urlResolver.GetUrl(contentLink, lang);

                        comparison.Add(new
                        {
                            language = lang,
                            urlWithoutExplicitLanguage = urlWithoutLang,
                            urlWithExplicitLanguage = urlWithLang,
                            match = urlWithoutLang == urlWithLang,
                            warning = urlWithoutLang == urlWithLang 
                                ? "URLs match - this is expected if cache hasn't been polluted" 
                                : "URLs differ - this may indicate cache collision if wrong language appears"
                        });
                    }
                    finally
                    {
                        if (originalLanguage != null)
                        {
                            SetLanguage(originalLanguage.Name);
                        }
                    }
                }

                return Ok(new
                {
                    message = "URL generation comparison completed",
                    contentCode = GetContentCode(content),
                    contentLink = contentLink.ToString(),
                    availableLanguages = availableLanguages,
                    comparison = comparison,
                    recommendation = "Always pass explicit language when generating catalog URLs to prevent cache collisions"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tests URL generation with null language context to reproduce the cache collision bug.
        /// This simulates scenarios where language context might be lost (background jobs, async operations, etc.).
        /// Sample usage: https://localhost:5000/util-api/custom-url-cache/test-null-language-context?contentCode=mens
        /// </summary>
        [HttpGet("test-null-language-context")]
        public IActionResult TestNullLanguageContext(string contentCode = null)
        {
            try
            {
                ContentReference contentLink = GetCatalogContentLink(contentCode);
                if (ContentReference.IsNullOrEmpty(contentLink))
                {
                    return BadRequest($"Catalog content not found. Provide a valid contentCode parameter, or use the first available catalog node/entry.");
                }

                var results = new List<object>();

                // Clear cache first
                _objectCache.Remove("ep:url:m");

                // Test 1: Generate URL with explicit language (should always work correctly)
                var urlWithLang = _urlResolver.GetUrl(contentLink, "fr");
                results.Add(new
                {
                    test = "With explicit language (fr)",
                    url = urlWithLang,
                    note = "This should always work correctly as language is in cache key"
                });

                // Test 2: Generate URL without explicit language in current context
                var urlWithoutLang = _urlResolver.GetUrl(contentLink);
                var currentLang = ContentLanguage.PreferredCulture?.Name ?? "null";
                results.Add(new
                {
                    test = $"Without explicit language (current context: {currentLang})",
                    url = urlWithoutLang,
                    currentLanguage = currentLang,
                    note = "This uses current language context - cache key may or may not include language depending on UrlGeneratorContext.Language"
                });

                // Test 3: Try to generate URL in a way that might have null language context
                // This is tricky because GetUrl() will use current context if language is not provided
                // The actual bug happens when UrlGeneratorContext.Language is null in the cache key calculation
                var urlWithDifferentLang = _urlResolver.GetUrl(contentLink, "sv");
                results.Add(new
                {
                    test = "With explicit language (sv) - different from fr",
                    url = urlWithDifferentLang,
                    note = "This should be different from fr URL if language-specific routing is configured"
                });

                // Test 4: Generate again without explicit language to see if cache is used
                var urlWithoutLang2 = _urlResolver.GetUrl(contentLink);
                results.Add(new
                {
                    test = "Without explicit language (second call)",
                    url = urlWithoutLang2,
                    note = "If this matches the first 'without language' call, cache is working. If it differs, there might be context issues."
                });

                return Ok(new
                {
                    message = "Null language context test completed",
                    contentLink = contentLink.ToString(),
                    results = results,
                    explanation = "The cache collision bug occurs when UrlGeneratorContext.Language is null in the cache key. " +
                                 "This typically happens in scenarios where language context is lost (background jobs, async operations, " +
                                 "or when ContentLanguage.PreferredCulture is not properly set). " +
                                 "In normal web requests, the language context is usually available, so the bug may not manifest."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets information about available catalog content for testing.
        /// Sample usage: https://localhost:5000/util-api/custom-url-cache/list-catalog-content
        /// </summary>
        [HttpGet("list-catalog-content")]
        public IActionResult ListCatalogContent(int maxItems = 10)
        {
            try
            {
                var rootLink = _referenceConverter.GetRootLink();
                var catalogs = _contentLoader.GetChildren<CatalogContent>(rootLink).ToList();

                if (!catalogs.Any())
                {
                    return Ok(new
                    {
                        message = "No catalogs found",
                        catalogs = new List<object>()
                    });
                }

                var catalog = catalogs.First();
                var nodes = _contentLoader.GetChildren<NodeContent>(catalog.ContentLink)
                    .Take(maxItems)
                    .Select(n => new
                    {
                        code = n.Code,
                        name = n.Name,
                        contentLink = n.ContentLink.ToString(),
                        contentType = n.GetType().Name,
                        languages = _contentRepository.GetLanguageBranches<NodeContent>(n.ContentLink)
                            .Select(l => l.Language.Name)
                            .ToList()
                    })
                    .ToList();

                var entries = _contentLoader.GetChildren<EntryContentBase>(catalog.ContentLink)
                    .Take(maxItems)
                    .Select(e => new
                    {
                        code = e.Code,
                        name = e.Name,
                        contentLink = e.ContentLink.ToString(),
                        contentType = e.GetType().Name,
                        languages = _contentRepository.GetLanguageBranches<EntryContentBase>(e.ContentLink)
                            .Select(l => l.Language.Name)
                            .ToList()
                    })
                    .ToList();

                return Ok(new
                {
                    message = "Catalog content available for testing",
                    catalog = new
                    {
                        name = catalog.Name,
                        contentLink = catalog.ContentLink.ToString()
                    },
                    nodes = nodes,
                    entries = entries,
                    note = "Use the 'code' value in other endpoints as the contentCode parameter"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        private ContentReference GetCatalogContentLink(string contentCode)
        {
            if (!string.IsNullOrWhiteSpace(contentCode))
            {
                // Try as catalog entry first
                var entryLink = _referenceConverter.GetContentLink(contentCode, CatalogContentType.CatalogEntry);
                if (!ContentReference.IsNullOrEmpty(entryLink))
                {
                    return entryLink;
                }

                // Try as catalog node
                var nodeLink = _referenceConverter.GetContentLink(contentCode, CatalogContentType.CatalogNode);
                if (!ContentReference.IsNullOrEmpty(nodeLink))
                {
                    return nodeLink;
                }
            }

            // Fallback: get first available catalog content
            var rootLink = _referenceConverter.GetRootLink();
            var catalogs = _contentLoader.GetChildren<CatalogContent>(rootLink).ToList();
            if (!catalogs.Any())
            {
                return ContentReference.EmptyReference;
            }

            var catalog = catalogs.First();
            
            // Try to get a node first
            var nodes = _contentLoader.GetChildren<NodeContent>(catalog.ContentLink).ToList();
            if (nodes.Any())
            {
                return nodes.First().ContentLink;
            }

            // Fallback to entry
            var entries = _contentLoader.GetChildren<EntryContentBase>(catalog.ContentLink).ToList();
            if (entries.Any())
            {
                return entries.First().ContentLink;
            }

            return ContentReference.EmptyReference;
        }

        private string GetContentCode(CatalogContentBase content)
        {
            if (content == null)
                return null;

            if (content is EntryContentBase entry)
                return entry.Code;

            if (content is NodeContent node)
                return node.Code;

            return null;
        }

        private void SetLanguage(string languageName)
        {
            try
            {
                var culture = new CultureInfo(languageName);
                var updateLanguage = ServiceLocator.Current.GetInstance<IUpdateCurrentLanguage>();
                updateLanguage.SetRoutedContent(null, languageName);
                
                var cultureContext = ServiceLocator.Current.GetInstance<ICurrentCultureContext>();
                cultureContext.CurrentCulture = culture;
                cultureContext.CurrentUICulture = culture;
            }
            catch
            {
                // If language setting fails, continue with current language
            }
        }
    }
}

