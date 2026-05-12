using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Services
{
    public class PromotionEngineService : IPromotionEngineService
    {
        private readonly IPromotionRepository _repository;
        private readonly ILogger<PromotionEngineService> _logger;

        public PromotionEngineService(
            IPromotionRepository repository,
            ILogger<PromotionEngineService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<CartDiscountResult> CalculateBestDiscountAsync(CartRequest request)
        {
            var result = new CartDiscountResult();

            if (request.Items == null || !request.Items.Any())
            {
                _logger.LogWarning("CartRequest không có Items");
                return result;
            }

            // BƯỚC 1: LẤY DỮ LIỆU
            var promos = await _repository.GetActivePromotionsAsync(request.CompanyID);

            if (request.BranchID.HasValue)
            {
                promos = promos.Where(p => !p.BranchId.HasValue || p.BranchId == request.BranchID.Value).ToList();
            }

            if (!promos.Any()) return result;

            var cartTotal = request.Items.Sum(i => i.LineTotal);
            var cartQty = request.Items.Sum(i => i.Quantity);
            var cartCategoryIDs = request.Items.Select(i => i.CategoryID).Distinct().ToList();

            // BƯỚC 2: VALIDATE & TÍNH DISCOUNT TỪNG PROMOTION
            var stackablePromotions = new List<(PromotionDto promo, decimal discount)>();
            var nonStackablePromotions = new List<(PromotionDto promo, decimal discount)>();

            foreach (var promo in promos.Where(p => p.PromotionType != "BOGO" && p.PromotionType != "BUY_X_GET_Y"))
            {
                // Giả định PromotionValidator đã được triển khai tĩnh (Static) ở project của bạn
                if (!PromotionValidator.IsPromotionActive(promo)) continue;

                // --- FIX LỖI 1: Lọc Sản phẩm được áp dụng ---
                // Nếu KM có danh sách sản phẩm (không phải hàng tặng), chỉ tính tiền các sản phẩm đó.
                // Nếu rỗng (Count == 0), tức là áp dụng toàn sàn.
                var appliedProductIds = promo.Products?.Where(p => !p.IsGiftProduct).Select(p => p.ProductID).ToList() ?? new List<long>();

                decimal applicableTotal = cartTotal;
                if (appliedProductIds.Any())
                {
                    applicableTotal = request.Items.Where(i => appliedProductIds.Contains(i.ProductID)).Sum(i => i.LineTotal);
                }

                // Nếu trong giỏ không có món nào thuộc danh sách áp dụng -> Bỏ qua KM này
                if (applicableTotal <= 0 && appliedProductIds.Any()) continue;

                bool ruleMatched = false;
                decimal ruleDiscount = 0;

                foreach (var rule in promo.Rules)
                {
                    if (!PromotionValidator.IsValidMinOrder(rule, cartTotal)) continue;
                    if (!PromotionValidator.IsValidQuantity(rule, cartQty)) continue;
                    if (!PromotionValidator.IsValidCustomerGroup(rule, request.CustomerGroupID)) continue;
                    if (!PromotionValidator.IsValidCategory(rule, cartCategoryIDs)) continue;

                    // Tính discount trên TỔNG TIỀN ĐƯỢC ÁP DỤNG (applicableTotal)
                    if (rule.DiscountType == "PERCENT")
                    {
                        ruleDiscount = applicableTotal * rule.DiscountValue / 100m;
                        if (rule.MaxDiscountAmount.HasValue)
                        {
                            ruleDiscount = Math.Min(ruleDiscount, rule.MaxDiscountAmount.Value);
                        }
                    }
                    else if (rule.DiscountType == "AMOUNT")
                    {
                        ruleDiscount = rule.DiscountValue;
                    }

                    ruleMatched = true;
                    break; // Thỏa 1 rule là ăn tiền, thoát vòng lặp
                }

                if (!ruleMatched || ruleDiscount <= 0) continue;

                if (promo.IsStackable)
                    stackablePromotions.Add((promo, ruleDiscount));
                else
                    nonStackablePromotions.Add((promo, ruleDiscount));
            }

            // BƯỚC 3: XỬ LÝ STACKABLE vs NON-STACKABLE
            foreach (var (promo, discount) in stackablePromotions)
            {
                result.TotalDiscount += discount;
                result.AppliedPromotions.Add(new AppliedPromotionDto { PromotionID = promo.PromotionID, PromotionName = promo.PromotionName, DiscountAmount = discount });
            }

            if (nonStackablePromotions.Any())
            {
                var bestNonStackable = nonStackablePromotions.OrderByDescending(x => x.discount).First();
                result.TotalDiscount += bestNonStackable.discount;
                result.AppliedPromotions.Add(new AppliedPromotionDto { PromotionID = bestNonStackable.promo.PromotionID, PromotionName = bestNonStackable.promo.PromotionName, DiscountAmount = bestNonStackable.discount });
            }

            // BƯỚC 4: XỬ LÝ BOGO (BUY X GET Y)
            var bogoPromotions = promos.Where(p => (p.PromotionType == "BOGO" || p.PromotionType == "BUY_X_GET_Y") && PromotionValidator.IsPromotionActive(p)).ToList();

            foreach (var bogo in bogoPromotions)
            {
                bool isBogoConditionMatched = true;
                if (bogo.Rules != null && bogo.Rules.Any())
                {
                    var cond = bogo.Rules.First();
                    if (!PromotionValidator.IsValidMinOrder(cond, cartTotal) ||
                        !PromotionValidator.IsValidCustomerGroup(cond, request.CustomerGroupID))
                    {
                        isBogoConditionMatched = false;
                    }
                }

                if (isBogoConditionMatched)
                {
                    // GỌI SANG HANDLER Ở ĐÂY
                    var gifts = BuyXGetYHandler.Process(bogo, request.Items);

                    if (gifts.Any())
                    {
                        result.GiftItems.AddRange(gifts);
                        result.AppliedPromotions.Add(new AppliedPromotionDto { PromotionID = bogo.PromotionID, PromotionName = bogo.PromotionName, DiscountAmount = 0 });
                    }
                }
            }

            // BƯỚC 5: TRẢ VỀ KẾT QUẢ
            result.HasDiscount = result.TotalDiscount > 0 || result.GiftItems.Any();
            return result;
        }

        // --- FIX LỖI 2: THUẬT TOÁN BOGO MỚI DỰA TRÊN VẾ TRÁI / VẾ PHẢI ---
        private List<CartItemDto> ProcessBOGO(PromotionDto bogo, List<CartItemDto> cartItems)
        {
            var gifts = new List<CartItemDto>();
            if (bogo.Products == null || !bogo.Products.Any()) return gifts;

            // Bóc tách Vế Trái (Điều kiện mua) và Vế Phải (Hàng tặng)
            var requiredProducts = bogo.Products.Where(p => !p.IsGiftProduct).ToList();
            var giftProducts = bogo.Products.Where(p => p.IsGiftProduct).ToList();

            if (!requiredProducts.Any() || !giftProducts.Any()) return gifts;

            bool isEligible = true;

            // Kiểm tra: Khách phải mua ĐỦ tất cả các mặt hàng yêu cầu với số lượng >= RequiredQuantity
            foreach (var req in requiredProducts)
            {
                var cartItem = cartItems.FirstOrDefault(c => c.ProductID == req.ProductID);
                if (cartItem == null || cartItem.Quantity < req.RequiredQuantity)
                {
                    isEligible = false;
                    break;
                }
            }

            // Nếu đủ điều kiện -> Xuất quà tặng
            if (isEligible)
            {
                foreach (var gift in giftProducts)
                {
                    gifts.Add(new CartItemDto
                    {
                        ProductID = gift.ProductID,
                        Quantity = gift.FreeQuantity ?? 1,
                        UnitPrice = 0 // Giá 0đ
                    });
                }
            }

            return gifts;
        }

        public async Task DeductPromotionUsageAsync(long promotionId)
        {
            try
            {
                await _repository.DeductPromotionUsageAsync(promotionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi nhận sử dụng KM {0}", promotionId);
                throw;
            }
        }
    }
}