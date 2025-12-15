using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Matching.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Foundation.Custom.EpiserverUtilApi.Commerce.CatalogGroup
{
    /// <summary>
    /// Quick GET endpoints to inspect how content routing resolves an SEO URL and why a GET action like LoadSizes can be selected.
    /// Sample usage: https://localhost:5000/util-api/custom-routing-debug/inspect-content-routing?url=/en/sample-product
    /// </summary>
    [ApiController]
    [Route("util-api/custom-routing-debug")]
    public class CustomRoutingDebugController : ControllerBase
    {
        private readonly IUrlResolver _urlResolver;
        private readonly IPatternMatcher _patternMatcher;

        public CustomRoutingDebugController()
        {
            _urlResolver = ServiceLocator.Current.GetInstance<IUrlResolver>();
            _patternMatcher = ServiceLocator.Current.GetInstance<IPatternMatcher>();
        }

        /// <summary>
        /// Resolve a content URL to see the routed content type and remaining path.
        /// Sample usage: https://localhost:5000/util-api/custom-routing-debug/inspect-content-routing?url=/en/sample-product
        /// </summary>
        [HttpGet("inspect-content-routing")]
        public IActionResult InspectContentRouting(string url = "/")
        {
            try
            {
                var routeData = _urlResolver.Route(
                    new UrlBuilder(url ?? "/"),
                    new RouteArguments
                    {
                        ExactMatch = false,
                        ContextMode = EPiServer.Web.ContextMode.Default
                    });

                return Ok(new
                {
                    inputUrl = url,
                    resolved = routeData?.Content != null,
                    contentLink = routeData?.Content?.ContentLink.ToString(),
                    contentType = routeData?.Content?.GetOriginalType().FullName,
                    remainingPath = routeData?.RemainingPath,
                    routeLanguage = routeData?.RouteLanguage
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Run the Optimizely pattern matcher to show what route values are produced for a template type and remaining path.
        /// (Variant focus) Sample usage: https://localhost:5000/util-api/custom-routing-debug/pattern-match?templateType=Foundation.Features.CatalogContent.Variation.VariationController&remainingPath=
        /// </summary>
        [HttpGet("pattern-match")]
        public IActionResult PatternMatch(string templateType = "Foundation.Features.CatalogContent.Variation.VariationController", string remainingPath = "")
        {
            try
            {
                var type = ResolveType(templateType);
                if (type == null)
                {
                    return BadRequest($"Template type '{templateType}' could not be found. Provide a fully qualified type name.");
                }

                var matches = _patternMatcher
                    .MatchPattern(
                        HttpContext,
                        type,
                        remainingPath ?? string.Empty,
                        new RouteValueDictionary(HttpContext.Request.RouteValues))
                    .Select(rv => rv.ToDictionary(kv => kv.Key, kv => kv.Value))
                    .ToList();

                return Ok(new
                {
                    templateType,
                    remainingPath,
                    matchCount = matches.Count,
                    matches
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// List public GET actions on a controller to spot competing candidates (e.g., variant Index vs other GETs).
        /// Sample usage: https://localhost:5000/util-api/custom-routing-debug/list-get-actions?controllerType=Foundation.Features.CatalogContent.Variation.VariationController
        /// </summary>
        [HttpGet("list-get-actions")]
        public IActionResult ListGetActions(string controllerType = "Foundation.Features.CatalogContent.Variation.VariationController")
        {
            try
            {
                var type = ResolveType(controllerType);
                if (type == null)
                {
                    return BadRequest($"Controller type '{controllerType}' could not be found. Provide a fully qualified type name.");
                }

                var actions = type
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m =>
                    {
                        var httpAttrs = m.GetCustomAttributes()
                            .OfType<HttpMethodAttribute>()
                            .ToList();

                        return httpAttrs.Count == 0 ||
                               httpAttrs.Any(a => a.HttpMethods.Contains("GET", StringComparer.OrdinalIgnoreCase));
                    })
                    .Select(m => new
                    {
                        action = m.Name,
                        parameters = m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}")
                    })
                    .ToList();

                return Ok(new
                {
                    controllerType,
                    actionCount = actions.Count,
                    actions
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Exception: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
            }
        }

        private static Type ResolveType(string nameOrFullName)
        {
            var t = Type.GetType(nameOrFullName, throwOnError: false);
            if (t != null)
            {
                return t;
            }

            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .FirstOrDefault(x =>
                    x.FullName.Equals(nameOrFullName, StringComparison.OrdinalIgnoreCase) ||
                    x.Name.Equals(nameOrFullName, StringComparison.OrdinalIgnoreCase));
        }
    }
}

