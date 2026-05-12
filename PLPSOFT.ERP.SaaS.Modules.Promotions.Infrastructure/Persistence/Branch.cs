namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence
{
    public class Branch
    {
        public long BranchId { get; set; }
        public long CompanyId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
    }
}