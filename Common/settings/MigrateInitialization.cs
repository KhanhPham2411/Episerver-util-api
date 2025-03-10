using EPiServer.Framework.Initialization;
using EPiServer.Framework;
using EPiServer.Commerce.Internal.Migration;
using EPiServer.ServiceLocation;

namespace Foundation.Custom
{
    [ModuleDependency(typeof(EPiServer.Shell.UI.InitializationModule))]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class MigrateInitialization : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
        }

        public void Initialize(InitializationEngine context)
        {
            var manager = context.Locate.Advanced.GetInstance<MigrationManager>();
            if (manager.SiteNeedsToBeMigrated())
            {
                manager.Migrate();
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
