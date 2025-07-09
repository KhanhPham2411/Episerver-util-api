//using System.Collections.Specialized;
//using EPiServer.Commerce.Catalog.ContentTypes;
//using EPiServer.Commerce.Catalog.Linking;
//using EPiServer.Commerce.Catalog.Provider;
//using EPiServer.Commerce.Routing;
//using EPiServer.Core;
//using EPiServer.Core.Internal;
//using EPiServer.Core.Routing;
//using EPiServer.Framework;
//using EPiServer.Framework.Initialization;

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.DependencyInjection.Extensions;
//using ServiceDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

//namespace Foundation.Custom.CustomContentProvider;

///// <summary>
///// MagicBox catalog content provider initialization.
///// </summary>
//[InitializableModule]
//[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
//public class TestCatalogContentProviderInitialization : IConfigurableModule
//{
//    /// <summary>
//    /// Initialization.
//    /// </summary>
//    /// <param name="context"></param>
//    public void Initialize(InitializationEngine context)
//    {
//        #region Get instances from the context

//        IContentProviderManager providerManager = context.Locate.Advanced.GetInstance<IContentProviderManager>();

//        TestCatalogContentProvider testCatalogContentProvider =
//            context.Locate.Advanced.GetInstance<TestCatalogContentProvider>();

//        #endregion

//        #region Register the provider

//        NameValueCollection readonlyCatalogContentProviderValues = new NameValueCollection()
//        {
//            { ContentProviderParameter.EntryPoint, "0" },
//            { ContentProviderParameter.Capabilities, String.Join(",",
//                ContentProviderCapabilities.MultiLanguage
//            )}
//        };

//        testCatalogContentProvider.Initialize("TestCatalogContent",
//            readonlyCatalogContentProviderValues);

//        providerManager.ProviderMap.AddProvider(testCatalogContentProvider);

//        #endregion
//    }

//    /// <summary>
//    /// Uninitialization.
//    /// </summary>
//    /// <param name="context"></param>
//    public void Uninitialize(InitializationEngine context)
//    {

//    }

//    /// <summary>
//    /// Configure container.
//    /// </summary>
//    /// <param name="context"></param>
//    public void ConfigureContainer(ServiceConfigurationContext context)
//    {

//        context.Services.AddSingleton<TestCatalogContentProvider>();


//        ServiceDescriptor catalogContentServiceProviderDescriptor =
//            ServiceDescriptor.Singleton<CatalogContentProvider, TestCatalogContentProvider>();
//        context.Services.Replace(catalogContentServiceProviderDescriptor);

//        ServiceDescriptor catalogReferenceConverterProviderDescriptor =
//           ServiceDescriptor.Singleton<ReferenceConverter, TestReferenceConverter>();
//        context.Services.Replace(catalogReferenceConverterProviderDescriptor);

//    }
//}