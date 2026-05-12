using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces;

namespace PLPSOFT.ERP.SaaS.Web.Areas.Promotions.Controllers
{
    [Area("Promotions")]
    public class CampaignsController : Controller
    {
        private readonly IPromotionRepository _repository;

        public CampaignsController(IPromotionRepository repository)
        {
            _repository = repository;
        }

        // GET: /Promotions/Campaigns
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                long companyID = 1;  // TODO: Get từ user context
                var promotions = await _repository.GetAllByCompanyAsync(companyID);

                // --- CƠ CHẾ LAZY UPDATE: Tự động cập nhật trạng thái Hết hạn ---
                var currentDate = DateTime.Now.Date; // Chỉ lấy ngày hiện tại (bỏ qua giờ phút) để so sánh chuẩn xác
                bool hasExpiredUpdates = false;

                foreach (var promo in promotions)
                {
                    // Nếu đang chạy mà ngày kết thúc nhỏ hơn hôm nay -> Hết hạn
                    if (promo.Status == "ACTIVE" && promo.EndDate.HasValue && promo.EndDate.Value.Date < currentDate)
                    {
                        // Cập nhật ngầm dưới Database
                        await _repository.UpdateStatusAsync(promo.PromotionID, "EXPIRED");
                        // Cập nhật đối tượng hiện tại để hiển thị ra View ngay lập tức
                        promo.Status = "EXPIRED";
                        hasExpiredUpdates = true;
                    }
                }

                // Nếu có cập nhật trạng thái, có thể bạn muốn ghi log ở đây (tùy chọn)
                // if (hasExpiredUpdates) { _logger.LogInformation("Đã quét và cập nhật các KM hết hạn."); }
                // ----------------------------------------------------------------

