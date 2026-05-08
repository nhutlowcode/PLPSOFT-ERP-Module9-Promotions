using System.Collections.Generic;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    /// <summary>
    /// Object Module Sales gửi sang để tính KM
    /// </summary>
    public class CartRequest
    {
        public long CompanyID { get; set; }
        public long? BranchID { get; set; }
        public long? CustomerGroupID { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Đại diện 1 dòng sản phẩm trong giỏ
    /// </summary>
    public class CartItemDto
    {
        public long ProductID { get; set; }
        public long? CategoryID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;   // readonly, tự tính
    }

    /// <summary>
    /// Kết quả Engine trả về cho Sales
    /// </summary>
    public class CartDiscountResult
    {
        public bool HasDiscount { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<AppliedPromotionDto> AppliedPromotions { get; set; } = new();
        public List<CartItemDto> GiftItems { get; set; } = new();   // UnitPrice = 0
    }

    /// <summary>
    /// Thông tin một KM đã được áp dụng
    /// </summary>
    public class AppliedPromotionDto
    {
        public long PromotionID { get; set; }
        public string PromotionName { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }
}
