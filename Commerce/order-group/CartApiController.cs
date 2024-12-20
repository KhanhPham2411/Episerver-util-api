
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
