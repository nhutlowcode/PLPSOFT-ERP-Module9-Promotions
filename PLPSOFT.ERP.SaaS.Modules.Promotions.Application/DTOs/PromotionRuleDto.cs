namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    /// <summary>
    /// Ánh xạ từ Entity PromotionRule
    /// </summary>
    public class PromotionRuleDto
    {
        public long RuleID { get; set; }
        public long PromotionID { get; set; }
        public string RuleType { get; set; } = string.Empty;        // MIN_ORDER / MIN_QTY / CUSTOMER_GROUP / CATEGORY
        public decimal? MinOrderAmount { get; set; }
        public decimal? MinQuantity { get; set; }
        public long? CustomerGroupID { get; set; }
        public long? CategoryID { get; set; }
        public string DiscountType { get; set; } = string.Empty;    // PERCENT / AMOUNT
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
    }
}