using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Reference.Commerce.Shared.Identity;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("metafield-api")]
    public class MetafieldApiController : ControllerBase
    {
        public MetafieldApiController()
        {

        }

        [HttpGet]
        [Route("AddMetaFieldCheckboxBooleanOrganization")]
        public async Task<ActionResult<string>> AddMetaFieldCheckboxBooleanOrganization([FromQuery] string firstName = null)
        {
            string name = "Attends";
            string friendlyName = name;

            var typeName = MetaFieldType.CheckboxBoolean;
            var orgMetaClass = DataContext.Current.MetaModel.MetaClasses[OrganizationEntity.ClassName];
            var metaClass = orgMetaClass;

            string log = "";
            var existingField = metaClass.Fields[name];
            if (existingField == null)
            {
                var attributes = new Mediachase.BusinessFoundation.Data.Meta.Management.AttributeCollection
            {
                { McDataTypeAttribute.BooleanLabel, friendlyName },
                { McDataTypeAttribute.EnumEditable, true }
            };
                metaClass.CreateMetaField(name, friendlyName, typeName, attributes); // DeleteMetaField
                using (var myEditScope = DataContext.Current.MetaModel.BeginEdit())
                {
                    metaClass.Fields[name].AccessLevel = AccessLevel.Development;
                    metaClass.Fields[name].Owner = "Development";
                    myEditScope.SaveChanges();
                }
                log += String.Format("Meta field {0} is added to meta class {1}", name, OrganizationEntity.ClassName);
            }
            else
            {
                log += String.Format("Meta field {0} is already exist in meta class {1}", name, OrganizationEntity.ClassName);
            }

            return Ok(log);
        }
        
        [HttpGet]
        [Route("AddMetaFieldCheckboxBooleanAddress")]
        public async Task<ActionResult<string>> AddMetaFieldCheckboxBooleanAddress([FromQuery] string firstName = null)
        {
            string name = "Attends";
            string friendlyName = name;

            var typeName = MetaFieldType.CheckboxBoolean;
            var metaClassName = AddressEntity.ClassName;
            var metaClass = DataContext.Current.MetaModel.MetaClasses[metaClassName];

            string log = "";
            var existingField = metaClass.Fields[name];
            if (existingField == null)
            {
                var attributes = new Mediachase.BusinessFoundation.Data.Meta.Management.AttributeCollection
            {
                { McDataTypeAttribute.BooleanLabel, friendlyName },
                { McDataTypeAttribute.EnumEditable, true }
            };
                metaClass.CreateMetaField(name, friendlyName, typeName, attributes); // DeleteMetaField
                using (var myEditScope = DataContext.Current.MetaModel.BeginEdit())
                {
                    metaClass.Fields[name].AccessLevel = AccessLevel.Development;
                    metaClass.Fields[name].Owner = "Development";
                    myEditScope.SaveChanges();
                }
                log += String.Format("Meta field {0} is added to meta class {1}", name, metaClassName);
            }
            else
            {
                log += String.Format("Meta field {0} is already exist in meta class {1}", name, metaClassName);
            }

            return Ok(log);
        }
        
        [HttpGet]
        [Route("AddCustomMetaFieldToContact")]
        public async Task<ActionResult<string>> AddCustomMetaFieldToContact([FromQuery] string firstName = null)
        {
            string name = "FieldDemo1";
            string friendlyName = name;

            var typeName = MetaFieldType.CheckboxBoolean;
            var metaClassName = ContactEntity.ClassName;
            var metaClass = DataContext.Current.MetaModel.MetaClasses[metaClassName];

            string log = "";
            var existingField = metaClass.Fields[name];
            if (existingField == null)
            {
                using(var builder = new MetaFieldBuilder(metaClass))
                {
                    builder.CreateText(name, friendlyName, true, 100, false);
                    //builder.CreateLongText("ContactLongText", "Long Text", true);
                    //builder.CreateInteger("ContactInetger", "Integer", true, 0);
                    //builder.CreateDecimal("ContactDecimal", "Decimal", true, 0);
                    //builder.CreateDateTime("ContactDateTime", "Date and Time", true, false);
                    //builder.CreateGuid("ContactGuid", "Guid", true);
                    //builder.CreateCheckBoxBoolean("ContactBoolean", "Boolean", true, false, "Boolean");
                    builder.SaveChanges();
                }

                log += String.Format("Meta field {0} is added to meta class {1}", name, metaClassName);
            }
            else
            {
                log += String.Format("Meta field {0} is already exist in meta class {1}", name, metaClassName);
            }

            return Ok(log);
        }

        [HttpGet]
        [Route("AddReferenceToContact")]
        public async Task<ActionResult<string>> AddReferenceToContact([FromQuery] string firstName = null)
        {
            string name = "ReferenceFieldDemo2";
            string friendlyName = name;

            var typeName = MetaFieldType.CheckboxBoolean;
            var metaClassName = ContactEntity.ClassName;
            var parentMetaClassName = OrganizationEntity.ClassName;
            var metaClass = DataContext.Current.MetaModel.MetaClasses[metaClassName];

            string log = "";
            var existingField = metaClass.Fields[name];
            if (existingField == null)
            {
                using (var builder = new MetaFieldBuilder(metaClass))
                {
                    builder.CreateReference(name, friendlyName, true, parentMetaClassName, false);
                    //builder.CreateLongText("ContactLongText", "Long Text", true);
                    //builder.CreateInteger("ContactInetger", "Integer", true, 0);
                    //builder.CreateDecimal("ContactDecimal", "Decimal", true, 0);
                    //builder.CreateDateTime("ContactDateTime", "Date and Time", true, false);
                    //builder.CreateGuid("ContactGuid", "Guid", true);
                    //builder.CreateCheckBoxBoolean("ContactBoolean", "Boolean", true, false, "Boolean");
                    builder.SaveChanges();
                }

                log += String.Format("Meta field {0} is added to meta class {1}", name, metaClassName);
            }
            else
            {
                log += String.Format("Meta field {0} is already exist in meta class {1}", name, metaClassName);
            }

            return Ok(log);
        }
    }
}