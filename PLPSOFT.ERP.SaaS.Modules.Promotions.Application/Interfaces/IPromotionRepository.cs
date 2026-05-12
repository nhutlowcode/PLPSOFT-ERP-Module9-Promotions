using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;


namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces
{
    /// <summary>
    /// Interface Repository cho Promotion.
    /// Xử lý tất cả truy vấn database.
    /// </summary>
    public interface IPromotionRepository
    {
        /// <summary>
        /// Lấy tất cả KM của một Company (không lọc status).
        /// Include: Rules, Products.
        /// Dùng để quản lý backend (xem toàn bộ).
        /// </summary>
        /// <param name="companyID">ID công ty</param>
        /// <returns>Danh sách toàn bộ KM</returns>
        Task<List<PromotionDto>> GetAllByCompanyAsync(long companyID);

        /// <summary>
        /// Lấy KM ACTIVE (còn hiệu lực) cho một Company & Branch.
        /// Lọc: IsActive=true, Status=ACTIVE, StartDate<=now, EndDate null hoặc >=now,
        ///      MaxUsage null hoặc CurrentUsage < MaxUsage,
        ///      BranchID null hoặc BranchID == branchID (nếu có)
        /// Include: Rules, Products.
        /// Dùng cho Engine tính discount (lấy KM áp dụng được).
        /// </summary>
        /// <param name="companyID">ID công ty</param>
        /// <param name="branchID">ID chi nhánh (optional)</param>
        /// <returns>Danh sách KM ACTIVE</returns>
        Task<List<PromotionDto>> GetActivePromotionsAsync(long companyID, long? branchID = null);

        /// <summary>
        /// Lấy 1 KM theo ID (KHÔNG lọc status).
        /// Dùng để sửa KM (cần lấy được DRAFT và EXPIRED cũng được).
        /// Include: Rules, Products.
        /// </summary>
        /// <param name="promotionID">ID KM</param>
        /// <returns>KM hoặc null nếu không tìm thấy</returns>
        Task<PromotionDto?> GetPromotionByIdAsync(long promotionID);

        /// <summary>
        /// Tạo mới 1 KM (insert Promotion + Rules + Products).
        /// Trả về PromotionID vừa tạo.
        /// </summary>
        /// <param name="dto">Dữ liệu KM từ form</param>
        /// <returns>ID KM vừa tạo</returns>
        Task<long> CreatePromotionAsync(PromotionDto dto);

        /// <summary>
        /// Cập nhật KM (update + xóa Rules/Products cũ → insert lại).
        /// Không được cập nhật: CompanyID, CreatedByUserID, CreatedAt, IsActive (dùng UpdateStatusAsync).
        /// </summary>
        /// <param name="dto">Dữ liệu KM sau khi edit</param>
        Task UpdatePromotionAsync(PromotionDto dto);

        /// <summary>
        /// Xóa KM thật.
        /// Điều kiện: CurrentUsage == 0 (chưa có đơn hàng nào dùng).
        /// Nếu CurrentUsage > 0 → throw InvalidOperationException:
        ///   "KM đã được sử dụng, không thể xóa. Hãy chuyển sang EXPIRED."
        /// ON DELETE CASCADE tự xóa Rules và Products.
        /// </summary>
        /// <param name="promotionID">ID KM cần xóa</param>
        /// <exception cref="InvalidOperationException">Nếu KM đã được sử dụng</exception>
        Task DeletePromotionAsync(long promotionID);

        /// <summary>
        /// Đổi trạng thái KM: DRAFT → ACTIVE → EXPIRED.
        /// ACTIVE  → IsActive = true, Status = ACTIVE
        /// EXPIRED → IsActive = false, Status = EXPIRED
        /// DRAFT   → IsActive = false, Status = DRAFT (nếu cần)
        /// </summary>
        /// <param name="promotionID">ID KM</param>
        /// <param name="newStatus">Trạng thái mới: "ACTIVE", "EXPIRED", "DRAFT"</param>
        Task UpdateStatusAsync(long promotionID, string newStatus);

        /// <summary>
        /// Tăng CurrentUsage lên 1 (khi đơn hàng confirm).
        /// Dùng transaction + UPDLOCK để tránh race condition.
        /// Nếu CurrentUsage >= MaxUsage → set IsActive=false, Status=EXPIRED.
        /// </summary>
        /// <param name="promotionID">ID KM</param>
        Task DeductPromotionUsageAsync(long promotionID);

        /// <summary>
        /// Lấy danh sách các chi nhánh của công ty.
        /// </summary>
        /// <param name="companyId">ID công ty</param>
        /// <returns>Danh sách chi nhánh</returns>
        Task<List<BranchDto>> GetBranchesAsync(long companyId);

        /// <summary>
        /// Lấy danh sách các giá trị hệ thống theo typeCode.
        /// </summary>
        /// <param name="typeCode">Mã loại (ví dụ: "PROMOTION_TYPE")</param>
        /// <returns>Danh sách giá trị hệ thống</returns>
        Task<List<SystemTypeValueDto>> GetTypeValuesAsync(string typeCode);

        /// <summary>
        /// Lấy danh sách nhóm khách hàng của một công ty.
        /// </summary>
        /// <param name="companyId">ID công ty</param>
        /// <returns>Danh sách nhóm khách hàng</returns>
        Task<List<CustomerGroupDto>> GetCustomerGroupsAsync(long companyId);

        /// <summary>
        /// Lấy danh sách danh mục sản phẩm của một công ty.
        /// </summary>
        /// <param name="companyId">ID công ty</param>
        /// <returns>Danh sách danh mục sản phẩm</returns>
        Task<List<ProductCategoryDto>> GetCategoriesAsync(long companyId);

        /// <summary>
        /// Lấy danh sách sản phẩm của một công ty.
        /// </summary>
        /// <param name="companyId">ID công ty</param>
        /// <returns>Danh sách sản phẩm</returns>
        Task<List<ProductDto>> GetProductsAsync(long companyId);
    }
}