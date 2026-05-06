namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities;

public class PromotionRule
{
    public long RuleId { get; set; }

    public long PromotionId { get; set; }

    public long RuleTypeId { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public decimal? MinQuantity { get; set; }

    public long DiscountTypeId { get; set; }

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation Properties

    public Promotion Promotion { get; set; } = null!;
}