using EPiServer.Core.Routing.Pipeline;
using EPiServer.Core.Routing;
using System.Globalization;

namespace Foundation.Features.Home
{
    public class MinimalPartialRouter : IPartialRouter<HomePage, HomePage>
    {
        public MinimalPartialRouter()
        {
        }

        public object RoutePartial(Product content, UrlResolverContext segmentContext)
        {
            if (segmentContext.RemainingSegments.ToString().Contains("virtual")) {
                segmentContext.RemainingSegments = String.Empty.AsMemory();
            }

            return content;
        }

        public PartialRouteData GetPartialVirtualPath(Product content, UrlGeneratorContext urlGeneratorContext)
        {
            return new PartialRouteData
            {
                BasePathRoot = content.ContentLink,
                PartialVirtualPath = "virtual"
            };
        }
    }
}
