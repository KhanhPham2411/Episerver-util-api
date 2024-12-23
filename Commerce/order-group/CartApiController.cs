
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Orders.Internal;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using HolmenExternal.Domain.Features.Commerce.Shared.Customer;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Foundation.Custom
{
    [ApiController]
    [Route("cart-api")]
    public class CartApiController : ControllerBase
    {
        
        public CartApiController()
        {

        }
        
        [HttpGet]
        [Route("add")]
        public async Task<ActionResult<string>> Add()
        {
            var uiSignInManager = ServiceLocator.Current.GetInstance<UISignInManager>();
            var _orderGroupFactory = ServiceLocator.Current.GetInstance<IOrderGroupFactory>();
            var orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
            var _orderValidationService = ServiceLocator.Current.GetInstance<OrderValidationService>();
            var cartService = ServiceLocator.Current.GetInstance<ICartService>();
            var _inventoryService = ServiceLocator.Current.GetInstance<IInventoryService>();
            var _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            var log = "";
            var cartName = "Default";
            var cart = orderRepository.LoadOrCreateCart<ICart>(CustomerContext.Current.CurrentContactId, cartName);

            
            //string[] codes = { "HZ-HO-7726-AA-54", "HZ-HO-7726-GR-54" };
            string[] codes = { "CO22021X", "HZ-HO-7726-AA-54" };

            foreach (var code in codes) 
            {
                var quantity = 1;
                var lineItem = cart.GetAllLineItems().FirstOrDefault(x => x.Code == code && !x.IsGift);
                if (lineItem == null)
                {
                    var contentLink = _referenceConverter.GetContentLink(code);
                    var defaultVariation = _contentLoader.Get<VariationContentBase>(contentLink);

                    lineItem = cart.CreateLineItem(code);
                    //lineItem.DisplayName = !string.IsNullOrWhiteSpace(defaultVariation.DisplayName) ? defaultVariation.DisplayName : defaultVariation.Name;
                    lineItem.Quantity = quantity;
                    //lineItem.PlacedPrice = _pricesService.GetSalePrice(defaultVariation, 1, cart.Currency).Money.Amount;
                    //lineItem.TaxCategoryId = defaultVariation.TaxCategoryId;
                }

                var inventoryRecord = _inventoryService.QueryByEntry(new List<string>() { code }).FirstOrDefault();
                if (inventoryRecord != null)
                {
                    var shipment = cart.GetFirstForm().Shipments.FirstOrDefault(x => x.WarehouseCode == inventoryRecord.WarehouseCode);

                    if (shipment == null)
                    {
                        shipment = cart.GetFirstShipment();

                        if (shipment == null)
                        {
                            shipment = cart.CreateShipment();
                            shipment.WarehouseCode = inventoryRecord.WarehouseCode;
                            shipment.OrderShipmentStatus = OrderShipmentStatus.AwaitingInventory;
                            //((Shipment)shipment).ShippingAddressId = ShippingAddressName;
                            cart.AddShipment(shipment);
                            cart.AddLineItem(shipment, lineItem);

                            log += $"add {code} to the cart \n";
                        }
                        else if (string.IsNullOrEmpty(shipment.WarehouseCode))
                        {
                            shipment.WarehouseCode = inventoryRecord.WarehouseCode;
                            shipment.OrderShipmentStatus = OrderShipmentStatus.AwaitingInventory;
                            //((Shipment)shipment).ShippingAddressId = ShippingAddressName;
                            cart.AddLineItem(shipment, lineItem);

                            log += $"add {code} to the cart \n";
                        }
                        else if (shipment.WarehouseCode != inventoryRecord.WarehouseCode)
                        {
                            var newShipment = cart.CreateShipment();
                            newShipment.WarehouseCode = inventoryRecord.WarehouseCode;
                            newShipment.OrderShipmentStatus = OrderShipmentStatus.AwaitingInventory;
                            //((Shipment)shipment).ShippingAddressId = ShippingAddressName;
                            cart.AddShipment(newShipment);
                            cart.AddLineItem(newShipment, lineItem);

                            log += $"add {code} to the cart \n";
                        }
                    }
                    else if (!shipment.LineItems.Any(x => x.Equals(lineItem)))
                    {
                        cart.AddLineItem(shipment, lineItem);
                        log += $"add {code} to the cart \n";
                    }
                }
            }
            //var validation = _orderValidationService.ValidateOrder(cart);

            orderRepository.Save(cart);

            return Ok(log);
        }
      
        [HttpGet]
        [Route("add-basic")]
        public async Task<ActionResult<string>> AddBasic()
        {
            var uiSignInManager = ServiceLocator.Current.GetInstance<UISignInManager>();
            var _orderGroupFactory = ServiceLocator.Current.GetInstance<IOrderGroupFactory>();
            var orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();
          
            var log = "";
            var cart = orderRepository.LoadOrCreateCart<ICart>(CustomerContext.Current.CurrentContactId, "Default");

            
            string[] codes = { "HZ-HO-7726-AA-54", "HZ-HO-7726-GR-54" };

            foreach (var code in codes) 
            {
                var quantity = 1;
                var lineItem = cart.GetAllLineItems().FirstOrDefault(x => x.Code == code && !x.IsGift);
                if (lineItem == null)
                {
                    lineItem = cart.CreateLineItem(code, _orderGroupFactory);
                    //lineItem.DisplayName = entryContent.DisplayName;
                    lineItem.Quantity = quantity;
                    cart.AddLineItem(lineItem, _orderGroupFactory);

                    log += $"add {code} to the cart \n";
                }
                else
                {
                    //var shipment = cart.GetFirstShipment();
                    //cart.UpdateLineItemQuantity(shipment, lineItem, lineItem.Quantity + quantity);

                    log += $"{code} already exist \n";
                }
            }
            orderRepository.Save(cart);

            return Ok(log);
        }
    }
}
