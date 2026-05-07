using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities
{
    [Table("PromotionRules", Schema = "promotions")]
    public class PromotionRule
    {
        [Key]
        [Column("RuleID")]
        public long RuleId { get; set; }

        [Required]
        [Column("PromotionID")]
        public long PromotionId { get; set; }

        [Required]
        [Column("RuleTypeID")]
        public long RuleTypeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? MinQuantity { get; set; }

        [Column("CustomerGroupID")]
        public long? CustomerGroupId { get; set; }

        [Column("CategoryID")]
        public long? CategoryId { get; set; }

        [Required]
        [Column("DiscountTypeID")]
        public long DiscountTypeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PromotionId")]
        public virtual Promotion Promotion { get; set; } = null!;
    }
}