using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities
{
    [Table("Promotions", Schema = "promotions")]
    public class Promotion
    {
        [Key]
        [Column("PromotionID")]
        public long PromotionId { get; set; }

        [Required]
        [Column("CompanyID")]
        public long CompanyId { get; set; }

        [Column("BranchID")]
        public long? BranchId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PromotionCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PromotionName { get; set; } = string.Empty;

        [Required]
        [Column("PromotionTypeID")]
        public long PromotionTypeId { get; set; }

        [Required]
        [Column("PromotionStatusID")]
        public long PromotionStatusId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? MaxUsage { get; set; }

        [Required]
        public int CurrentUsage { get; set; }

        [Required]
        public int Priority { get; set; }

        [Required]
        public bool IsStackable { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        [Column("CreatedByUserID")]
        public long CreatedByUserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        // Navigation properties (Quan hệ 1-N)
        public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();

        public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
    }
}