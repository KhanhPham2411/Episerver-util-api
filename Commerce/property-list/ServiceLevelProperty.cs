using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.PlugIn;
using EPiServer.Shell.ObjectEditing;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using System.Linq;

namespace EPiServer.Reference.Commerce.Site.Custom.PropertyList
{
    [PropertyDefinitionTypePlugIn]
    [Serializable]
    public class ServiceLevelProperty : PropertyList<ServiceLevel>
    {
        public ServiceLevelProperty()
        {
        }
        //public override PropertyData ParseToObject(string value)
        //{
        //    ParseToSelf(value);
        //    return this;
        //}
        protected override ServiceLevel ParseItem(string value)
        {
            return Parse(value);
        }
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (IsNull)
            {
                return String.Empty;
            }
            return string.Join(StringRepresentationSeparator.ToString(),
            List.Select(x => String.Format("{0};{1}", x.DealerProductLineCode, x.DealerServiceCode)));
        }
        /// <summary>
        /// Parses the specified string value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A money object.</returns>
        /// <exception cref="ArgumentNullException">If value is null.</exception>
        /// <exception cref="ArgumentException">If the string value cannot be parsed because of format, invalid decimal, or amount less than zero.</exception>
        private static ServiceLevel Parse(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException("value");
            }
            var values = value.Split(';');
            if (values.Length != 2)
            {
                throw new ArgumentException(String.Format("String representation does not have two values for properties. Example: 'USD;99.99'. Actual: {0}", value));
            }
            return new ServiceLevel
            {
                DealerProductLineCode = values[0],
                DealerServiceCode = values[1]
            };
        }
    }
    public class ServiceLevel
    {
        [Display(Name = "Product Line Code", Order = 10)]
        //[SelectOne(SelectionFactoryType = typeof(ColorSelectionFactory))]
        public string DealerProductLineCode { get; set; }

        [Display(Name = "Dealer Service Code", Order = 10)]
        //[SelectOne(SelectionFactoryType = typeof(ColorSelectionFactory))]
        public string DealerServiceCode { get; set; }
    }

    //[Display(Name = "Poaris", Order = 80)]
    //[BackingType(typeof(ServiceLevelProperty))]
    //[EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<ServiceLevel>))]
    //public virtual IList<ServiceLevel> Poaris { get; set; }
}
