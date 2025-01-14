using EPiServer.Commerce.Order;
using Mediachase.Commerce.Anonymous;

namespace Foundation.Custom
{
    // app.useMiddleware<CustomAnonymousIdMiddleware>();
    public class CustomAnonymousIdMiddleware
    {
        private readonly RequestDelegate _next;

        private const string ANONYMOUS_COOKIE_NAME = "EPiServer_Commerce_AnonymousId";

        public CustomAnonymousIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            HandleRequest(httpContext);
            await (_next?.Invoke(httpContext));
        }

        private void HandleRequest(HttpContext httpContext)
        {
            if ((httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated) && httpContext.Features.Get<IAnonymousIdFeature>() == null)
            {
                string text = httpContext.Request.Cookies["EPiServer_Commerce_AnonymousId"];
                if (string.IsNullOrWhiteSpace(text))
                {
                    var newGuid = Guid.NewGuid();
                    CookieOptions options = new CookieOptions
                    {
                        Expires = DateTime.Now.AddYears(1),
                        HttpOnly = true,
                        Secure = httpContext.Request.IsHttps
                    };
                    httpContext.Response.Cookies.Append("EPiServer_Commerce_AnonymousId", newGuid.ToString(), options);

                    var orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();

                    var cart = orderRepository.LoadCart<ICart>(newGuid, "Default");
                    if (cart != null)
                    {
                        // TODO:


                    }
                }

                httpContext.Features.Set((IAnonymousIdFeature?)new AnonymousIdFeature
                {
                    AnonymousId = text
                });
            }
        }
    }

    class AnonymousIdFeature : IAnonymousIdFeature
    {
        public string AnonymousId { get; set; }
    }
}
