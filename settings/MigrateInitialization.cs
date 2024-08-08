using EPiServer.ServiceLocation;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.Framework;
using EPiServer.Commerce.Internal.Migration;

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
            var manager = ServiceLocator.Current.GetInstance<MigrationManager>();
            manager.Migrate();
        }

		public void Uninitialize(InitializationEngine context)
		{
			
		}
	}
}
