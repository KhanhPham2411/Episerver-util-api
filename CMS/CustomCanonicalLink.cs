public static class HtmlHelperExtensions
{
    public static IHtmlContent CanonicalLink(this IHtmlHelper html)
    {
        return html.CanonicalLink(null, null, null);
    }

    public static IHtmlContent CanonicalLink(this IHtmlHelper html, ContentReference contentLink, string language, string action)
    {
        var services = html.ViewContext.HttpContext.RequestServices;
        var routeHelper = services.GetRequiredService<IContentRouteHelper>();
        var urlResolver = services.GetRequiredService<IUrlResolver>();
        var contentLoader = services.GetRequiredService<IContentLoader>();
        var languageSettings = services.GetRequiredService<IContentLanguageSettingsHandler>();

        contentLink ??= routeHelper.ContentLink;
        language ??= routeHelper.LanguageID;
        action ??= html.ViewContext.HttpContext.GetRouteValue(RoutingConstants.ActionKey) as string;

        if (ContentReference.IsNullOrEmpty(contentLink))
        {
            return HtmlString.Empty;
        }

        // If the content is loaded due to a fallback or replacement settings,
        // then the URLs should be to the original content
        if (language is not null)
        {
            var loaderOptions = new LoaderOptions 
            {
                LanguageLoaderOption.FallbackWithMaster(CultureInfo.GetCultureInfo(language))
            };

            if (contentLoader.TryGet(contentLink, loaderOptions, out IContent content) &&
                content is ILocale localizable &&
                languageSettings.MatchLanguageSettings(content, language).FallbackOrReplacement())
            {
                language = localizable.Language.Name;
            }
        }

        var contentUrl = urlResolver.GetUrl(
            contentLink, 
            language,
            new VirtualPathArguments
            {
                ForceCanonical = true,
                ForceAbsolute = true,
                Action = action
            });

        if (string.IsNullOrEmpty(contentUrl))
        {
            return HtmlString.Empty;
        }

        return new TagBuilder("link")
        {
            TagRenderMode = TagRenderMode.StartTag,
            Attributes =
            {
                { "rel", "canonical" },
                { "href", contentUrl }
            }
        };
    }
}
