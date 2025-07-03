//using EPiServer;
//using EPiServer.Core;
//using EPiServer.DataAbstraction;
//using EPiServer.Web.Mvc;
//using Foundation.Features.Shared;
//using Mediachase.Commerce.Catalog;
//using Microsoft.AspNetCore.Mvc;
//using EPiServer.Security;
//using EPiServer.Commerce.Security;

//namespace Foundation.Features.Home
//{
//    public class HomeController : PageController<HomePage>
//    {
//        private readonly IContentLoader _contentLoader;
//        private readonly IContentSecurityRepository _contentSecurityRepository;
//        private readonly ReferenceConverter _referenceConverter;

//        public HomeController(
//            IContentLoader contentLoader,
//            IContentSecurityRepository contentSecurityRepository,
//            ReferenceConverter referenceConverter
//        ) {
//            _contentLoader = contentLoader;
//            _contentSecurityRepository = contentSecurityRepository;
//            _referenceConverter = referenceConverter;
//        }

//        public ActionResult Index(HomePage currentContent) {
//            if (_contentLoader.TryGet(_referenceConverter.GetRootLink(), out IContent content))
//            {
//                var securableContent = (IContentSecurable)content;
//                var defaultAccessControlList = (IContentSecurityDescriptor)securableContent.GetContentSecurityDescriptor().CreateWritableClone();
//                defaultAccessControlList.AddEntry(new AccessControlEntry(RoleNames.CommerceAdmins, AccessLevel.FullAccess, SecurityEntityType.Role));
//                defaultAccessControlList.AddEntry(new AccessControlEntry(EveryoneRole.RoleName, AccessLevel.Read, SecurityEntityType.Role));

//                _contentSecurityRepository.Save(content.ContentLink, defaultAccessControlList, SecuritySaveType.Replace);
//            }

//            return View(ContentViewModel.Create<HomePage>(currentContent));
//        }
//    }
//}
