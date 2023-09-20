using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Mediachase.Commerce.Catalog.Events;
using Mediachase.Commerce.Catalog.Loggers;
using Microsoft.Extensions.DependencyInjection;

namespace Foundation.Custom
{
    public class CustomCatalogLogger : CatalogLogger
    {
        [Obsolete]
        public CustomCatalogLogger()
        {
        }

        public CustomCatalogLogger(IPrincipalAccessor principal, IHttpContextAccessor httpContextAccessor)
            : base(principal, httpContextAccessor)
        {
        }
        public override void EntryUpdated(object source, EntryEventArgs args)
        {
            base.EntryUpdated(source, args);

            Console.WriteLine("EntryUpdated");
        }
    }

    [ModuleDependency(typeof(InitializationModule))]//, typeof(SetupBootstrapRenderer))]
    public class Initialize : IConfigurableModule
    {
        void IConfigurableModule.ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<CatalogEventListenerBase, CustomCatalogLogger>();
        }

        void IInitializableModule.Initialize(InitializationEngine context)
        {
        }

        void IInitializableModule.Uninitialize(InitializationEngine context)
        {
        }
    }
}
