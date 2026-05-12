using System;
using System.Collections.Generic;

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
        public string PromotionCode { get; set; } = string.Empty;
        public string PromotionName { get; set; } = string.Empty;
        public string PromotionType { get; set; } = string.Empty;   // DISCOUNT / BOGO
        public string Status { get; set; } = string.Empty;          // DRAFT / ACTIVE / EXPIRED
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public bool IsStackable { get; set; }
        public int Priority { get; set; }
        public string? Note { get; set; }

        public List<PromotionRuleDto> Rules { get; set; } = new();
        public List<PromotionProductDto> Products { get; set; } = new();
    }
}