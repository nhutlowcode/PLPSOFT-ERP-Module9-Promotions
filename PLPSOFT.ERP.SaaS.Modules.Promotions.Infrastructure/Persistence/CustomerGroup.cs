namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence
{
    public class CustomerGroup
    {
        public long CustomerGroupId { get; set; }
        public long CompanyId { get; set; }
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}