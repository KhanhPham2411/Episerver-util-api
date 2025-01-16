using EPiServer.Commerce.Catalog.DataAnnotations;
using System.ComponentModel;
using Castle.Core.Internal;


namespace Foundation.Features.CustomCatalog
{
    [CatalogContentType(
        GUID = "4F546F4E-30C9-4E48-B5C3-8756C7FEB6E1",
        MetaClassName = nameof(CustomCatalog),
        DisplayName = "Custom Catalog")]
    public class CustomCatalog : EPiServer.Commerce.Catalog.ContentTypes.CatalogContent
    {
        [Display(
            Name = "DemoText",
            Description = "DemoText field",
            GroupName = SystemTabNames.Content,
            Order = 100)]
        [CultureSpecific]
        public virtual string DemoText { get; set; }


        public override void SetDefaultValues(ContentType contentType)
        {
            var properties = GetType()?.BaseType?.GetProperties() ?? throw new InvalidOperationException();
            foreach (var property in properties)
            {
                var defaultValueAttribute = property.GetAttribute<DefaultValueAttribute>();
                if (defaultValueAttribute != null)
                {
                    this[property.Name] = defaultValueAttribute.Value;
                }
            }

            base.SetDefaultValues(contentType);
        }
    }
}
