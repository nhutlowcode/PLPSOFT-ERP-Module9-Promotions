using System.Collections.Generic;
using System.Linq;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Services
{
    public static class BuyXGetYHandler
    {
        public static List<CartItemDto> Process(PromotionDto promo, List<CartItemDto> cartItems)
        {
            var gifts = new List<CartItemDto>();

            // BƯỚC 1: Kiểm tra xem promotion có phải BOGO không
            if (promo?.PromotionType != "BOGO" && promo?.PromotionType != "BUY_X_GET_Y")
            {
                return gifts;
            }

            // Kiểm tra promo có sản phẩm định nghĩa không
            if (promo.Products == null || !promo.Products.Any())
            {
                return gifts;
            }

            // BƯỚC 2: Tách sản phẩm điều kiện & sản phẩm tặng
            var requiredProducts = promo.Products.Where(p => !p.IsGiftProduct).ToList();
            var giftProducts = promo.Products.Where(p => p.IsGiftProduct).ToList();

            // BƯỚC 3: Kiểm tra giỏ hàng có đủ sản phẩm điều kiện không
            foreach (var requiredProduct in requiredProducts)
            {
                var cartItem = cartItems.FirstOrDefault(c => c.ProductID == requiredProduct.ProductID);

                if (cartItem == null) return gifts;

                if (requiredProduct.RequiredQuantity.HasValue && cartItem.Quantity < requiredProduct.RequiredQuantity.Value)
                {
                    return gifts;
                }
            }

            // BƯỚC 4: TẠO DANH SÁCH QUÀ TẶNG (Đã khôi phục lại đoạn này)
            foreach (var giftProduct in giftProducts)
            {
                var giftItem = new CartItemDto
                {
                    ProductID = giftProduct.ProductID,
                    CategoryID = null,
                    Quantity = giftProduct.FreeQuantity ?? 1,
                    UnitPrice = 0  // Hàng tặng không tính tiền
                };
                gifts.Add(giftItem);
            }

            // BƯỚC 5: Trả về kết quả
            return gifts;
        }

        public static bool IsBOGOValid(PromotionDto promo)
        {
            return (promo?.PromotionType == "BOGO" || promo?.PromotionType == "BUY_X_GET_Y")
                && promo.Products != null
                && promo.Products.Any();
        }

        public static List<PromotionProductDto> GetRequiredProducts(PromotionDto promo)
        {
            if (promo?.Products == null) return new List<PromotionProductDto>();
            return promo.Products.Where(p => !p.IsGiftProduct).ToList();
        }

        public static List<PromotionProductDto> GetGiftProducts(PromotionDto promo)
        {
            if (promo?.Products == null) return new List<PromotionProductDto>();
            return promo.Products.Where(p => p.IsGiftProduct).ToList();
        }
    }
}