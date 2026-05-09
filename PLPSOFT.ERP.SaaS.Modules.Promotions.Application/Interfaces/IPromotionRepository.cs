using System.Collections.Generic;
using System.Threading.Tasks;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces
{
    /// <summary>
    /// Hợp đồng truy vấn DB cho module Promotions.
    /// Application/Engine chỉ gọi qua interface này — không biết DB là gì.
    /// Minh Nhựt (Engine) sẽ inject interface này để dùng.
    /// </summary>
    public interface IPromotionRepository
    {
        // ─── ĐỌC DỮ LIỆU ────────────────────────────────────────────

        /// <summary>
        /// Lấy toàn bộ KM đang ACTIVE của 1 công ty (kèm Rules và Products).
        /// Engine dùng để lọc KM phù hợp với giỏ hàng.
        /// </summary>
        Task<List<PromotionDto>> GetActivePromotionsAsync(long companyID);

        /// <summary>
        /// Lấy tất cả KM (mọi trạng thái) để hiển thị màn hình quản lý CRUD.
        /// Web layer (Nhựt Huỳnh) dùng hàm này.
        /// </summary>
        Task<List<PromotionDto>> GetAllAsync(long companyID);

        /// <summary>
        /// Lấy 1 KM theo ID (kèm Rules và Products).
        /// </summary>
        Task<PromotionDto?> GetByIdAsync(long promotionID);

        /// <summary>
        /// Lấy 1 KM theo mã code trong cùng công ty.
        /// </summary>
        Task<PromotionDto?> GetByCodeAsync(long companyID, string promotionCode);

        /// <summary>
        /// Kiểm tra mã KM có bị trùng trong cùng công ty không.
        /// Dùng khi tạo mới để validate trước khi lưu.
        /// </summary>
        Task<bool> IsCodeExistsAsync(long companyID, string promotionCode);

        // ─── GHI DỮ LIỆU ────────────────────────────────────────────

        /// <summary>
        /// Tạo mới 1 chương trình KM. Trả về PromotionID vừa tạo.
        /// </summary>
        Task<long> CreateAsync(PromotionDto dto, long companyID, long createdByUserID);

        /// <summary>
        /// Cập nhật thông tin KM (tên, ngày, ghi chú...).
        /// </summary>
        Task UpdateAsync(PromotionDto dto);

        /// <summary>
        /// Xóa mềm KM: set IsActive = false, không xóa vật lý khỏi DB.
        /// </summary>
        Task DeactivateAsync(long promotionID);

        // ─── DÙNG CHO ENGINE (Minh Nhựt gọi) ────────────────────────

        /// <summary>
        /// Tăng CurrentUsage lên 1 sau khi KM được áp dụng thành công.
        /// Dùng ExecuteUpdate để tránh race condition khi nhiều đơn cùng lúc.
        /// </summary>
        Task DeductUsageAsync(long promotionID);
    }
}