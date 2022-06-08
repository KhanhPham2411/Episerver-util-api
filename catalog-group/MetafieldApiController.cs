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
    }
}