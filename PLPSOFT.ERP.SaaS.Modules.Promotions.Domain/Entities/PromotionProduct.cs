namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities;

public class PromotionProduct
{
    public long PromotionProductId { get; set; }

    public long PromotionId { get; set; }

    public long ProductId { get; set; }

    public decimal? RequiredQuantity { get; set; }

    public decimal? FreeQuantity { get; set; }

    public bool IsGiftProduct { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation Properties

    public Promotion Promotion { get; set; } = null!;
}