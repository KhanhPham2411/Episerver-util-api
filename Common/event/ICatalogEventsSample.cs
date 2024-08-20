using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog.Events;

    public class Startup
    {
        //...

        public void Configuration(IAppBuilder app)
        {
            var evt = ServiceLocator.Current.GetInstance<ICatalogEvents>();
            evt.CatalogUpdated += Evt_CatalogUpdated;
        }

        private void Evt_CatalogUpdated(object sender, CatalogEventArgs e)
        {
           ///e.CatalogChanges
        }
    }
