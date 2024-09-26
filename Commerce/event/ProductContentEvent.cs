using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Initialization;

[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
public class EPiServerChangeEventInitialization : IInitializableModule
{
    private IContentRepository _contentRepository;
    private ILogger<EPiServerChangeEventInitialization> _logger;

    public void Initialize(InitializationEngine context)
    {
        _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        _logger = ServiceLocator.Current.GetInstance<ILogger<EPiServerChangeEventInitialization>>();

        var events = ServiceLocator.Current.GetInstance<IContentEvents>();

        events.PublishedContent += Events_PublishedContent;
    }

    public void Uninitialize(InitializationEngine context)
    {
        var events = ServiceLocator.Current.GetInstance<IContentEvents>();
        events.PublishedContent -= Events_PublishedContent;
    }

    private void Events_PublishedContent(object sender, ContentEventArgs e)
    {
        if (e.Content is ProductContent product)
        {
            ValidateCommerceMedia(product);
        }
    }

    private void ValidateCommerceMedia(ProductContent content)
    {
        var writableClone = content.CreateWritableClone<EntryContentBase>();

        var toDelete = new List<InRiverGenericMedia>();

        _logger.LogDebug("Checking for asset duplicates after product {Code} update", content.Code);

        foreach (var productMedia in writableClone.CommerceMediaCollection)
        {
            if (ContentReference.IsNullOrEmpty(productMedia.AssetLink))
                continue;

            if (!_contentRepository.TryGet<InRiverGenericMedia>(productMedia.AssetLink, out var inRiverGenericMedia))
                continue;

            _logger.LogTrace("Checking for duplicates of asset with entityId {EntityId} linked to product {Code}", inRiverGenericMedia.EntityId, content.Code);

            var containingFolder = _contentRepository.Get<ContentFolder>(inRiverGenericMedia.ParentLink);
            var allMedia = _contentRepository.GetChildren<InRiverGenericMedia>(containingFolder.ContentLink);

            var duplicatesByEntityId = allMedia.Where(m => m.EntityId == inRiverGenericMedia.EntityId).ToArray();

            if (duplicatesByEntityId.Length == 1) continue; // no duplicates

            var duplicatesToDelete = duplicatesByEntityId.Except(new[] { inRiverGenericMedia });

            foreach (var duplicateMedia in duplicatesToDelete)
            {
                var references = _contentRepository.GetReferencesToContent(duplicateMedia.ContentLink, false);

                if (references.All(r => r.OwnerID != containingFolder.ContentLink))
                {
                    toDelete.Add(duplicateMedia);
                }
            }
        }

        if (!toDelete.Any()) return;

        _logger.LogInformation("Deleting {Count} duplicates found for assets linked to product {Code}", toDelete.Count, content.Code);

        foreach (var duplicateToDelete in toDelete)
            _contentRepository.Delete(duplicateToDelete.ContentLink, true, AccessLevel.NoAccess);
    }
}
