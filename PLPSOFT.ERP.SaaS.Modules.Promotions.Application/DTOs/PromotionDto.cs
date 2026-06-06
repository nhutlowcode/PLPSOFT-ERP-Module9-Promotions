using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Đã bổ sung thư viện này

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    /// <summary>
    /// Ánh xạ từ Entity Promotion
    /// </summary>
    public class PromotionDto
    {
        public long PromotionID { get; set; }
        public long CompanyID { get; set; }
        public long? BranchId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi.")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được vượt quá 50 ký tự.")]
        public string PromotionCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên chương trình.")]
        [StringLength(255, ErrorMessage = "Tên chương trình không được vượt quá 255 ký tự.")]
        public string PromotionName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại hình khuyến mãi.")]
        public string PromotionType { get; set; } = string.Empty;   // DISCOUNT / BOGO

        public string Status { get; set; } = string.Empty;          // DRAFT / ACTIVE / EXPIRED

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Giới hạn sử dụng không được là số âm.")]
        public int? MaxUsage { get; set; }

        public int CurrentUsage { get; set; }
        public bool IsStackable { get; set; }

        // Bổ sung Validate cho field Priority
        [Required(ErrorMessage = "Vui lòng nhập độ ưu tiên.")]
        [Range(1, int.MaxValue, ErrorMessage = "Độ ưu tiên phải là số nguyên dương lớn hơn 0.")]
        public int Priority { get; set; }

        public string? Note { get; set; }

        public List<PromotionRuleDto> Rules { get; set; } = new();
        public List<PromotionProductDto> Products { get; set; } = new();
    }
}