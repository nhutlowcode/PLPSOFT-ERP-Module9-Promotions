namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    /// <summary>
    /// Ánh xạ từ Entity PromotionProduct
    /// </summary>
    public class PromotionProductDto
    {
        public long ProductID { get; set; }
        public decimal? RequiredQuantity { get; set; }
        public decimal? FreeQuantity { get; set; }
        public bool IsGiftProduct { get; set; }     // true = quà tặng / false = hàng áp dụng
    }
}