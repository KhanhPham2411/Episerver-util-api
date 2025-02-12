
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.DataAbstraction;

namespace Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Data.DataInitialization))]
    public class RootPageInitialization : IInitializableModule
    {
        public const string RootPage = "SysRoot";

        public void Initialize(InitializationEngine context)
        {
            var setting = new AvailableSetting { Availability = Availability.Specific };
            var serviceLocator = ServiceLocator.Current;
            var contentRepository = serviceLocator.GetInstance<IContentTypeRepository>();

            //var startPage = contentRepository.Load(typeof(StartPage));
            //if (startPage == null)
            //{
            //    return;
            //}
            //setting.AllowedContentTypeNames.Add(startPage.Name);

            //var localizationContainer = contentRepository.Load(typeof(LocalizationContainer));
            //if (localizationContainer == null)
            //{
            //    return;
            //}
            //setting.AllowedContentTypeNames.Add(localizationContainer.Name);

            setting.AllowedContentTypeNames.Add("CatalogImportExportFolder");

            var sysRoot = contentRepository.Load(RootPage) as PageType;
            var availabilityRepository = serviceLocator.GetInstance<IAvailableSettingsRepository>();
            availabilityRepository.RegisterSetting(sysRoot, setting);
        }

        public void Uninitialize(InitializationEngine context)
        {

        }

        public void Preload(string[] parameters)
        {

        }
    }
}
