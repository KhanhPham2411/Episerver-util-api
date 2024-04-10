using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;
using Newtonsoft.Json;
using Serilog;

namespace Foundation.Custom
{
    [ApiController]
    [Route("price")]
    public class PriceApiController : ControllerBase
    {
        public PriceApiController()
        {

        }

        [HttpGet]
        [Route("CreatePrice")]

        public async Task<ActionResult<string>> CreatePrice([FromQuery] string firstName = null)
        {
            var priceService = ServiceLocator.Current.GetInstance<IPriceService>();
            string log = "";
            var sku = "SKU-39813617";
            var catalogKey = new CatalogKey(sku);

            var pricesJson = $"[\r\n    {{\r\n        \"Price\": 89.9,\r\n        \"ValidFromDate\": \"2021-03-08T00:00:00Z\",\r\n        \"ValidToDate\": \"2022-01-12T23:59:59.999Z\"\r\n    }},\r\n    {{\r\n        \"Price\": 99.0,\r\n        \"ValidFromDate\": \"2022-01-13T00:00:00Z\",\r\n        \"ValidToDate\": \"2022-11-02T23:59:59.999Z\"\r\n    }},\r\n    {{\r\n        \"Price\": 99.0,\r\n        \"ValidFromDate\": \"2022-11-03T00:00:00Z\",\r\n        \"ValidToDate\": \"2022-11-23T23:59:59.999Z\"\r\n    }},\r\n    {{\r\n        \"Price\": 129.0,\r\n        \"ValidFromDate\": \"2022-11-24T00:00:00Z\",\r\n        \"ValidToDate\": \"2022-11-24T23:59:59.999Z\"\r\n    }},\r\n    {{\r\n        \"Price\": 129.0,\r\n        \"ValidFromDate\": \"2022-11-25T00:00:00Z\",\r\n        \"ValidToDate\": \"9999-12-31T00:00:00Z\"\r\n    }}\r\n]\r\n";
            var prices = JsonConvert.DeserializeObject<List<PriceImport>>(pricesJson);
            var skuPrices = new List<IPriceValue>();

            foreach (var price in prices)
            {
                var newPrice = new PriceValue
                {
                    CatalogKey = catalogKey,
                    CustomerPricing = CustomerPricing.AllCustomers,
                    MarketId = "SWE",
                    MinQuantity = decimal.Zero,
                    UnitPrice = new Money(price.Price, "SEK"),
                    ValidFrom = price.ValidFromDate != DateTime.MinValue ? price.ValidFromDate : new DateTime(2000, 1, 1),
                    ValidUntil = price.ValidToDate != DateTime.MinValue ? price.ValidToDate : null
                };

                skuPrices.Add(newPrice);
            }

            priceService.SetCatalogEntryPrices(new CatalogKey(sku), skuPrices);

            return Ok("create price successfully");
        }

        public class PriceImport
        {
            public DateTime ValidFromDate { get; set; }
            public DateTime ValidToDate { get; set; }
            public decimal Price { get; set; }
        }

    }
}
