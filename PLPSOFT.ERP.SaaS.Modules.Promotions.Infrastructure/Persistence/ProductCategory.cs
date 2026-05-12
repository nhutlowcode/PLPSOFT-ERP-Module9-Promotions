namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence
{
    public class ProductCategory
    {
        public long CategoryId { get; set; }
        public long CompanyId { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}