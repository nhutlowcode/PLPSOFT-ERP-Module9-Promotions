using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities
{
    [Table("PromotionProducts", Schema = "promotions")]
    public class PromotionProduct
    {
        [Key]
        [Column("PromotionProductID")]
        public long PromotionProductId { get; set; }

        [Required]
        [Column("PromotionID")]
        public long PromotionId { get; set; }

        [Required]
        [Column("ProductID")]
        public long ProductId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? RequiredQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? FreeQuantity { get; set; }

        [Required]
        public bool IsGiftProduct { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; }

        // ═══════════════════════════════════════════════
        // Navigation Properties
        // ═══════════════════════════════════════════════
        [ForeignKey("PromotionId")]
        public virtual Promotion Promotion { get; set; } = null!;
    }
}