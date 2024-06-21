
using EPiServer.Commerce.UI.CustomerService.Models;
using EPiServer.Shell.Web.Mvc;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using static Mediachase.Commerce.Orders.Dto.ReturnReasonsDto;

namespace Foundation.Custom
{
    [ApiController]
    [Route("returns")]
    public class ReturnReasonsManagerApiController : ControllerBase
    {
        public ReturnReasonsManagerApiController()
        {

        }

        #region CatalogContext

        [HttpGet]
        [Route("listReturnReason")]
        public async Task<ActionResult<string>> listReturnReason()
        {
            var dto = ReturnReasonsManager.GetReturnReasonsDto(false); //only the visible items
            var reasonList = dto.ReturnReasonDictionary.Select(row => new ReturnReasonModel(row.ReturnReasonText, row.ReturnReasonId.ToString())).ToList();

            return new JsonDataResult(reasonList);
        }

        [HttpGet]
        [Route("loadReturnReason")]
        public async Task<ActionResult<string>> loadReturnReason([FromQuery] string name = null)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "Faulty";
            }

            var dto = ReturnReasonsManager.GetReturnReasonByName(name);
            var reasonModel = dto.ReturnReasonDictionary.Select(row => new ReturnReasonModel(row.ReturnReasonText, row.ReturnReasonId.ToString())).First();

            return new JsonDataResult(reasonModel);
        }


        [HttpGet]
        [Route("createReturnReason")]
        public async Task<ActionResult<string>> CreateReturnReason([FromQuery] string name = null)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "TestReturnReason";
            }

            string log = "";

            // Create an instance of ReturnReasonsDto which contains the DataTable
            var returnReasonsDto = new ReturnReasonsDto();

            // Ensure the DataTable is initialized if it's not already
            var returnReasonTable = returnReasonsDto.ReturnReasonDictionary ?? new ReturnReasonDictionaryDataTable();

            // Add the new return reason to the DataTable
            returnReasonTable.AddReturnReasonDictionaryRow(name, 0, true);

            // Save the return reason using ReturnReasonsManager
            // Assuming SaveReturnReason accepts the modified DataTable
            ReturnReasonsManager.SaveReturnReason(returnReasonsDto);

            log += $"ReturnReason '{name}' has been created";

            return Ok(log);
        }



        #endregion


    }
}