                return View(promotions);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải danh sách KM: {ex.Message}";
                return View(new List<PromotionDto>());
            }
        }

        // GET: /Promotions/Campaigns/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                long companyId = 1; // TODO: Get từ user context

                var dto = new PromotionDto
                {
                    PromotionCode = string.Empty,
                    PromotionName = string.Empty,
                    PromotionType = "DISCOUNT",
                    Status = "DRAFT",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30),
                    Priority = 0,
                    IsStackable = false,
                    Note = string.Empty,
                    Rules = new List<PromotionRuleDto>
                    {
                        new PromotionRuleDto
                        {
                            RuleType = "MIN_ORDER",
                            DiscountType = "PERCENT",
                            DiscountValue = 0
                        }
                    },
                    Products = new List<PromotionProductDto>
                    {
                        new PromotionProductDto
                        {
                            ProductID = 0,
                            IsGiftProduct = false
                        }
                    }
                };

                // Load ViewBag
                ViewBag.Branches = await _repository.GetBranchesAsync(companyId);
                ViewBag.PromotionTypes = await _repository.GetTypeValuesAsync("PROMOTION_TYPE");
                ViewBag.RuleTypes = await _repository.GetTypeValuesAsync("PROMO_RULE_TYPE");
                ViewBag.DiscountTypes = await _repository.GetTypeValuesAsync("DISCOUNT_TYPE");
                ViewBag.CustomerGroups = await _repository.GetCustomerGroupsAsync(companyId);
                ViewBag.Categories = await _repository.GetCategoriesAsync(companyId);
                ViewBag.Products = await _repository.GetProductsAsync(companyId);

                return View(dto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Promotions/Campaigns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionDto dto)
        {
            long companyId = 1; // TODO: Get từ user context
            try
            {
                dto.CompanyID = companyId;
                string? error = null;

                if (string.IsNullOrWhiteSpace(dto.Status))
                {
                    dto.Status = "DRAFT";
                }

                var isValid = ModelState.IsValid;
                if (isValid)
                {
                    isValid = TryValidatePromotion(dto, out error);
                }

                if (!isValid)
                {
                    TempData["ErrorMessage"] = error ?? "Dữ liệu không hợp lệ.";
                    await LoadViewBagsAsync(companyId);
                    return View(dto);
                }

                long promotionID = await _repository.CreatePromotionAsync(dto);
                TempData["SuccessMessage"] = $"Tạo chương trình khuyến mãi thành công (Mã: {dto.PromotionCode}).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Bóc tách lỗi tận gốc từ Database
                string exactError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                // Kiểm tra nếu lỗi là do vi phạm ràng buộc Unique (Trùng mã)
                if (exactError.Contains("UNIQUE") || exactError.Contains("duplicate") || exactError.Contains("Violation of UNIQUE KEY"))
                {
                    TempData["ErrorMessage"] = $"Lỗi: Mã khuyến mãi '{dto.PromotionCode}' đã tồn tại trong hệ thống. Vui lòng nhập một mã khác!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi khi lưu dữ liệu: {exactError}";
                }

                // CỰC KỲ QUAN TRỌNG: Load lại các danh sách (Dropdown) và trả về View cũ 
                // để người dùng không bị mất trắng dữ liệu vừa nhập
                await LoadViewBagsAsync(companyId);
                return View(dto);
            }
        }

        // GET: /Promotions/Campaigns/Edit/1
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                long companyId = 1; // TODO: Get từ user context
                var promotion = await _repository.GetPromotionByIdAsync(id);

                if (promotion is null)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy KM ID {id}.";
                    return RedirectToAction(nameof(Index));
                }

                // --- CƠ CHẾ LAZY UPDATE: Kiểm tra và chốt trạng thái trước khi mở Form ---
                var currentDate = DateTime.Now.Date;
                if (promotion.Status == "ACTIVE" && promotion.EndDate.HasValue && promotion.EndDate.Value.Date < currentDate)
                {
                    await _repository.UpdateStatusAsync(promotion.PromotionID, "EXPIRED");
                    promotion.Status = "EXPIRED"; // Đổi trạng thái để View nhận diện là form Readonly
                }
                // ----------------------------------------------------------------

                // Load ViewBag thông qua hàm Helper đã viết ở cuối file
                await LoadViewBagsAsync(companyId);

                return View(promotion);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Promotions/Campaigns/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PromotionDto dto)
        {
            long companyId = 1; // TODO: Get từ user context
            try
            {
                dto.CompanyID = companyId;
                string? error = null;

                var isValid = ModelState.IsValid;
                if (isValid)
                {
                    isValid = TryValidatePromotion(dto, out error);
                }

                if (!isValid)
                {
                    TempData["ErrorMessage"] = error ?? "Dữ liệu không hợp lệ.";
                    await LoadViewBagsAsync(companyId);
                    return View(dto);
                }

                await _repository.UpdatePromotionAsync(dto);
                TempData["SuccessMessage"] = "Cập nhật chương trình khuyến mãi thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Bóc tách lỗi tận gốc từ Database
                string exactError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                // Mặc dù mã KM bị khóa (readonly) ở UI, nhưng vẫn chặn Unique để an toàn tuyệt đối ở tầng Backend
                if (exactError.Contains("UNIQUE") || exactError.Contains("duplicate") || exactError.Contains("Violation of UNIQUE KEY"))
                {
                    TempData["ErrorMessage"] = $"Lỗi: Mã khuyến mãi '{dto.PromotionCode}' bị xung đột với một chiến dịch khác trong hệ thống.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi khi lưu dữ liệu cập nhật: {exactError}";
                }

                // CỰC KỲ QUAN TRỌNG: Load lại các danh sách (Dropdown) và trả về View cũ 
                // để người dùng không bị mất trắng dữ liệu họ vừa chỉnh sửa
                await LoadViewBagsAsync(companyId);
                return View(dto);
            }
        }

        // POST: /Promotions/Campaigns/Delete/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _repository.DeletePromotionAsync(id);
                TempData["SuccessMessage"] = "Xóa KM thành công.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Promotions/Campaigns/Activate/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(long id)
        {
            try
            {
                await _repository.UpdateStatusAsync(id, "ACTIVE");
                TempData["SuccessMessage"] = "Kích hoạt KM thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Promotions/Campaigns/Deactivate/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(long id)
        {
            try
            {
                await _repository.UpdateStatusAsync(id, "DRAFT");
                TempData["SuccessMessage"] = "Tắt KM thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadViewBagsAsync(long companyId)
        {
            ViewBag.Branches = await _repository.GetBranchesAsync(companyId);
            ViewBag.PromotionTypes = await _repository.GetTypeValuesAsync("PROMOTION_TYPE");
            ViewBag.RuleTypes = await _repository.GetTypeValuesAsync("PROMO_RULE_TYPE");
            ViewBag.DiscountTypes = await _repository.GetTypeValuesAsync("DISCOUNT_TYPE");
            ViewBag.CustomerGroups = await _repository.GetCustomerGroupsAsync(companyId);
            ViewBag.Categories = await _repository.GetCategoriesAsync(companyId);
            ViewBag.Products = await _repository.GetProductsAsync(companyId);
        }

        private static bool TryValidatePromotion(PromotionDto dto, out string? error)
        {
            error = null;

            // 1. KIỂM TRA THÔNG TIN CƠ BẢN (CHUNG)
            if (string.IsNullOrWhiteSpace(dto.PromotionCode) || string.IsNullOrWhiteSpace(dto.PromotionName))
            {
                error = "Mã KM và tên KM không được trống.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.PromotionType))
            {
                error = "Vui lòng chọn loại khuyến mãi.";
                return false;
            }

            if (dto.StartDate == default)
            {
                error = "Ngày bắt đầu không hợp lệ.";
                return false;
            }

            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            {
                error = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.";
                return false;
            }

            bool isBogo = dto.PromotionType == "BUY_X_GET_Y" || dto.PromotionType == "BOGO";

            // 2. KIỂM TRA QUY TẮC (RULES) THEO RẼ NHÁNH
            if (!isBogo)
            {
                // Nhánh A: Dành cho GIẢM GIÁ (Phải có ít nhất 1 mức giảm và giá trị phải > 0)
                if (dto.Rules == null || !dto.Rules.Any())
                {
                    error = "Chương trình Giảm giá phải thiết lập ít nhất 1 mức ưu đãi (Trị giá giảm).";
                    return false;
                }

                foreach (var rule in dto.Rules)
                {
                    if (string.IsNullOrWhiteSpace(rule.RuleType) || string.IsNullOrWhiteSpace(rule.DiscountType))
                    {
                        error = "Loại điều kiện và Kiểu giảm giá không được để trống.";
                        return false;
                    }

                    if (rule.DiscountValue <= 0)
                    {
                        error = "Trị giá giảm (DiscountValue) phải lớn hơn 0.";
                        return false;
                    }

                    // Ràng buộc riêng theo từng loại điều kiện
                    if (rule.RuleType == "MIN_ORDER" && (!rule.MinOrderAmount.HasValue || rule.MinOrderAmount <= 0))
                    {
                        error = "Điều kiện 'Tổng đơn' yêu cầu số tiền tối thiểu phải > 0.";
                        return false;
                    }

                    if ((rule.RuleType == "MIN_QTY" || rule.RuleType == "MIN_QUANTITY") &&
                        (!rule.MinQuantity.HasValue || rule.MinQuantity <= 0))
                    {
                        error = "Điều kiện 'Số lượng SP' yêu cầu số lượng tối thiểu phải > 0.";
                        return false;
                    }

                    if (rule.RuleType == "CUSTOMER_GROUP" && !rule.CustomerGroupID.HasValue)
                    {
                        error = "Điều kiện 'Nhóm khách hàng' yêu cầu phải chọn một nhóm khách cụ thể.";
                        return false;
                    }

                    if (rule.RuleType == "CATEGORY" && !rule.CategoryID.HasValue)
                    {
                        error = "Điều kiện 'Ngành hàng' yêu cầu phải chọn một ngành hàng cụ thể.";
                        return false;
                    }
                }
            }
            else
            {
                // Nhánh B: Dành cho BOGO (Rules đóng vai trò là "Điều kiện kích hoạt" - Có thể rỗng nếu áp dụng mọi đơn)
                if (dto.Rules != null && dto.Rules.Any())
                {
                    foreach (var rule in dto.Rules)
                    {
                        if (string.IsNullOrWhiteSpace(rule.RuleType))
                        {
                            error = "Loại điều kiện kích hoạt không được để trống.";
                            return false;
                        }

                        // KHÔNG KIỂM TRA DiscountValue VÌ BOGO KHÔNG DÙNG FIELD NÀY

                        if (rule.RuleType == "MIN_ORDER" && (!rule.MinOrderAmount.HasValue || rule.MinOrderAmount <= 0))
                        {
                            error = "Điều kiện 'Tổng đơn' yêu cầu số tiền tối thiểu phải > 0.";
                            return false;
                        }

                        if ((rule.RuleType == "MIN_QTY" || rule.RuleType == "MIN_QUANTITY") &&
                            (!rule.MinQuantity.HasValue || rule.MinQuantity <= 0))
                        {
                            error = "Điều kiện 'Số lượng SP' yêu cầu số lượng tối thiểu phải > 0.";
                            return false;
                        }

                        if (rule.RuleType == "CUSTOMER_GROUP" && !rule.CustomerGroupID.HasValue)
                        {
                            error = "Điều kiện 'Nhóm khách hàng' yêu cầu phải chọn một nhóm khách cụ thể.";
                            return false;
                        }

                        if (rule.RuleType == "CATEGORY" && !rule.CategoryID.HasValue)
                        {
                            error = "Điều kiện 'Ngành hàng' yêu cầu phải chọn một ngành hàng cụ thể.";
                            return false;
                        }
                    }
                }
            }

            // 3. KIỂM TRA SẢN PHẨM (PRODUCTS) THEO RẼ NHÁNH
            if (isBogo)
            {
                // Nhánh B: BOGO (Bắt buộc có vế mua và vế tặng)
                if (dto.Products == null || !dto.Products.Any(p => !p.IsGiftProduct) || !dto.Products.Any(p => p.IsGiftProduct))
                {
                    error = "Chương trình Mua X Tặng Y phải có ít nhất 1 sản phẩm khách mua và 1 sản phẩm tặng.";
                    return false;
                }

                foreach (var product in dto.Products)
                {
                    if (product.ProductID <= 0)
                    {
                        error = "Vui lòng chọn sản phẩm hợp lệ trong danh sách BOGO.";
                        return false;
                    }

                    if (product.IsGiftProduct && (!product.FreeQuantity.HasValue || product.FreeQuantity <= 0))
                    {
                        error = "Sản phẩm tặng miễn phí phải có số lượng > 0.";
                        return false;
                    }

                    if (!product.IsGiftProduct && (!product.RequiredQuantity.HasValue || product.RequiredQuantity <= 0))
                    {
                        error = "Sản phẩm yêu cầu mua phải có số lượng > 0.";
                        return false;
                    }
                }
            }
            else
            {
                // Nhánh A: Giảm giá (Sản phẩm là tùy chọn - rỗng thì áp dụng toàn sàn)
                if (dto.Products != null && dto.Products.Any())
                {
                    foreach (var product in dto.Products)
                    {
                        if (product.ProductID <= 0)
                        {
                            error = "Vui lòng chọn sản phẩm áp dụng giảm giá hợp lệ.";
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
