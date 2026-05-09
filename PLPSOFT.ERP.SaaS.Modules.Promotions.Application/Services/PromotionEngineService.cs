using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Services;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Services
{
    /// <summary>
    /// Triển khai IPromotionEngineService.
    /// Tính toán discount tốt nhất theo thứ tự ưu tiên.
    /// </summary>
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

        /// <summary>
        /// BƯỚC 1: Lấy dữ liệu từ request
        /// BƯỚC 2: Validate & tính discount từng Promotion
        /// BƯỚC 3: Xử lý Stackable (cộng dồn) vs NonStackable (lấy max)
        /// BƯỚC 4: Xử lý BOGO (Buy X Get Y)
        /// BƯỚC 5: Trả về kết quả
        /// </summary>
        public async Task<CartDiscountResult> CalculateBestDiscountAsync(CartRequest request)
        {
            var result = new CartDiscountResult();

            // Kiểm tra dữ liệu input
            if (request.Items == null || !request.Items.Any())
            {
                _logger.LogWarning("CartRequest không có Items");
                return result;
            }

            // ═══════════════════════════════════════════════════════════
            // BƯỚC 1: LẤY DỮ LIỆU
            // ═══════════════════════════════════════════════════════════
            var promos = await _repository.GetActivePromotionsAsync(request.CompanyID);

            // Lọc theo BranchID nếu có
            if (request.BranchID.HasValue)
            {
                promos = promos
                    .Where(p => !p.BranchId.HasValue || p.BranchId == request.BranchID.Value)
                    .ToList();
            }

            if (!promos.Any())
            {
                _logger.LogInformation("Không có KM nào active cho CompanyID {0}", request.CompanyID);
                return result;
            }

            var cartTotal = request.Items.Sum(i => i.LineTotal);
            var cartQty = request.Items.Sum(i => i.Quantity);
            var cartCategoryIDs = request.Items
                .Select(i => i.CategoryID)
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "Tính KM: CompanyID={0}, CartTotal={1}, CartQty={2}, Categories={3}",
                request.CompanyID, cartTotal, cartQty, string.Join(",", cartCategoryIDs));

            // ═══════════════════════════════════════════════════════════
            // BƯỚC 2: VALIDATE & TÍNH DISCOUNT TỪNG PROMOTION
            // ═══════════════════════════════════════════════════════════
            var stackablePromotions = new List<(PromotionDto promo, decimal discount)>();
            var nonStackablePromotions = new List<(PromotionDto promo, decimal discount)>();

            foreach (var promo in promos)
            {
                // 2a. Kiểm tra KM còn hiệu lực
                if (!PromotionValidator.IsPromotionActive(promo))
                {
                    _logger.LogDebug("KM {0} không active", promo.PromotionCode);
                    continue;
                }

                // Duyệt từng rule để tìm rule hợp lệ
                bool ruleMatched = false;
                decimal ruleDiscount = 0;

                foreach (var rule in promo.Rules)
                {
                    // 2b. Kiểm tra tối thiểu đơn hàng
                    if (!PromotionValidator.IsValidMinOrder(rule, cartTotal))
                    {
                        _logger.LogDebug("Rule {0} không đạt MinOrderAmount", rule.RuleID);
                        continue;
                    }

                    // 2c. Kiểm tra tối thiểu số lượng
                    if (!PromotionValidator.IsValidQuantity(rule, cartQty))
                    {
                        _logger.LogDebug("Rule {0} không đạt MinQuantity", rule.RuleID);
                        continue;
                    }

                    // 2d. Kiểm tra nhóm khách hàng
                    if (!PromotionValidator.IsValidCustomerGroup(rule, request.CustomerGroupID))
                    {
                        _logger.LogDebug("Rule {0} không phù hợp CustomerGroup", rule.RuleID);
                        continue;
                    }

                    // 2e. Kiểm tra danh mục sản phẩm
                    if (!PromotionValidator.IsValidCategory(rule, cartCategoryIDs))
                    {
                        _logger.LogDebug("Rule {0} không phù hợp Category", rule.RuleID);
                        continue;
                    }

                    // ═══════════════════════════════════════════════════════════
                    // Tính discount
                    // ═══════════════════════════════════════════════════════════
                    if (rule.DiscountType == "PERCENT")
                    {
                        // PERCENT: Chiết khấu phần trăm
                        ruleDiscount = cartTotal * rule.DiscountValue / 100m;

                        // Giới hạn tối đa nếu có
                        if (rule.MaxDiscountAmount.HasValue)
                        {
                            ruleDiscount = Math.Min(ruleDiscount, rule.MaxDiscountAmount.Value);
                        }
                    }
                    else if (rule.DiscountType == "AMOUNT")
                    {
                        // AMOUNT: Chiết khấu cố định
                        ruleDiscount = rule.DiscountValue;
                    }

                    ruleMatched = true;
                    _logger.LogInformation(
                        "KM {0} - Rule {1} hợp lệ, Discount={2}",
                        promo.PromotionCode, rule.RuleID, ruleDiscount);

                    // 1 rule hợp lệ là đủ kích hoạt KM
                    break;
                }

                if (!ruleMatched || ruleDiscount <= 0)
                {
                    _logger.LogDebug("KM {0} không có rule hợp lệ", promo.PromotionCode);
                    continue;
                }

                // Thêm vào danh sách stackable hoặc non-stackable
                if (promo.IsStackable)
                {
                    stackablePromotions.Add((promo, ruleDiscount));
                }
                else
                {
                    nonStackablePromotions.Add((promo, ruleDiscount));
                }
            }

            // ═══════════════════════════════════════════════════════════
            // BƯỚC 3: XỬ LÝ STACKABLE vs NON-STACKABLE
            // ═══════════════════════════════════════════════════════════

            // Stackable: cộng dồn TẤT CẢ
            foreach (var (promo, discount) in stackablePromotions)
            {
                result.TotalDiscount += discount;
                result.AppliedPromotions.Add(new AppliedPromotionDto
                {
                    PromotionID = promo.PromotionID,
                    PromotionName = promo.PromotionName,
                    DiscountAmount = discount
                });
            }

            // Non-stackable: chỉ lấy 1 cái discount CAO NHẤT
            if (nonStackablePromotions.Any())
            {
                var bestNonStackable = nonStackablePromotions
                    .OrderByDescending(x => x.discount)
                    .First();

                result.TotalDiscount += bestNonStackable.discount;
                result.AppliedPromotions.Add(new AppliedPromotionDto
                {
                    PromotionID = bestNonStackable.promo.PromotionID,
                    PromotionName = bestNonStackable.promo.PromotionName,
                    DiscountAmount = bestNonStackable.discount
                });

                _logger.LogInformation(
                    "NonStackable KM: lấy {0} với discount {1}",
                    bestNonStackable.promo.PromotionCode, bestNonStackable.discount);
            }

            // ═══════════════════════════════════════════════════════════
            // BƯỚC 4: XỬ LÝ BOGO (BUY X GET Y)
            // ═══════════════════════════════════════════════════════════
            var bogoPromotions = promos
                .Where(p => p.PromotionType == "BOGO" && PromotionValidator.IsPromotionActive(p))
                .ToList();

            if (bogoPromotions.Any())
            {
                foreach (var bogo in bogoPromotions)
                {
                    var gifts = BuyXGetYHandler.Process(bogo, request.Items);
                    result.GiftItems.AddRange(gifts);

                    _logger.LogInformation(
                        "BOGO KM {0}: {1} hàng tặng",
                        bogo.PromotionCode, gifts.Count);
                }
            }

            // ═══════════════════════════════════════════════════════════
            // BƯỚC 5: TRẢ VỀ KẾT QUẢ
            // ═══════════════════════════════════════════════════════════
            result.HasDiscount = result.TotalDiscount > 0 || result.GiftItems.Any();

            _logger.LogInformation(
                "Kết quả tính KM: HasDiscount={0}, TotalDiscount={1}, AppliedCount={2}, GiftCount={3}",
                result.HasDiscount, result.TotalDiscount, result.AppliedPromotions.Count, result.GiftItems.Count);

            return result;
        }
        /*
        /// <summary>
        /// Xử lý BOGO: Lấy hàng tặng dựa trên Products của KM.
        /// </summary>
        private List<CartItemDto> ProcessBOGO(PromotionDto bogo, List<CartItemDto> cartItems)
        {
            var gifts = new List<CartItemDto>();

            // Kiểm tra KM có sản phẩm định nghĩa không
            if (!bogo.Products.Any())
            {
                _logger.LogWarning("BOGO KM {0} không có Products định nghĩa", bogo.PromotionCode);
                return gifts;
            }

            foreach (var promoProduct in bogo.Products)
            {
                // Tìm sản phẩm trong giỏ hàng
                var cartItem = cartItems
                    .FirstOrDefault(x => x.ProductID == promoProduct.ProductID);

                if (cartItem == null)
                {
                    _logger.LogDebug(
                        "BOGO KM {0}: Sản phẩm {1} không trong giỏ",
                        bogo.PromotionCode, promoProduct.ProductID);
                    continue;
                }

                // Kiểm tra số lượng điều kiện
                if (promoProduct.RequiredQuantity.HasValue &&
                    cartItem.Quantity < promoProduct.RequiredQuantity.Value)
                {
                    _logger.LogDebug(
                        "BOGO KM {0}: Không đạt RequiredQuantity ({1}/{2})",
                        bogo.PromotionCode, cartItem.Quantity, promoProduct.RequiredQuantity);
                    continue;
                }

                // Tạo CartItemDto cho hàng tặng (UnitPrice = 0)
                if (promoProduct.FreeQuantity.HasValue && promoProduct.FreeQuantity > 0)
                {
                    gifts.Add(new CartItemDto
                    {
                        ProductID = promoProduct.ProductID,
                        CategoryID = cartItem.CategoryID,
                        Quantity = promoProduct.FreeQuantity.Value,
                        UnitPrice = 0  // Hàng tặng không tính tiền
                    });

                    _logger.LogDebug(
                        "BOGO KM {0}: Tặng sản phẩm {1}, số lượng {2}",
                        bogo.PromotionCode, promoProduct.ProductID, promoProduct.FreeQuantity);
                }
            }

            return gifts;
        }
        */
        /// <summary>
        /// Ghi nhận sử dụng KM (tăng CurrentUsage).
        /// Gọi từ Module Sales sau khi Invoice confirmed.
        /// </summary>
        public async Task DeductPromotionUsageAsync(long promotionId)
        {
            try
            {
                await _repository.DeductUsageAsync(promotionId);
                _logger.LogInformation("Ghi nhận sử dụng KM {0}", promotionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi nhận sử dụng KM {0}", promotionId);
                throw;
            }
        }
    }
}
