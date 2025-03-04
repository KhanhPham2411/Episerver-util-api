using System.Web.Mvc;
using EPiServer.Web.Mvc;
using EPiServer.Core;
using EPiServer.Tracking.Commerce;
using EPiServer.Personalization.Commerce;
using LinqToTwitter;
using EPiServer.Personalization.Common;
using System.Collections.Generic;
using EPiServer.Personalization.Commerce.Tracking;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;

namespace Foundation.Features.Home
{
    public class HomeController : PageController<HomePage>
    {
        public HomeController()
        {
        }

        [CommerceTracking(TrackingType.Home)]
        public ActionResult Index(IContent currentPage)
        {
            string log = "";
            //TrackingResponseData result = _trackingService.Service.Track(commerceTrackingData, httpContext, content, Scope);
            var group = (ViewData["epi_Recommendations"] as IEnumerable<RecommendationGroup>) ?? Enumerable.Empty<RecommendationGroup>();
            var recommendations = group
                .Where(x => x.Area == "homeWidget")
                .SelectMany(x => x.Recommendations);
            log += $"recommendations: Count({recommendations.Count()})\n";
            log += string.Join(", ", recommendations.Select(r => r.ContentLink));


            return Content(log, "text/plain", Encoding.UTF8);


            // var scopes = personalizationClientConfiguration.GetScopes();
            // var scopeConfig = new List<ScopeConfiguration>();
            // foreach (var scope in scopes)
            // {
            //     var config = personalizationClientConfiguration.GetConfiguration(scope);
            //     scopeConfig.Add(config);
            // }
        }
    }
}
