namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence
{
    public class Product
    {
        public long ProductId { get; set; }
        public long CompanyId { get; set; }
        public long? CategoryId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }
    }
}