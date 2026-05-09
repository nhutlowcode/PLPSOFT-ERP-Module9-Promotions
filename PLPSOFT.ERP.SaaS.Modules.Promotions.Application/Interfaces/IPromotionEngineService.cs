using System.Threading.Tasks;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces
{
    /// <summary>
    /// Engine tính toán KM tốt nhất cho đơn hàng.
    /// Module Sales sẽ gọi khi:
    ///   - Thêm hàng vào giỏ: CalculateBestDiscountAsync()
    ///   - Invoice xác nhận: DeductPromotionUsageAsync()
    /// </summary>
    public interface IPromotionEngineService
    {
        /// <summary>
        /// Tính discount tốt nhất cho giỏ hàng hiện tại.
        /// Xử lý logic: Stackable, NonStackable (lấy max), BOGO, Gift items.
        /// </summary>
        /// <param name="request">Dữ liệu giỏ hàng từ Module Sales</param>
        /// <returns>Kết quả discount: tổng tiền, KM áp dụng, hàng tặng</returns>
        Task<CartDiscountResult> CalculateBestDiscountAsync(CartRequest request);

        /// <summary>
        /// Ghi nhận sử dụng KM (tăng CurrentUsage lên 1).
        /// Gọi sau khi Invoice confirmed để tránh trùng lặp.
        /// </summary>
        /// <param name="promotionId">ID của KM</param>
        Task DeductPromotionUsageAsync(long promotionId);
    }
}
