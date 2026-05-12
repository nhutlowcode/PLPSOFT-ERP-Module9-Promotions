namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    public class ProductDto
    {
        public long ProductID { get; set; }
        public long CompanyID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}