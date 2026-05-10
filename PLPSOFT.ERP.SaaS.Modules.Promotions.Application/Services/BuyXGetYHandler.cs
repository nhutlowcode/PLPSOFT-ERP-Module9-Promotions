using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Generic;
using System.Linq;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Services
{
    public static class BuyXGetYHandler
    {
        public static List<CartItemDto> Process(
            PromotionDto promo,
            List<CartItemDto> cartItems)
        {
            var gifts = new List<CartItemDto>();

            if (promo.PromotionType != "BOGO")
                return gifts;

            var requiredProducts = promo.Products
                .Where(p => !p.IsGiftProduct)
                .ToList();

            var giftProducts = promo.Products
                .Where(p => p.IsGiftProduct)
                .ToList();

            bool conditionMet = requiredProducts.All(req =>
            {
                var cartItem = cartItems
                    .FirstOrDefault(c => c.ProductID == req.ProductID);

                if (cartItem == null)
                    return false;

                return !req.RequiredQuantity.HasValue
                    || cartItem.Quantity >= req.RequiredQuantity.Value;
            });

            if (!conditionMet)
                return gifts;

            foreach (var gift in giftProducts)
            {
                gifts.Add(new CartItemDto
                {
                    ProductID = gift.ProductID,
                    Quantity = gift.FreeQuantity ?? 1,
                    UnitPrice = 0
                });
            }

            return gifts;
        }
    }
}