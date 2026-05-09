using System;
using System.Collections.Generic;
using System.Linq;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    public static class PromotionValidator
    {
        /// <summary>
        /// Kiểm tra KM còn hiệu lực không.
        /// Status == "ACTIVE", StartDate <= now, EndDate chưa qua, CurrentUsage < MaxUsage.
        /// </summary>
        public static bool IsPromotionActive(PromotionDto promo)
        {
            if (promo.Status != "ACTIVE") return false;

            var now = DateTime.Now;
            if (now < promo.StartDate) return false;
            if (promo.EndDate.HasValue && now > promo.EndDate.Value) return false;
            if (promo.MaxUsage.HasValue && promo.CurrentUsage >= promo.MaxUsage) return false;

            return true;
        }

        /// <summary>
        /// Kiểm tra tổng tiền đơn hàng có đạt tối thiểu không.
        /// Nếu MinOrderAmount null → true (không giới hạn).
        /// </summary>
        public static bool IsValidMinOrder(PromotionRuleDto rule, decimal cartTotal)
        {
            if (!rule.MinOrderAmount.HasValue) return true;
            return cartTotal >= rule.MinOrderAmount.Value;
        }

        /// <summary>
        /// Kiểm tra số lượng sản phẩm có đạt tối thiểu không.
        /// Nếu MinQuantity null → true (không giới hạn).
        /// </summary>
        public static bool IsValidQuantity(PromotionRuleDto rule, decimal totalQty)
        {
            if (!rule.MinQuantity.HasValue) return true;
            return totalQty >= rule.MinQuantity.Value;
        }

        /// <summary>
        /// Kiểm tra KM có áp dụng cho nhóm KH này không.
        /// Nếu CustomerGroupID null → true (không giới hạn nhóm KH).
        /// </summary>
        public static bool IsValidCustomerGroup(PromotionRuleDto rule, long? cartCustomerGroupID)
        {
            if (!rule.CustomerGroupID.HasValue) return true;
            return rule.CustomerGroupID == cartCustomerGroupID;
        }

        /// <summary>
        /// Kiểm tra KM có áp dụng cho danh mục sản phẩm trong giỏ không.
        /// Nếu CategoryID null → true (không giới hạn danh mục).
        /// Nếu giỏ không có CategoryID nào → false (vì rule yêu cầu danh mục cụ thể).
        /// </summary>
        public static bool IsValidCategory(PromotionRuleDto rule, List<long?> cartCategoryIDs)
        {
            if (!rule.CategoryID.HasValue) return true;
            if (!cartCategoryIDs.Any()) return false;
            return cartCategoryIDs.Contains(rule.CategoryID.Value);
        }
    }
}
