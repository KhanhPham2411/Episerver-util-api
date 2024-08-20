using EPiServer.Find.Helpers;


namespace Foundation.Features.CustomCatalog
{
    public class CatalogBaseController : ContentController<CustomCatalog>
    {
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentRepository _contentRepository;

        public CatalogBaseController(ReferenceConverter referenceConverter,
            IContentRepository contentRepository,
            IContentVersionRepository contentVersionRepository)
        {
            _referenceConverter = referenceConverter;
            _contentRepository = contentRepository;
            _contentVersionRepository = contentVersionRepository;
        }

        public IActionResult Index(CustomCatalog currentPage)
        {
            if (currentPage.IsNull()) throw new ArgumentNullException(nameof(currentPage));

            if (currentPage.ContentLink.WorkID == 0)
            {
                var versions = _contentVersionRepository.List(currentPage.ContentLink).ToList();
                var lastestVersion = versions
                    .Where(v => v.Status == VersionStatus.Published && v.LanguageBranch == currentPage.Language.Name)
                    .OrderByDescending(v => v.Saved)
                    .FirstOrDefault();

                var catalog = _contentRepository.Get<CustomCatalog>(lastestVersion.ContentLink).CreateWritableClone<CustomCatalog>();
                return View(new CatalogBaseViewModel(catalog));
            }

            var vm = new CatalogBaseViewModel(currentPage);

            return View(vm);
        }
    }
}
