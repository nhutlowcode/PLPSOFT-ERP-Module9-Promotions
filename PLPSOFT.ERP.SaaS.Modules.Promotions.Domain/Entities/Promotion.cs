using System;
using System.Collections.Generic;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities
{
    public class Promotion
    {
        public long PromotionId { get; set; }
        public long CompanyId { get; set; }
        public long? BranchId { get; set; }
        public string PromotionCode { get; set; } = string.Empty;
        public string PromotionName { get; set; } = string.Empty;
        public long PromotionTypeId { get; set; }
        public long PromotionStatusId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public int Priority { get; set; }
        public bool IsStackable { get; set; }
        public bool IsActive { get; set; }
        public long CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
        public string? ExtraData { get; set; }

        // Navigation properties (Quan hệ giữa các bảng)

        public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();
    }
}