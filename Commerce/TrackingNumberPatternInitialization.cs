using EPiServer.Framework.Initialization;
using EPiServer.Framework;
using Mediachase.Commerce.Orders;

namespace Foundation.Custom.Episerver_util_api.Commerce
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class TrackingNumberPatternInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            OrderContext.Current.TrackingNumberPattern = "^[A-Za-z0-9-]+$";
        }

        public void Uninitialize(InitializationEngine context) { }
    }
}
