

using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.DataAbstraction.RuntimeModel;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;
using System.Drawing.Imaging;
using MetaClass = Mediachase.MetaDataPlus.Configurator.MetaClass;
using MetaField = Mediachase.MetaDataPlus.Configurator.MetaField;

namespace Foundation.Custom
{
    [ApiController]
    [Route("ordermetadataplus")]
    public class OrderMetaDataPlusApiController : ControllerBase
    {
        public OrderMetaDataPlusApiController()
        {

        }

        #region OrderContext
        [HttpGet]
        [Route("listMetaClass")]
        public async Task<ActionResult<string>> listMetaClass([FromQuery] string demo = null)
        {
            if (String.IsNullOrEmpty(demo))
            {
                demo = "demo";
            }
            string log = "";


            var metaClassCollection = Mediachase.MetaDataPlus.Configurator.MetaClass.GetList(OrderContext.MetaDataContext, true);
            var result = metaClassCollection.Cast<Mediachase.MetaDataPlus.Configurator.MetaClass>()
                        .Where(c => c.IsCatalogMetaClass == false);

            log += "List of user metaclass:\n";
            log += string.Join("\n", result.Select(s => s.Name));


            return Ok(log);
        }


        [HttpGet]
        [Route("listUserMetaClass")]
        public async Task<ActionResult<string>> listUserMetaClass([FromQuery] string demo = null)
        {
            if (String.IsNullOrEmpty(demo))
            {
                demo = "demo";
            }
            string log = "";


            var metaClassCollection = Mediachase.MetaDataPlus.Configurator.MetaClass.GetList(OrderContext.MetaDataContext, true);
            var result = metaClassCollection.Cast<Mediachase.MetaDataPlus.Configurator.MetaClass>()
                        .Where(c => c.IsUser
                        && c.MetaFields.Any(x => x.IsUser));

            log += "List of user metaclass:\n";
            log += string.Join("\n", result.Select(s => s.Name));


            return Ok(log);
        }

        [HttpGet]
        [Route("loadMetaClass")]
        public async Task<ActionResult<string>> loadMetaClass([FromQuery] string metaClassName = null)
        {
            if (String.IsNullOrEmpty(metaClassName))
            {
                metaClassName = "PurchaseOrder";
            }
            string log = "";


            var metaClass = MetaClass.Load(OrderContext.MetaDataContext, metaClassName);
            if (metaClass == null)
            {
                Ok("Not found");
            }


            return Ok(metaClass.FriendlyName);
        }


        public MetaField createMetaFieldInternal(string name)
        {
            var metaNamespace = string.Empty;
            var friendlyName = name;
            var description = string.Empty;
            var metaFieldType = MetaDataType.LongString;
            var isNullable = false; // set it to false for required attribute
            var length = 0;
            var isMultiLanguage = false;
            var isSearchable = false;
            var isEncrypted = false;


            var metaField = MetaField.Create(OrderContext.MetaDataContext,
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
            return metaField;
        }

        [HttpGet]
        [Route("createMetaField")]
        public async Task<ActionResult<string>> createMetaField([FromQuery] string demo = null)
        {
            if (String.IsNullOrEmpty(demo))
            {
                demo = "demo";
            }
            string log = "";

            var name = "TestField";
            createMetaFieldInternal(name);

            log += $"Field {name} has been created";

            return Ok(log);
        }

        [HttpGet]
        [Route("addMetaField")]
        public async Task<ActionResult<string>> addMetaField([FromQuery] string metaFieldName = null)
        {
            if (String.IsNullOrEmpty(metaFieldName))
            {
                metaFieldName = "TestField";
            }
            string log = "";
            var metaClassName = "PurchaseOrder";

            var metaField = MetaField.Load(OrderContext.MetaDataContext, metaFieldName);
            if (metaField == null)
            {
                log += $"Creating the field {metaFieldName} due to not found \n";
                metaField = createMetaFieldInternal(metaFieldName);
            }

            var metaClass = MetaClass.Load(OrderContext.MetaDataContext, metaClassName);

            if (!metaClass.MetaFields.Contains(metaField))
            {
                metaClass.AddField(metaField);
                log += $"Successfully, field {metaField.Name} has been added to class {metaClass.Name}";
                return Ok(log);
            }

            log += $"Failed, field {metaField.Name} has already been added to class {metaClass.Name}";
            return Ok(log);
        }

        #endregion


    }
}
