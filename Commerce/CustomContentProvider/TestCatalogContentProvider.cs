using EPiServer.Commerce.Catalog.Provider;

namespace Foundation.Custom.CustomContentProvider
{
    public class TestCatalogContentProvider : CatalogContentProvider
    {
        private string? providerKey;
        //public override ContentProviderCapabilities ProviderCapabilities => ContentProviderCapabilities.MultiLanguage;

        public override string ProviderKey => this.providerKey == null ? "NOT-PROVIDED" : this.providerKey;

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);

            this.providerKey = name;
        }
    }
}
