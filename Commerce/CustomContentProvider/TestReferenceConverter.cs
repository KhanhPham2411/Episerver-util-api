namespace Foundation.Custom.CustomContentProvider
{
    public class TestReferenceConverter : ReferenceConverter
    {
        public TestReferenceConverter(EntryIdentityResolver entryIdentityResolver, NodeIdentityResolver nodeIdentityResolver) : base(entryIdentityResolver, nodeIdentityResolver)
        {
        }

        public override ContentReference GetContentLink(int objectId, CatalogContentType contentType, int versionId)
        {
            var contentID = objectId | ((int)contentType << 30);

            if (contentType == CatalogContentType.CatalogEntry) {
                return new ContentReference(contentID, versionId, "TestCatalogContent");
            }

            return new ContentReference(contentID, versionId, "CatalogContent");
        }
    }
}
