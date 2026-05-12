namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs
{
    public class ProductCategoryDto
    {
        public long CategoryID { get; set; }
        public long CompanyID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
    }
}