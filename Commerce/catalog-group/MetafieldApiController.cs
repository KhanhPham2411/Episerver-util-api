using EPiServer.Cms.UI.AspNetIdentity;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.BusinessFoundation.Data.Sql.Management;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus.Configurator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("metafield")]
    public class MetafieldApiController : ControllerBase
    {
        public MetafieldApiController()
        {

        }

        [HttpGet]
        [Route("CreateMetaClassPrimaryKey")]
        public async Task<ActionResult<string>> CreateMetaClassPrimaryKey([FromQuery] string name = null)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "TestMetaClass";
            }
            string log = "";


            var dataContext = DataContext.Current;
            if (dataContext.MetaModel.MetaClasses.Contains(name))
            {
                log += String.Format("Meta class {0} is already exist", name);
                return log;
            }

            Mediachase.BusinessFoundation.Data.Meta.Management.MetaClass metaClass = dataContext.MetaModel.CreateMetaClass(
                name,
                name,
                name + "s",
                "cls_" + name,
                PrimaryKeyIdValueType.Integer);

            log += String.Format("Meta class {0} is created successfully", name);

            return Ok(log);
        }

        [HttpGet]
        [Route("DeleteMetaField")]
        public async Task<ActionResult<string>> DeleteMetaField([FromQuery] string name = null)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "TestMaxLength";
            }

            string log = "";
            var metaClassname = OrganizationEntity.ClassName;
            var orgMetaClass = DataContext.Current.MetaModel.MetaClasses[metaClassname];
            var metaClass = orgMetaClass;
            var existingField = metaClass.Fields[name];

            if (existingField != null)
            {
                metaClass.DeleteMetaField(existingField);
                log += String.Format("Meta field {0} is deleted to meta class {1}", name, metaClassname);
            }
            else
            {
                log += String.Format("Meta field {0} is not exist in meta class {1}", name, metaClassname);
            }
            return Ok(log);
        }

        [HttpGet]
        [Route("AddMetaFieldWithoutMaxLengthOrganization")]
        public async Task<ActionResult<string>> AddMetaFieldWithoutMaxLengthOrganization([FromQuery] string name = null)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "Testv1";
            }

            string friendlyName = name;

            var typeName = MetaFieldType.Text;
            var orgMetaClass = DataContext.Current.MetaModel.MetaClasses[OrganizationEntity.ClassName];
            var metaClass = orgMetaClass;

            string log = "";
            var existingField = metaClass.Fields[name];
            if (existingField == null)
            {
                var attributes = new Mediachase.BusinessFoundation.Data.Meta.Management.AttributeCollection
                {
                    { McDataTypeAttribute.StringLongText, friendlyName }
                };

                metaClass.CreateMetaField(name, friendlyName, typeName, true, "", attributes);

                log += String.Format("Meta field {0} is added to meta class {1}", name, OrganizationEntity.ClassName);
            }
            else
            {
                log += String.Format("Meta field {0} is already exist in meta class {1}", name, OrganizationEntity.ClassName);
            }

            return Ok(log);
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
        public async Task<ActionResult<string>> AddMetaFieldCheckboxBooleanAddress()
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
        public async Task<ActionResult<string>> AddCustomMetaFieldToContact()
        {
            string name = "FieldDemo4";
            string friendlyName = name;

            var typeName = MetaFieldType.CheckboxBoolean;
            var metaClassName = ContactEntity.ClassName;
            var metaClass = DataContext.Current.MetaModel.MetaClasses[metaClassName];

            string log = "";
            var existingField = metaClass.Fields[name];
            if (existingField == null)
            {
                using (var builder = new MetaFieldBuilder(metaClass))
                {
                    builder.CreateText(name, friendlyName, true, 200, false);
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
        [Route("UpdateMaxLengthMetaFieldOfContact")]
        public async Task<ActionResult<string>> UpdateMaxLengthMetaFieldOfContact()
        {
            string name = "FieldDemo2";
            string friendlyName = name;

            var typeName = MetaFieldType.CheckboxBoolean;
            var metaClassName = ContactEntity.ClassName;
            var metaClass = DataContext.Current.MetaModel.MetaClasses[metaClassName];

            string log = "";
            var existingField = metaClass.Fields[name];
            if (existingField == null)
            {
                log += String.Format("Meta field {0} is not exist in meta class {1}", name, metaClassName);
            }
            else
            {
                using (MetaClassManagerEditScope editScope = DataContext.Current.MetaModel.BeginEdit())
                {
                    existingField.Attributes.Remove("Maxlength");
                    existingField.Attributes.Add("Maxlength", 300);
                    editScope.SaveChanges();
                }

                log += String.Format("Meta field {0} is changed the max length in meta class {1}", name, metaClassName);
            }

            return Ok(log);
        }

        [HttpGet]
        [Route("ExecuteTableSPCreateScript")]
        public async Task<ActionResult<string>> ExecuteTableSPCreateScript()
        {

            var metaClassName = ContactEntity.ClassName;
            var metaClass = DataContext.Current.MetaModel.MetaClasses[metaClassName];

            string log = "";

            Database.ExecuteTableSPCreateScript(metaClass.GetTableConfig());
            log += String.Format("ExecuteTableSPCreateScript on Meta class {0}", metaClassName);


            return Ok(log);
        }

        [HttpGet]
        [Route("AddReferenceToContact")]
        public async Task<ActionResult<string>> AddReferenceToContact()
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

        [HttpGet]
        [Route("AddPhoneNumberToOrder")]
        public async Task<ActionResult<string>> AddPhoneNumberToOrder()
        {
            var name = "PhoneNumber";
            var metaNamespace = string.Empty;
            var friendlyName = "PhoneNumber";
            var description = string.Empty;
            var metaFieldType = MetaDataType.ShortString;
            var isNullable = true;
            var length = 0;
            var isMultiLanguage = false;
            var isSearchable = false;
            var isEncrypted = false;

            var metaClass = OrderContext.Current.PurchaseOrderMetaClass;
            if (metaClass.MetaFields.Any(x => x.Name == name))
                return Ok(name + " metafield is already exists");

            var metaContext = OrderContext.MetaDataContext;

            var metaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(metaContext,
                                           metaNamespace,
                                           name,
                                           friendlyName,
                                           description,
                                           metaFieldType,
                                           length,
                                           isNullable,
                                           isMultiLanguage,
                                           isSearchable,
                                           isEncrypted);

            metaClass.AddField(metaField);
            return Ok(name + " metafield is added to metaclass " + metaClass.Name);
        }

        [HttpGet]
        [Route("AddEnumSingleToOrder")]
        public async Task<ActionResult<string>> AddEnumSingleToOrder()
        {

            var name = "TestEnumSingle";
            var metaNamespace = string.Empty;
            var friendlyName = "TestEnumSingle";
            var description = string.Empty;
            var metaFieldType = MetaDataType.EnumSingleValue;
            var isNullable = true;
            var length = 0;
            var isMultiLanguage = false;
            var isSearchable = false;
            var isEncrypted = false;

            var metaClass = OrderContext.Current.PurchaseOrderMetaClass;
            if (metaClass.MetaFields.Any(x => x.Name == name))
                return Ok(name + " metafield is already exists");

            var metaContext = OrderContext.MetaDataContext;

            var metaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(metaContext,
                                           metaNamespace,
                                           name,
                                           friendlyName,
                                           description,
                                           metaFieldType,
                                           length,
                                           isNullable,
                                           isMultiLanguage,
                                           isSearchable,
                                           isEncrypted);

            metaClass.AddField(metaField);
            return Ok(name + " metafield is added to metaclass " + metaClass.Name);
        }

        [HttpGet]
        [Route("SetReadOnlyContactField")]
        public async Task<ActionResult<string>> SetReadOnlyContactField([FromQuery] string fieldName = "UserLocation", 
            [FromQuery] bool value = true
        )
        {
            SetReadOnlyFieldsValue(new string[] { fieldName }, value);
            return "";
        }

        private void SetReadOnlyFieldsValue(IEnumerable<string> fieldNames, bool value)
        {
            bool fieldsUpdated = false;

            var metaClassName = ContactEntity.ClassName;
            var customerMetadata = DataContext.Current.MetaModel.MetaClasses[metaClassName];

            foreach (string fieldName in fieldNames)
            {
                if (customerMetadata.Fields[fieldName] != null)
                {
                    if (customerMetadata.Fields[fieldName].ReadOnly == value)
                    {
                        continue;
                    }

                    var field = customerMetadata.Fields[fieldName];
                    field.ReadOnly = value;

                    fieldsUpdated = true;
                }
            }

            if (fieldsUpdated)
            {
                using (MetaClassManagerEditScope editScope = DataContext.Current.MetaModel.BeginEdit())
                {
                    editScope.SaveChanges();
                }
            }
        }
    }
}
